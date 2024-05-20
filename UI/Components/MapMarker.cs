using System;
using System.Collections.Generic;
using InGameMap.Data;
using InGameMap.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class MapMarker : MonoBehaviour, ILayerBound, IPointerEnterHandler, IPointerExitHandler
    {
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

        // TODO: this seems... not great?
        public Dictionary<string, Dictionary<LayerStatus, float>> CategoryImageAlphaLayerStatus { get; protected set; }
            = new Dictionary<string, Dictionary<LayerStatus, float>>
            {
                {"Extract", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.25f},
                    {LayerStatus.Underneath, 0.50f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }}
            };
        public Dictionary<string, Dictionary<LayerStatus, float>> CategoryLabelAlphaLayerStatus { get; protected set; }
            = new Dictionary<string, Dictionary<LayerStatus, float>>
            {
                {"Extract", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }}
            };

        private float _initialRotation;
        private bool _hasSetOutline = false;

        public static MapMarker Create(GameObject parent, MapMarkerDef def, Vector2 size, float degreesRotation, float scale)
        {
            var mapMarker = Create<MapMarker>(parent, def.Text, def.Category, def.ImagePath, def.Color, def.Position, size,
                                              def.Pivot, degreesRotation, scale, def.ShowInRaid);
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
            marker.Label.fontSizeMin = _markerMinFontSize;
            marker.Label.fontSizeMax = _markerMaxFontSize;
            marker.Label.alignment = TextAlignmentOptions.Top;
            marker.Label.enableWordWrapping = true;
            marker.Label.enableAutoSizing = true;
            marker.Label.text = marker.Text;

            marker._hasSetOutline = UIUtils.TrySetTMPOutline(marker.Label);

            marker.Color = color;

            marker.Label.gameObject.SetActive(false);

            return marker;
        }

        protected virtual void OnEnable()
        {
            if (_hasSetOutline || Label == null)
            {
                return;
            }

            _hasSetOutline = UIUtils.TrySetTMPOutline(Label);
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
            HandleNewLayerStatus(LayerStatus.FullReveal);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPositionChanged?.Invoke(this);
        }
    }
}
