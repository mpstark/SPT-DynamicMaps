using System;
using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    public class MapMarker : MonoBehaviour, ILayerBound, IPointerEnterHandler, IPointerExitHandler
    {
        // TODO: this seems... not great?
        public static Dictionary<string, Dictionary<LayerStatus, float>> CategoryImageAlphaLayerStatus { get; protected set; }
            = new Dictionary<string, Dictionary<LayerStatus, float>>
            {
                {"Extract", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Quest", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
            };
        public static Dictionary<string, Dictionary<LayerStatus, float>> CategoryLabelAlphaLayerStatus { get; protected set; }
            = new Dictionary<string, Dictionary<LayerStatus, float>>
            {
                {"Extract", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Quest", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
            };

        private static Vector2 _labelSizeMultiplier = new Vector2(2.5f, 2f);
        private static float _markerMinFontSize = 9f;
        private static float _markerMaxFontSize = 13f;

        public event Action<ILayerBound> OnPositionChanged;

        public string Text { get; protected set; }
        public string Category { get; protected set; }
        public MapView ContainingMapView { get; set; }

        public Image Image { get; protected set; }
        public TextMeshProUGUI Label { get; protected set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public string AssociatedItemId { get; protected set; } = "";
        public bool IsDynamic { get; protected set; } = false;
        public bool ShowInRaid { get; protected set; } = true;

        private Vector3 _position;
        public Vector3 Position
        {
            get
            {
                return _position;
            }

            set
            {
                Move(value);
            }
        }

        private float _rotation = 0f;
        public float Rotation
        {
            get
            {
                return _rotation;
            }

            set
            {
                SetRotation(value);
            }
        }

        private Color _color = Color.white;
        public Color Color
        {
            get
            {
                return _color;
            }

            set
            {
                _color = value;
                Image.color = value;
                Label.color = value;
            }
        }

        private Vector2 _size = new Vector2(30f, 30f);
        public Vector2 Size
        {
            get
            {
                return _size;
            }

            set
            {
                _size = value;
                RectTransform.sizeDelta = _size;
                Image.GetRectTransform().sizeDelta = _size;
                Label.GetRectTransform().sizeDelta = _size * _labelSizeMultiplier;
            }
        }

        public Dictionary<LayerStatus, float> ImageAlphaLayerStatus { get; protected set; } = new Dictionary<LayerStatus, float>
            {
                {LayerStatus.Hidden, 0.0f},
                {LayerStatus.Underneath, 0.0f},
                {LayerStatus.OnTop, 1f},
                {LayerStatus.FullReveal, 1f},
            };
        public Dictionary<LayerStatus, float> LabelAlphaLayerStatus { get; protected set; } = new Dictionary<LayerStatus, float>
            {
                {LayerStatus.Hidden, 0.0f},
                {LayerStatus.Underneath, 0.0f},
                {LayerStatus.OnTop, 0.0f},
                {LayerStatus.FullReveal, 1f},
            };

        private float _initialRotation;
        private bool _hasSetOutline = false;
        private bool _isInFullReveal = false;

        public static MapMarker Create(GameObject parent, MapMarkerDef def, Vector2 size, float degreesRotation, float scale)
        {
            var mapMarker = Create<MapMarker>(parent, def.Text, def.Category, def.ImagePath, def.Color, def.Position, size,
                                              def.Pivot, degreesRotation, scale, def.ShowInRaid);
            mapMarker.AssociatedItemId = def.AssociatedItemId;

            return mapMarker;
        }

        public static T Create<T>(GameObject parent, string text, string category, string imageRelativePath, Color color,
                                  Vector3 position, Vector2 size, Vector2 pivot, float degreesRotation, float scale,
                                  bool showInRaid = true)
                            where T : MapMarker
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapMarker {text}");

            // this is to receive mouse events
            var fakeImage = go.AddComponent<Image>();
            fakeImage.color = Color.clear;
            fakeImage.raycastTarget = true;

            var rectTransform = go.GetRectTransform();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);
            rectTransform.pivot = pivot;

            var marker = go.AddComponent<T>();
            marker.Text = text;
            marker.Category = category;
            marker.Position = position;
            marker._initialRotation = degreesRotation;
            marker.ShowInRaid = showInRaid;

            // image
            var imageGO = UIUtils.CreateUIGameObject(go, "image");
            imageGO.AddComponent<CanvasRenderer>();
            imageGO.GetRectTransform().sizeDelta = size;
            imageGO.GetRectTransform().pivot = new Vector2(0.5f, 0.5f);
            marker.Image = imageGO.AddComponent<Image>();
            marker.Image.raycastTarget = false;
            marker.Image.sprite = TextureUtils.GetOrLoadCachedSprite(imageRelativePath);
            marker.Image.type = Image.Type.Simple;

            // var outline = imageGO.AddComponent<Outline>();
            // outline.effectColor = Color.black;
            // outline.effectDistance = Vector2.one;

            // label
            var labelGO = UIUtils.CreateUIGameObject(go, "label");
            labelGO.AddComponent<CanvasRenderer>();
            labelGO.GetRectTransform().anchorMin = new Vector2(0.5f, 0f);
            labelGO.GetRectTransform().anchorMax = new Vector2(0.5f, 0f);
            labelGO.GetRectTransform().pivot = new Vector2(0.5f, 1f);
            labelGO.GetRectTransform().sizeDelta = size * _labelSizeMultiplier;
            marker.Label = labelGO.AddComponent<TextMeshProUGUI>();
            marker.Label.alignment = TextAlignmentOptions.Top;
            marker.Label.enableWordWrapping = true;
            marker.Label.enableAutoSizing = true;
            marker.Label.fontSizeMin = _markerMinFontSize;
            marker.Label.fontSizeMax = _markerMaxFontSize;
            marker.Label.text = marker.Text;

            marker._hasSetOutline = UIUtils.TrySetTMPOutline(marker.Label);

            marker.Color = color;
            marker._size = size;

            marker.Label.gameObject.SetActive(false);

            return marker;
        }

        protected virtual void OnEnable()
        {
            TrySetOutlineAndResize();
        }

        protected virtual void OnDestroy()
        {
            OnPositionChanged = null;
        }

        public void Move(Vector3 newPosition, bool callback = true)
        {
            RectTransform.anchoredPosition = newPosition; // vector3 to vector2 discards z
            _position = newPosition;

            if (callback)
            {
                OnPositionChanged?.Invoke(this);
            }
        }

        public void SetRotation(float degreesRotation)
        {
            _rotation = degreesRotation;
            Image.gameObject.GetRectTransform().localRotation = Quaternion.Euler(0, 0, degreesRotation - _initialRotation);
        }

        public void MoveAndRotate(Vector3 newPosition, float rotation, bool callback = true)
        {
            Move(newPosition, callback);
            SetRotation(rotation);
        }

        public void HandleNewLayerStatus(LayerStatus status)
        {
            if (!ShowInRaid && GameUtils.IsInRaid())
            {
                gameObject.SetActive(false);
                return;
            }

            if (_isInFullReveal)
            {
                status = LayerStatus.FullReveal;
            }

            var imageAlpha = ImageAlphaLayerStatus[status];
            var labelAlpha = LabelAlphaLayerStatus[status];

            if (CategoryImageAlphaLayerStatus.ContainsKey(Category))
            {
                imageAlpha = CategoryImageAlphaLayerStatus[Category][status];
            }
            if (CategoryLabelAlphaLayerStatus.ContainsKey(Category))
            {
                labelAlpha = CategoryLabelAlphaLayerStatus[Category][status];
            }

            Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, imageAlpha);
            Label.color = new Color(Label.color.r, Label.color.g, Label.color.b, labelAlpha);

            Image.gameObject.SetActive(imageAlpha > 0f);
            Label.gameObject.SetActive(labelAlpha > 0f);
            gameObject.SetActive(labelAlpha > 0f || imageAlpha > 0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TrySetOutlineAndResize();

            _isInFullReveal = true;
            transform.SetAsLastSibling();
            HandleNewLayerStatus(LayerStatus.FullReveal);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isInFullReveal = false;
            OnPositionChanged?.Invoke(this);
        }

        private void TrySetOutlineAndResize()
        {
            if (_hasSetOutline || Label == null)
            {
                return;
            }

            // try resetting text, since it seems like if outline fails, it doesn't size properly
            Label.enableAutoSizing = true;
            Label.enableWordWrapping = true;
            Label.fontSizeMin = _markerMinFontSize;
            Label.fontSizeMax = _markerMaxFontSize;
            Label.alignment = TextAlignmentOptions.Top;
            Label.text = $"{Label.text}";

            _hasSetOutline = UIUtils.TrySetTMPOutline(Label);
        }
    }
}
