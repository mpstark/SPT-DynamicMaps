using DynamicMaps.Utils;
using TMPro;
using UnityEngine;

namespace DynamicMaps.UI.Controls
{
    public abstract class AbstractTextControl : MonoBehaviour
    {
        public TextMeshProUGUI Text { get; protected set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private bool _hasSetOutline = false;

        public static T Create<T>(GameObject parent, string name, float fontSize) where T : AbstractTextControl
        {
            var go = UIUtils.CreateUIGameObject(parent, name);

            var textControl = go.AddComponent<T>();
            textControl.Text = go.AddComponent<TextMeshProUGUI>();
            textControl.Text.autoSizeTextContainer = true;
            textControl.Text.fontSize = fontSize;
            textControl.Text.alignment = TextAlignmentOptions.Left;

            textControl._hasSetOutline = UIUtils.TrySetTMPOutline(textControl.Text);

            return textControl;
        }

        private void OnEnable()
        {
            if (_hasSetOutline || Text == null)
            {
                return;
            }

            _hasSetOutline = UIUtils.TrySetTMPOutline(Text);
            Text.text = Text.text;  // try resetting text, since it seems like if outline fails, it doesn't size properly
        }
    }
}
