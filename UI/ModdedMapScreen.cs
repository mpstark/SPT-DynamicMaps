using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DG.Tweening;
using EFT;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private static float _fadeMultiplierPerLayer = 0.5f;
        private static float _zoomScaler = 1.75f;
        private static float _zoomTweenTime = 0.25f;
        private static float _zoomMaxScaler = 10f;
        private static Vector2 _markerSize = new Vector2(16, 16);

        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private GameObject _mapContentGO;
        private GameObject _mapLayersGO;
        private GameObject _mapMarkersGO;

        private RectTransform _mapRectTransform => _mapContentGO.GetRectTransform();

        private MapDef _currentMapDef;
        private Dictionary<string, MapLayer> _layers = new Dictionary<string, MapLayer>();
        private Dictionary<string, MapMarker> _markers = new Dictionary<string, MapMarker>();

        private Vector2 _immediateMapAnchor = Vector2.zero;
        private float _zoomMin; // set when map loaded
        private float _zoomMax; // set when map loaded
        private float _zoomCurrent = 0.5f;
        private float _coordinateRotation = 0;

        private void Update()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                OnScroll(scroll);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mapRectTransform, Input.mousePosition, null, out Vector2 relativePosition);

                Plugin.Log.LogInfo($"Position: {relativePosition}");
            }
        }

        private void OnScroll(float scroll)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapRectTransform, Input.mousePosition, null, out Vector2 mouseRelative);
            var rotatedRelative = MathUtils.GetRotatedVector2(mouseRelative, _coordinateRotation);

            var zoomDelta = scroll * _zoomCurrent * _zoomScaler;
            var zoomNew = Mathf.Clamp(_zoomCurrent + zoomDelta, _zoomMin, _zoomMax);
            var actualDelta = zoomNew - _zoomCurrent;

            SetMapZoom(zoomNew, _zoomTweenTime);
            ShiftMap(-rotatedRelative * actualDelta, _zoomTweenTime);
        }

        public void SetMapZoom(float zoomNew, float tweenTime)
        {
            zoomNew = Mathf.Clamp(zoomNew, _zoomMin, _zoomMax);

            // already there
            if (zoomNew == _zoomCurrent)
            {
                return;
            }

            _zoomCurrent = zoomNew;

            // stop any movement that the scroll rect is doing because of momentum
            _scrollRect.StopMovement();

            // scale all map content up by scaling parent
            _mapRectTransform.DOScale(_zoomCurrent * Vector3.one, tweenTime);

            // inverse scale all map markers
            // FIXME: does this generate large amounts of garbage?
            var mapMarkers = _mapMarkersGO.transform.GetChildren();
            foreach (var mapMarker in mapMarkers)
            {
                mapMarker.DOScale(1/_zoomCurrent * Vector3.one, tweenTime);
            }
        }

        public void ShiftMap(Vector2 shift, float tweenTime)
        {
            if (shift == Vector2.zero)
            {
                return;
            }

            // stop any movement that the scroll rect is doing because of momentum
            _scrollRect.StopMovement();

            // check if tweening to update _immediateMapAnchor, since the scroll rect might have moved the anchor
            if (!DOTween.IsTweening(_mapRectTransform, true))
            {
                _immediateMapAnchor = _mapRectTransform.anchoredPosition;
            }

            _immediateMapAnchor += shift;
            _mapRectTransform.DOAnchorPos(_immediateMapAnchor, tweenTime);
        }

        private void Awake()
        {
            // set up scroll rect
            var scrollRectGO = new GameObject("Scroll", typeof(RectTransform), typeof(CanvasRenderer));
            scrollRectGO.layer = gameObject.layer;
            scrollRectGO.transform.SetParent(gameObject.transform);
            scrollRectGO.GetRectTransform().sizeDelta = gameObject.RectTransform().sizeDelta;
            scrollRectGO.ResetRectTransform();
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

            // set up mask
            var scrollMaskGO = new GameObject("ScrollMask", typeof(RectTransform), typeof(CanvasRenderer));
            scrollMaskGO.layer = gameObject.layer;
            scrollMaskGO.transform.SetParent(scrollRectGO.transform);
            // FIXME: adjust scroll mask to exact fit
            scrollMaskGO.GetRectTransform().sizeDelta = gameObject.RectTransform().sizeDelta - new Vector2(0, 80f);
            scrollMaskGO.ResetRectTransform();
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();
            _scrollRect.viewport = _scrollMask.GetRectTransform();

            // set up container for the map content that will scroll
            _mapContentGO = new GameObject("MapContent", typeof(RectTransform), typeof(CanvasRenderer));
            _mapContentGO.layer = gameObject.layer;
            _mapContentGO.transform.SetParent(scrollMaskGO.transform);
            _mapContentGO.ResetRectTransform();
            _scrollRect.content = _mapRectTransform;

            // put all map layers in a container for neatness
            _mapLayersGO = new GameObject("MapLayers", typeof(RectTransform), typeof(CanvasRenderer));
            _mapLayersGO.layer = gameObject.layer;
            _mapLayersGO.transform.SetParent(_mapContentGO.transform);
            _mapLayersGO.ResetRectTransform();

            // map markers need to inverse scale, so make a container for them so we can do them all at once
            _mapMarkersGO = new GameObject("MapMarkers", typeof(RectTransform), typeof(CanvasRenderer));
            _mapMarkersGO.layer = gameObject.layer;
            _mapMarkersGO.transform.SetParent(_mapContentGO.transform);
            _mapMarkersGO.ResetRectTransform();

            // TODO: remove this and load map dynamically or from dropdown
            // var mapDef = MapDef.LoadFromPath("Maps\\Factory\\factory.json");
            var mapDef = MapDef.LoadFromPath("Maps\\Interchange\\interchange.json");
            LoadMap(mapDef);
        }

        private void LoadMap(MapDef mapDef)
        {
            _currentMapDef = mapDef;
            _coordinateRotation = mapDef.CoordinateRotation;

            // set width and height for top level
            var size = MathUtils.GetBoundingRectangle(mapDef.Bounds);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, _coordinateRotation);
            _mapRectTransform.sizeDelta = rotatedSize;

            // set offset
            var offset = MathUtils.GetMidpoint(mapDef.Bounds);
            _mapRectTransform.anchoredPosition = offset;

            // set zoom min and max based on size of map and size of mask
            var maskSize = _scrollMask.RectTransform().sizeDelta;
            _zoomMin = Mathf.Min(maskSize.x / size.x, maskSize.y / size.y);
            _zoomMax = _zoomMaxScaler * _zoomMin;

            // rotate all of the map content
            var _mapRotationQ = Quaternion.Euler(0, 0, _coordinateRotation);
            _mapContentGO.RectTransform().localRotation = _mapRotationQ;

            // load all layers
            foreach (var (layerName, layerDef) in mapDef.Layers)
            {
                _layers[layerName] = new MapLayer(_mapLayersGO, layerName, layerDef, -_coordinateRotation);
            }

            // set layer order
            int i = 0;
            foreach (var layer in _layers.Values.OrderBy(l => l.Level))
            {
                layer.RectTransform.SetSiblingIndex(i++);
            }

            foreach (var (name, markerDef) in mapDef.StaticMarkers)
            {
                _markers[name] = new MapMarker(_mapMarkersGO, name, markerDef, _markerSize, -_coordinateRotation);
            }

            // this will set everything up for initial zoom
            SetMapZoom(_zoomMin, 0);

            // select layer by the default level
            SelectLayersByLevel(mapDef.DefaultLevel);
        }

        private void UnloadMap()
        {
            // TODO: this
        }

        private void SelectLayersByLevel(int level)
        {
            // go through each layer and set fade color
            foreach (var (layerName, layer) in _layers)
            {
                // show layer if at or below the current level
                layer.GameObject.SetActive(layer.Level <= level);

                // fade other layers according to difference in level
                var c = Mathf.Pow(_fadeMultiplierPerLayer, level - layer.Level);
                layer.Image.color = new Color(c, c, c, 1);
            }

            // go through all markers and call OnLayerSelect
            foreach (var (markerName, marker) in _markers)
            {
                foreach (var (layerName, layer) in _layers)
                {
                    marker.OnLayerSelect(layerName, layer.Level == level);
                }
            }
        }

        private void SelectLayersByCoords(Vector2 coords, float height)
        {
            // TODO: better select that shows only layers in coords
            foreach(var (name, layer) in _layers)
            {
                if (height > layer.HeightBounds.x && height < layer.HeightBounds.y)
                {
                    SelectLayersByLevel(layer.Level);
                    return;
                }
            }
        }

        private void ShowInRaid(Player player)
        {
            // TODO: adjust mask

            // TODO: hide map selector and make sure that current map is loaded

            // create player marker if one doesn't already exist
            if (!_markers.ContainsKey("player"))
            {
                _markers["player"] = new MapMarker(_mapMarkersGO, "player", "player", "Markers\\arrow.png",
                                                    new Vector2(0f, 0f), _markerSize, -_coordinateRotation, 1 / _zoomCurrent);
                _markers["player"].Image.color = Color.cyan;
            }

            // move player marker
            var player3dPos = player.CameraPosition.position;
            var player2dPos = new Vector2(player3dPos.x, player3dPos.z);
            var angles = player.CameraPosition.eulerAngles;
            _markers["player"].Move(player2dPos, -angles.y); // I'm unsure why negative rotation here

            // select layers to show
            SelectLayersByCoords(player2dPos, player3dPos.y);
        }

        private void ShowOutOfRaid()
        {
            // TODO: adjust mask

            // TODO: show map selector
        }

        internal void Show()
        {
            transform.parent.Find("MapBlock").gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock").gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);
            gameObject.SetActive(true);

            // check if raid
            var game = Singleton<AbstractGame>.Instance;
            if (game != null && game is LocalGame)
            {
                var owner = (game as LocalGame).PlayerOwner;
                ShowInRaid(owner.Player);
            }
            else
            {
                ShowOutOfRaid();
            }
        }

        internal void Close()
        {
            transform.parent.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        internal static ModdedMapScreen AttachTo(GameObject parent)
        {
            var go = new GameObject("ModdedMapBlock", typeof(RectTransform));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();
            var rect = parent.GetRectTransform().rect;
            go.GetRectTransform().sizeDelta = new Vector2(rect.width, rect.height);

            return go.AddComponent<ModdedMapScreen>();
        }
    }
}
