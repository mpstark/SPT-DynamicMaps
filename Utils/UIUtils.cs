using BepInEx.Configuration;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleCrosshair.Utils
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
            return component.transform as RectTransform;
        }

        public static void ResetTransform(this GameObject gameObject)
        {
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
		    gameObject.transform.localScale = Vector3.one;
        }

        public static Tween TweenColor(this Image image, Color to, float duration)
        {
            return DOTween.To(() => image.color, c => image.color = c, to, duration);
        }

        /// <summary>
        /// KeyboardShortcut.IsPressed is awful and doesn't allow other buttons to be pressed during
        /// </summary>
        public static bool BetterIsPressed(this KeyboardShortcut key)
        {
            if (!Input.GetKey(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
