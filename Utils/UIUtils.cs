using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.Utils
{
    public static class UIUtils
    {
        public static void ResetRectTransform(this GameObject gameObject)
        {
            var rectTransform = gameObject.transform as RectTransform;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        public static RectTransform GetRectTransform(this GameObject gameObject)
        {
            return gameObject.transform as RectTransform;
        }

        public static RectTransform GetRectTransform(this Component component)
        {
            return component.gameObject.transform as RectTransform;
        }

        public static Tween TweenColor(this Image image, Color to, float duration)
        {
            return DOTween.To(() => image.color, c => image.color = c, to, duration);
        }

        public static GameObject CreateUIGameObject(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            return go;
        }

        /// <summary>
        /// Annoyingly, sometimes setting the outline fails with an exception, particularly if stack originates in
        /// Unity C++ -- not sure why this happens, so just catch and try again in each thing that wants a outline
        /// </summary>
        public static bool TrySetTMPOutline(TextMeshProUGUI text)
        {
            if (text == null)
            {
                Plugin.Log.LogWarning($"TrySetTMPOutline: text cannot be null");
                return false;
            }

            try
            {
                text.outlineColor = new Color32(0, 0, 0, 255);
                text.outlineWidth = 0.15f;
                text.fontStyle = FontStyles.Bold;
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed at setting outline: {e.Message}");
                Plugin.Log.LogWarning($"{e.StackTrace}");
            }

            return false;
        }
    }
}
