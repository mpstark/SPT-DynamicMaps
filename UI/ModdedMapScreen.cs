using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using SimpleCrosshair.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private static float _fadeMultiplierPerLayer = 0.66f;
        private static float _zoomScaler = 1.75f;
        private static float _zoomTweenTime = 0.25f;

        private ScrollRect _scrollRect;
        private Mask _scrollMask;

        private GameObject _mapContent;
        private GameObject _mapLayers;
        private GameObject _mapMarkers;
        private Dictionary<int, Image> _layers = new Dictionary<int, Image>();

        private Vector2 _immediateMapAnchor = Vector2.zero;
        private float _zoomMin = 1f; // will be replaced with the min zoom based on size of map
        private float _zoomMax = 10f; // TODO: set this one from size of map too?
        private float _zoom = 0.5f;
        private float _rotation = 0;

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
                    _mapContent.GetRectTransform(), Input.mousePosition, null, out Vector2 relativePosition);

                Plugin.Log.LogInfo($"Position: {relativePosition}");
            }
        }

        private void OnScroll(float scroll)
        {
            var mapRectTransform = _mapContent.GetRectTransform();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapRectTransform, Input.mousePosition, null, out Vector2 relPos);

            var x = relPos.x;
            var y = relPos.y;
            var sin = Mathf.Sin(_rotation * Mathf.Deg2Rad);
            var cos = Mathf.Cos(_rotation * Mathf.Deg2Rad);
            // FIXME: this is busted
            var rotatedPos = new Vector2(x * cos + y * sin, x * sin + y * cos);

            var oldZoom = _zoom;
            var zoomDelta = scroll * _zoom * _zoomScaler;
            _zoom = Mathf.Clamp(_zoom + zoomDelta, _zoomMin, _zoomMax);
            zoomDelta = _zoom - oldZoom;

            if (_zoom != mapRectTransform.localScale.x)
            {
                _scrollRect.StopMovement();

                // check if tweening to update _immediateMapAnchor, since the scroll rect might have moved the anchor
                if (!DOTween.IsTweening(mapRectTransform, true))
                {
                    _immediateMapAnchor = mapRectTransform.GetRectTransform().anchoredPosition;
                }

                // scale all map content up by scaling parent
                mapRectTransform.DOScale(_zoom * Vector3.one, _zoomTweenTime);

                // adjust position to new scroll, since we're moving towards cursor
                _immediateMapAnchor -= rotatedPos * zoomDelta;
                mapRectTransform.DOAnchorPos(_immediateMapAnchor, _zoomTweenTime);

                // inverse scale all map markers
                // FIXME: does this generate large amounts of garbage?
                var mapMarkers = _mapMarkers.transform.GetChildren();
                foreach (var mapMarker in mapMarkers)
                {
                    mapMarker.DOScale(1/_zoom * Vector3.one, _zoomTweenTime);
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
            scrollMaskImage.color = new Color(0f,0f,0f,1f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();
            _scrollRect.viewport = _scrollMask.GetRectTransform();

            // set up container for the map content that will scroll
            _mapContent = new GameObject("MapContent", typeof(RectTransform), typeof(CanvasRenderer));
            _mapContent.layer = gameObject.layer;
            _mapContent.transform.SetParent(scrollMaskGO.transform);
            _mapContent.ResetRectTransform();
            _scrollRect.content = _mapContent.GetRectTransform();

            // put all map layers in a container for neatness
            _mapLayers = new GameObject("MapLayers", typeof(RectTransform), typeof(CanvasRenderer));
            _mapLayers.layer = gameObject.layer;
            _mapLayers.transform.SetParent(_mapContent.transform);
            _mapLayers.ResetRectTransform();

            // map markers need to inverse scale, so make a container for them so we can do them all at once
            _mapMarkers = new GameObject("MapMarkers", typeof(RectTransform), typeof(CanvasRenderer));
            _mapMarkers.layer = gameObject.layer;
            _mapMarkers.transform.SetParent(_mapContent.transform);
            _mapMarkers.ResetRectTransform();

            LoadMap(new Vector2(530, -439), new Vector2(-364, 452), 180);
            CreateMarker("TestSwitch", "switch.png", new Vector2(-201.16f, -357.81f), new Vector2(30, 30));
        }

        private void LoadMap(Vector2 bottomRight, Vector2 topLeft, float rotate)
        {
            // set width and height for top level
            var size = new Vector2(bottomRight.x - topLeft.x, topLeft.y - bottomRight.y);
            _mapContent.GetRectTransform().sizeDelta = size;

            // set offset
            var offset = (bottomRight + topLeft) / 2f;
            _mapContent.GetRectTransform().anchoredPosition = offset;

            // set initial zoom
            var maskSize = _scrollMask.RectTransform().sizeDelta;
            _zoomMin = Mathf.Min(maskSize.x / size.x, maskSize.y / size.y);
            _zoom = _zoomMin;
            _mapContent.GetRectTransform().localScale = _zoom * Vector2.one;

            // rotate all of the map content
            _rotation = rotate;
            var contentAngles = _mapContent.RectTransform().eulerAngles;
            _mapContent.RectTransform().rotation = Quaternion.Euler(contentAngles.x, contentAngles.y, _rotation);

            LoadLayer("interchange_layer_0.png", bottomRight, topLeft, 0);
            LoadLayer("interchange_layer_1.png", bottomRight, topLeft, 1);
            LoadLayer("interchange_layer_2.png", bottomRight, topLeft, 2);
            DisplayLayer(0);
        }

        private void LoadLayer(string imagePath, Vector2 bottomRight, Vector2 topLeft, int layerNum)
        {
            // set base image
            var layerGO = new GameObject($"layer{layerNum}", typeof(RectTransform), typeof(CanvasRenderer));
            layerGO.layer = gameObject.layer;
            layerGO.transform.SetParent(_mapLayers.transform);
            layerGO.ResetRectTransform();

            var layerImage = layerGO.AddComponent<Image>();
            layerImage.type = Image.Type.Simple;
            layerImage.sprite = LoadSprite(imagePath);

            // set layer size
            var size = new Vector2(bottomRight.x - topLeft.x, topLeft.y - bottomRight.y);
            layerImage.GetRectTransform().sizeDelta = size;

            // set layer offset
            var offset = (bottomRight + topLeft) / 2f;
            layerImage.GetRectTransform().anchoredPosition = offset;

            // rotate base layer image, to combat when we rotate the whole map content
            var angles = layerImage.RectTransform().eulerAngles;
            layerImage.RectTransform().localRotation = Quaternion.Euler(angles.x, angles.y, -_rotation);

            _layers[layerNum] = layerImage;
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

        private Sprite LoadSprite(string texturePath)
        {
            var path = Path.Combine(Plugin.Path, texturePath);
            var texture = TextureUtils.LoadTexture2DFromPath(path);
            var sprite = Sprite.Create(texture,
                                       new Rect(0f, 0f, texture.width, texture.height),
                                       new Vector2(texture.width / 2, texture.height / 2));
            return sprite;
        }

        private Image CreateMarker(string name, string texturePath, Vector2 position, Vector2 size)
        {
            var markerGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            markerGO.layer = _mapMarkers.layer;
            markerGO.transform.SetParent(_mapMarkers.transform);

            // set position
            markerGO.ResetRectTransform();
            markerGO.GetRectTransform().anchoredPosition = position;
            markerGO.GetRectTransform().sizeDelta = size;

            // set rotation to combat when we rotate the whole map content
            var angles = markerGO.RectTransform().eulerAngles;
            markerGO.RectTransform().localRotation = Quaternion.Euler(angles.x, angles.y, -_rotation);

            // load image
            var markerImage = markerGO.AddComponent<Image>();
            markerImage.sprite = LoadSprite(texturePath);
            markerImage.type = Image.Type.Simple;

            return markerImage;
        }

        internal void Show()
        {
            transform.parent.Find("MapBlock").gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock").gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);
            gameObject.SetActive(true);
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
