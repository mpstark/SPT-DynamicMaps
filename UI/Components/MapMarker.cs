using System;
using InGameMap.Data;
using InGameMap.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class MapMarker : MonoBehaviour
    {
        private static Vector2 _labelSizeMultiplier = new Vector2(2.5f, 2f);
        private static float _markerMinFontSize = 8f;
        private static float _markerMaxFontSize = 12f;

        public event Action<MapMarker> OnPositionChanged;

        public string Text { get; protected set; }
        public string Category { get; protected set; }
        public MapView ContainingMapView { get; set; }

        public Image Image { get; protected set; }
        public TextMeshProUGUI Label { get; protected set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public bool IsDynamic { get; protected set; } = false;
        public Vector3 Position { get; protected set;}
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

        private Color _color = Color.white;
        private float _initialRotation;
        private bool _hasSetOutline = false;

        public static MapMarker Create(GameObject parent, MapMarkerDef def, Vector2 size, float degreesRotation, float scale)
        {
            var mapMarker = Create<MapMarker>(parent, def.Text, def.Category, def.ImagePath, def.Position, size,
                                              def.Pivot, degreesRotation, scale);
            return mapMarker;
        }

        public static T Create<T>(GameObject parent, string text, string category, string imageRelativePath,
                                  Vector3 position, Vector2 size, Vector2 pivot, float degreesRotation, float scale)
                            where T : MapMarker
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapMarker {text}");

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

            // image
            var imageGO = UIUtils.CreateUIGameObject(go, "image");
            imageGO.AddComponent<CanvasRenderer>();
            imageGO.GetRectTransform().sizeDelta = size;
            imageGO.GetRectTransform().pivot = new Vector2(0.5f, 0.5f);
            marker.Image = imageGO.AddComponent<Image>();
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
            Position = newPosition;

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

        protected virtual Color GetLayerAdjustedLabelColor(bool isLayerDisplayed, bool isLayerOnTop)
        {
            var alpha = (!isLayerDisplayed || !isLayerOnTop)
                        ? 0.0f
                        : 1.0f;
            return new Color(Label.color.r, Label.color.g, Label.color.b, alpha);
        }

        protected virtual Color GetLayerAdjustedImageColor(bool isLayerDisplayed, bool isLayerOnTop)
        {
            var alpha = 1f;
            if (!isLayerDisplayed)
            {
                alpha = 0.0f;
            }
            else if (!isLayerOnTop)
            {
                alpha = 0.25f;
            }

            return new Color(Image.color.r, Image.color.g, Image.color.b, alpha);
        }

        public void OnContainingLayerChanged(bool isLayerDisplayed, bool isLayerOnTop)
        {
            Image.color = GetLayerAdjustedImageColor(isLayerDisplayed, isLayerOnTop);
            Label.color = GetLayerAdjustedLabelColor(isLayerDisplayed, isLayerOnTop);
        }
    }
}
