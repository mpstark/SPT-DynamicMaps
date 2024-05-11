using System.Collections.Generic;
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
        private static float _fadeMultiplierPerLayer = 0.66f;
        private static float _zoomScaler = 1.75f;
        private static float _zoomTweenTime = 0.25f;
        private static float _zoomMaxScaler = 10f;
        private static Vector2 _markerSize = new Vector2(16, 16);

        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private GameObject _mapContentGO;
        private GameObject _mapLayersGO;
        private GameObject _mapMarkersGO;

        private MapDef _currentMapDef;
        private Dictionary<string, MapLayer> _layers = new Dictionary<string, MapLayer>();
        private Dictionary<string, MapMarker> _markers = new Dictionary<string, MapMarker>();

        private Vector2 _immediateMapAnchor = Vector2.zero;
        private float _zoomMin; // set when map loaded
        private float _zoomMax; // set when map loaded
        private float _zoomCurrent = 0.5f;
        private float _mapRotation = 0;
        private int _selectedLayer;

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
                    _mapContentGO.GetRectTransform(), Input.mousePosition, null, out Vector2 relativePosition);

                Plugin.Log.LogInfo($"Position: {relativePosition}");
            }
        }

        private void OnScroll(float scroll)
        {
            var mapRectTransform = _mapContentGO.GetRectTransform();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapRectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            var rotatedPos = MathUtils.GetRotatedVector2(mouseRelative, _mapRotation);

            var oldZoom = _zoomCurrent;
            var zoomDelta = scroll * _zoomCurrent * _zoomScaler;
            _zoomCurrent = Mathf.Clamp(_zoomCurrent + zoomDelta, _zoomMin, _zoomMax);
            zoomDelta = _zoomCurrent - oldZoom;

            if (_zoomCurrent != mapRectTransform.localScale.x)
            {
                _scrollRect.StopMovement();

                // check if tweening to update _immediateMapAnchor, since the scroll rect might have moved the anchor
                if (!DOTween.IsTweening(mapRectTransform, true))
                {
                    _immediateMapAnchor = mapRectTransform.GetRectTransform().anchoredPosition;
                }

                // scale all map content up by scaling parent
                mapRectTransform.DOScale(_zoomCurrent * Vector3.one, _zoomTweenTime);

                // adjust position to new scroll, since we're moving towards cursor
                _immediateMapAnchor -= rotatedPos * zoomDelta;
                mapRectTransform.DOAnchorPos(_immediateMapAnchor, _zoomTweenTime);

                // inverse scale all map markers
                // FIXME: does this generate large amounts of garbage?
                var mapMarkers = _mapMarkersGO.transform.GetChildren();
                foreach (var mapMarker in mapMarkers)
                {
                    mapMarker.DOScale(1/_zoomCurrent * Vector3.one, _zoomTweenTime);
                }
            }
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
            _scrollRect.content = _mapContentGO.GetRectTransform();

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
            _mapRotation = mapDef.Rotation;

            // set width and height for top level
            var size = MathUtils.GetBoundingRectangle(mapDef.Bounds);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, _mapRotation);
            _mapContentGO.GetRectTransform().sizeDelta = rotatedSize;

            // set offset
            var offset = MathUtils.GetMidpoint(mapDef.Bounds);
            _mapContentGO.GetRectTransform().anchoredPosition = offset;

            // set initial zoom
            var maskSize = _scrollMask.RectTransform().sizeDelta;
            _zoomMin = Mathf.Min(maskSize.x / size.x, maskSize.y / size.y);
            _zoomMax = _zoomMaxScaler * _zoomMin;
            _zoomCurrent = _zoomMin;
            _mapContentGO.GetRectTransform().localScale = _zoomCurrent * Vector2.one;

            // rotate all of the map content
            var _mapRotationQ = Quaternion.Euler(0, 0, _mapRotation);
            _mapContentGO.RectTransform().localRotation = _mapRotationQ;

            // load all layers
            foreach (var (layerName, layerDef) in mapDef.Layers)
            {
                _layers[layerName] = new MapLayer(_mapLayersGO, layerName, layerDef, -_mapRotation);
            }

            // TODO: should check if layer 0 exists
            SelectLayer(0);

            // load static markers from def
            foreach (var (name, markerDef) in mapDef.StaticMarkers)
            {
                _markers[name] = new MapMarker(_mapMarkersGO, name, markerDef, _markerSize, -_mapRotation, _zoomCurrent);
            }
        }

        private void UnloadMap()
        {
            // TODO: this
        }

        private void SelectLayer(int layerNum)
        {
            _selectedLayer = layerNum;

            // go through each layer and set fade color
            foreach(var (name, layer) in _layers)
            {
                layer.GameObject.SetActive(layer.LayerNumber <= layerNum);

                var c = Mathf.Pow(_fadeMultiplierPerLayer, layerNum - layer.LayerNumber);
                layer.Image.color = new Color(c, c, c, 1);
            }
        }

        public void MoveMarker(string name, Vector2 position, float rotation)
        {
            if (!_markers.ContainsKey(name))
            {
                return;
            }

            var marker = _markers[name];
            marker.RectTransform.anchoredPosition = position;

            // FIXME: I'm unsure why this is correct
            marker.RectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
        }

        private void PlaceOrMovePlayerMarker(Player player)
        {
            if (!_markers.ContainsKey("player"))
            {
                _markers["player"] = new MapMarker(_mapMarkersGO, "player", "player", "Markers\\arrow.png",
                                                    new Vector2(0f, 0f), _markerSize, -_mapRotation, _zoomCurrent);
                _markers["player"].Image.color = Color.cyan;
            }

            var player3dPos = player.CameraPosition.position;
            var player2dPos = new Vector2(player3dPos.x, player3dPos.z);
            var angles = player.CameraPosition.eulerAngles;

            MoveMarker("player", player2dPos, angles.y);
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
                PlaceOrMovePlayerMarker(owner.Player);
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
