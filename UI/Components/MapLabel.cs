using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using TMPro;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class MapLabel : MonoBehaviour, ILayerBound
    {
        public string Text { get; protected set; }
        public string Category { get; protected set; }

        public TextMeshProUGUI Label { get; protected set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private Vector3 _position;
        public Vector3 Position
        {
            get
            {
                return _position;
            }

            set
            {
                gameObject.GetRectTransform().anchoredPosition = value;
                _position = value;
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
                Label.color = value;
            }
        }

        public Dictionary<LayerStatus, float> LabelAlphaLayerStatus { get; set; } = new Dictionary<LayerStatus, float>
            {
                {LayerStatus.Hidden, 0.0f},
                {LayerStatus.Underneath, 0.0f},
                {LayerStatus.OnTop, 1f},
                {LayerStatus.FullReveal, 1f},
            };

        private bool _hasSetOutline = false;

        public static MapLabel Create(GameObject parent, MapLabelDef def, float degreesRotation, float scale)
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapLabel {def.Text}");

            var rectTransform = go.GetRectTransform();
            rectTransform.anchoredPosition = def.Position;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation - def.DegreesRotation);

            var label = go.AddComponent<MapLabel>();
            label.Text = def.Text;
            label.Category = def.Category;
            label.Position = def.Position;

            label.Label = go.AddComponent<TextMeshProUGUI>();
            label.Color = def.Color;
            label.Label.autoSizeTextContainer = true;
            label.Label.fontSize = def.FontSize;
            label.Label.alignment = TextAlignmentOptions.Center;
            label.Label.text = label.Text;

            label._hasSetOutline = UIUtils.TrySetTMPOutline(label.Label);

            return label;
        }

        private void OnEnable()
        {
            if (_hasSetOutline || Label == null)
            {
                return;
            }

            _hasSetOutline = UIUtils.TrySetTMPOutline(Label);
            Label.text = Label.text;  // try resetting text, since it seems like if outline fails, it doesn't size properly
        }

        public void HandleNewLayerStatus(LayerStatus status)
        {
            Label.color = new Color(Label.color.r, Label.color.g, Label.color.b, LabelAlphaLayerStatus[status]);
        }
    }
}
