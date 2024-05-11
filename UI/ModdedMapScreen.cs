using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comfort.Common;
using DG.Tweening;
using EFT;
using InGameMap.Data;
using SimpleCrosshair.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace InGameMap
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private static float _fadeMultiplierPerLayer = 0.66f;
        private static float _zoomScaler = 1.75f;
        private static float _zoomTweenTime = 0.25f;
        private float _zoomMaxScaler = 10f;
        private static Vector2 _markerSize = new Vector2(16, 16);

        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private GameObject _mapContentGO;
        private GameObject _mapLayersGO;
        private GameObject _mapMarkersGO;

        private MapDef _currentMapDef;
        private Dictionary<int, Image> _layers = new Dictionary<int, Image>();
        private Dictionary<string, Image> _markers = new Dictionary<string, Image>();

        private Vector2 _immediateMapAnchor = Vector2.zero;
        private float _zoomMin;
        private float _zoomMax;
        private float _zoomCurrent = 0.5f;

        private float _mapRotation = 0;
        private Quaternion _mapRotationQ;

        public void Update()
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

            var rotatedPos = GetRotatedVector2(mouseRelative, _mapRotation);

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

        public void Awake()
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
            var size = GetRectangleFromBounds(mapDef.Bounds);
            var rotatedSize = GetRotatedRectangle(size, _mapRotation);
            _mapContentGO.GetRectTransform().sizeDelta = rotatedSize;

            // set offset
            var offset = GetMidpoint(mapDef.Bounds);
            _mapContentGO.GetRectTransform().anchoredPosition = offset;

            // set initial zoom
            var maskSize = _scrollMask.RectTransform().sizeDelta;
            _zoomMin = Mathf.Min(maskSize.x / size.x, maskSize.y / size.y);
            _zoomMax = _zoomMaxScaler * _zoomMin;
            _zoomCurrent = _zoomMin;
            _mapContentGO.GetRectTransform().localScale = _zoomCurrent * Vector2.one;

            // rotate all of the map content
            _mapRotationQ = Quaternion.Euler(0, 0, _mapRotation);
            _mapContentGO.RectTransform().localRotation = _mapRotationQ;

            // load all layers
            foreach (var layerDef in mapDef.Layers)
            {
                LoadLayer(layerDef);
            }

            // TODO: should check if layer 0 exists
            DisplayLayer(0);

            // load static markers from def
            foreach (var (markerName, markerDef) in mapDef.StaticMarkers)
            {
                _markers[markerName] = CreateMarker(markerName, markerDef);
            }
        }

        private void UnloadMap()
        {
            // TODO: this
        }

        private void LoadLayer(LayerDef layerDef)
        {
            // set base image
            var layerGO = new GameObject($"layer{layerDef.LayerNumber}", typeof(RectTransform), typeof(CanvasRenderer));
            layerGO.layer = gameObject.layer;
            layerGO.transform.SetParent(_mapLayersGO.transform);
            layerGO.ResetRectTransform();

            var layerImage = layerGO.AddComponent<Image>();
            layerImage.type = Image.Type.Simple;
            layerImage.sprite = LoadSprite(layerDef.ImagePath);

            // set layer size
            var size = GetRectangleFromBounds(layerDef.Bounds);
            var rotatedSize = GetRotatedRectangle(size, _mapRotation);
            layerImage.GetRectTransform().sizeDelta = rotatedSize;

            // set layer offset
            var offset = GetMidpoint(layerDef.Bounds);
            layerImage.GetRectTransform().anchoredPosition = offset;

            // rotate base layer image, to combat when we rotate the whole map content
            layerImage.RectTransform().localRotation = Quaternion.Euler(0, 0, -_mapRotation);

            _layers[layerDef.LayerNumber] = layerImage;
        }

        private void DisplayLayer(int topLayer)
        {
            if (!_layers.ContainsKey(topLayer))
            {
                return;
            }

            // go through each layer and set fade color
            foreach(var (thisLayer, layerImage) in _layers)
            {
                layerImage.gameObject.SetActive(thisLayer <= topLayer);
                var c = Mathf.Pow(_fadeMultiplierPerLayer, topLayer - thisLayer);
                layerImage.color = new Color(c, c, c, 1);
            }
        }

        private Sprite LoadSprite(string path)
        {
            if (_spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = TextureUtils.LoadTexture2DFromPath(absolutePath);
            _spriteCache[path] = Sprite.Create(texture,
                                               new Rect(0f, 0f, texture.width, texture.height),
                                               new Vector2(texture.width / 2, texture.height / 2));

            return _spriteCache[path];
        }

        private Image CreateMarker(string name, MarkerDef def)
        {
            return CreateMarker(name, def.ImagePath, def.Position, _markerSize);
        }

        private Image CreateMarker(string name, string texturePath, Vector2 position, Vector2 size)
        {
            var markerGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            markerGO.layer = _mapMarkersGO.layer;
            markerGO.transform.SetParent(_mapMarkersGO.transform);

            // set position
            markerGO.ResetRectTransform();
            markerGO.GetRectTransform().anchoredPosition = position;
            markerGO.GetRectTransform().sizeDelta = size;

            // set rotation to combat when we rotate the whole map content
            markerGO.GetRectTransform().localRotation = Quaternion.Euler(0, 0, -_mapRotation);
            markerGO.GetRectTransform().localScale = 1 / _zoomCurrent * Vector3.one;

            // load image
            var markerImage = markerGO.AddComponent<Image>();
            markerImage.sprite = LoadSprite(texturePath);
            markerImage.type = Image.Type.Simple;

            return markerImage;
        }

        private void MoveMarker(string name, Vector2 position, float rotation)
        {
            if (!_markers.ContainsKey(name))
            {
                return;
            }

            var marker = _markers[name];
            marker.GetRectTransform().anchoredPosition = position;
            // FIXME: this is almost certainly incorrect
            marker.GetRectTransform().localRotation = Quaternion.Euler(0, 0, -rotation);
        }

        private void PlaceOrMovePlayerMarker(Player player)
        {
            if (!_markers.ContainsKey("player"))
            {
                _markers["player"] = CreateMarker("player", "Markers\\arrow.png", new Vector2(0f, 0f), _markerSize);
                _markers["player"].color = Color.blue;
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

        private static Vector2 GetRotatedVector2(Vector2 rotateMe, float degreeRotation)
        {
            var x = rotateMe.x;
            var y = rotateMe.y;
            var sin = Mathf.Sin(degreeRotation * Mathf.Deg2Rad);
            var cos = Mathf.Cos(degreeRotation * Mathf.Deg2Rad);
            return new Vector2(x * cos - y * sin, x * sin + y * cos);
        }

        private static Vector2 GetRotatedRectangle(Vector2 rectangle, float degreeRotation)
        {
            // adapted from https://stackoverflow.com/questions/54072295/get-bounds-of-unrotated-rotated-rectangle
            // Under CC BY-SA 4.0 Deed License
            var AB = rectangle.x;
            var AD = rectangle.y;
            var alpha = degreeRotation * Mathf.Deg2Rad;
            var gamma = (float)(Math.PI / 2f);
            var beta = gamma - alpha;
            var EA = Mathf.Abs(AD * Mathf.Sin(alpha));
            var ED = Mathf.Abs(AD * Mathf.Sin(beta));
            var FB = Mathf.Abs(AB * Mathf.Sin(alpha));
            var AF = Mathf.Abs(AB * Mathf.Sin(beta));

            return new Vector2(EA + AF, ED + FB);
            // END CC BY-SA 4.0 Deed License
        }

        private static Vector2 GetRectangleFromBounds(List<Vector2> bounds)
        {
            // TODO: there is probably a better way to do this
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var bound in bounds)
            {
                minX = Mathf.Min(minX, bound.x);
                maxX = Mathf.Max(maxX, bound.x);
                minY = Mathf.Min(minY, bound.y);
                maxY = Mathf.Max(maxY, bound.y);
            }

            return new Vector2(maxX - minX, maxY - minY);
        }

        private static Vector2 GetMidpoint(List<Vector2> bounds)
        {
            var sum = Vector2.zero;
            var count = 0;

            foreach (var bound in bounds)
            {
                sum += bound;
                count++;
            }

            return sum / count;
        }
    }
}
