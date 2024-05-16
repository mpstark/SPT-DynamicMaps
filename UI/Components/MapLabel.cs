using InGameMap.Data;
using InGameMap.Utils;
using TMPro;
using UnityEngine;

namespace InGameMap.UI.Components
{
    public class MapLabel : MonoBehaviour
    {
        public string Text { get; protected set; }
        public string Category { get; protected set; }
        public Vector3 Position { get; protected set;}

        public TextMeshProUGUI Label { get; protected set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

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

        private Color _color = Color.white;

        public static MapLabel Create(GameObject parent, MapLabelDef def, float degreesRotation, float scale)
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapLabel {def.Text}");

            var rectTransform = go.GetRectTransform();
            rectTransform.anchoredPosition = def.Position;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            var label = go.AddComponent<MapLabel>();
            label.Text = def.Text;
            label.Category = def.Category;
            label.Position = def.Position;

            label.Label = go.AddComponent<TextMeshProUGUI>();
            label.Color = def.Color;
            label.Label.autoSizeTextContainer = true;
            label.Label.fontSize = def.FontSize;
            label.Label.alignment = TextAlignmentOptions.Center;
            label.Label.outlineColor = new Color32(0, 0, 0, 255);
            label.Label.outlineWidth = 0.15f;
            label.Label.fontStyle = FontStyles.Bold; // for some reason this refreshes the outline and applies the outline
            label.Label.text = label.Text;

            return label;
        }

        public void OnContainingLayerChanged(bool isLayerDisplayed, bool isLayerOnTop)
        {
            Label.color = GetLayerAdjustedLabelColor(isLayerDisplayed, isLayerOnTop);
        }

        private Color GetLayerAdjustedLabelColor(bool isLayerDisplayed, bool isLayerOnTop)
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

            return new Color(Label.color.r, Label.color.g, Label.color.b, alpha);
        }
    }
}
