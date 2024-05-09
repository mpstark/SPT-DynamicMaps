using System.IO;
using DG.Tweening;
using SimpleCrosshair.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private Image _image;
        private GameObject _mapContent;

        private float _zoomMin = 0.1f;
        private float _zoomMax = 10f;
        private float _zoom = 0.5f;
        private float _zoomScaler = 1.75f;
        private float _zoomTweenTime = 0.25f;

        private Vector2 _immediateAnchor = Vector2.zero;

        public void Update()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                OnScroll(scroll);
            }
        }

        private void OnScroll(float scroll)
        {
            var rectTransform = _mapContent.GetRectTransform();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, Input.mousePosition, null, out Vector2 relativePosition);

            var oldZoom = _zoom;
            var zoomDelta = scroll * _zoom * _zoomScaler;
            _zoom = Mathf.Clamp(_zoom + zoomDelta, _zoomMin, _zoomMax);
            zoomDelta = _zoom - oldZoom;

            if (_zoom != rectTransform.localScale.x)
            {
                _scrollRect.StopMovement();

                // check if tweening to update _immediateAnchor, since the scroll rect might have moved the anchor
                if (!DOTween.IsTweening(rectTransform, true))
                {
                    _immediateAnchor = rectTransform.GetRectTransform().anchoredPosition;
                }

                rectTransform.DOScale(_zoom * Vector3.one, _zoomTweenTime);

                // adjust position to new scroll, since we're moving towards cursor
                _immediateAnchor -= relativePosition * zoomDelta;
                rectTransform.DOAnchorPos(_immediateAnchor, _zoomTweenTime);
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
            scrollMaskGO.GetRectTransform().sizeDelta = gameObject.RectTransform().sizeDelta - new Vector2(0, 80f);
            scrollMaskGO.ResetRectTransform();
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f,0f,0f,1f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();
            _scrollRect.viewport = _scrollMask.GetRectTransform();

            // set up container for the scroll content
            _mapContent = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer));
            _mapContent.layer = gameObject.layer;
            _mapContent.transform.SetParent(scrollMaskGO.transform);
            _mapContent.ResetRectTransform();
            _mapContent.GetRectTransform().localScale = Vector3.one * _zoom;
            _scrollRect.content = _mapContent.GetRectTransform();

            // set base image
            var imageGO = new GameObject("BaseMap", typeof(RectTransform), typeof(CanvasRenderer));
            imageGO.layer = gameObject.layer;
            imageGO.transform.SetParent(_mapContent.transform);
            imageGO.ResetRectTransform();
            _image = imageGO.AddComponent<Image>();
            _image.type = Image.Type.Simple;
            _image.sprite = LoadSprite(Path.Combine(Plugin.Path, "Interchange-Ground_Level_medium.png"));

            imageGO.GetRectTransform().sizeDelta = new Vector2(_image.sprite.rect.width, _image.sprite.rect.height);
            _mapContent.GetRectTransform().sizeDelta = new Vector2(_image.sprite.rect.width, _image.sprite.rect.height);
        }


        private Sprite LoadSprite(string texturePath)
        {
            var texture = TextureUtils.LoadTexture2DFromPath(texturePath);
            var sprite = Sprite.Create(texture,
                                       new Rect(0f, 0f, texture.width, texture.height),
                                       new Vector2(texture.width / 2, texture.height / 2));
            return sprite;
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

        internal static ModdedMapScreen Create(GameObject parent)
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
