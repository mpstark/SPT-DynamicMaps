using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class MapLayer : MonoBehaviour
    {
        private static float _fadeMultiplierPerLayer = 0.5f;
        private static float _defaultLevelFallbackAlpha = 0.1f;

        public delegate void LayerDisplayChangeHandler(bool isDisplayed, bool isOnTopLevel);
        public event LayerDisplayChangeHandler OnLayerDisplayChanged;

        public string Name { get; private set; }
        public Image Image { get; private set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public int Level => _def.Level;
        public bool IsDisplayed { get; private set; }
        public bool IsOnTopLevel { get; private set; }
        public bool IsOnDefaultLevel { get; set; }

        private MapLayerDef _def = new MapLayerDef();

        public static MapLayer Create(GameObject parent, string name, MapLayerDef def, float degreesRotation)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            var layer = go.AddComponent<MapLayer>();

            // set layer size
            var size = def.ImageBounds.Max - def.ImageBounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            rectTransform.sizeDelta = rotatedSize;

            // set layer offset
            var offset = MathUtils.GetMidpoint(def.ImageBounds.Min, def.ImageBounds.Max);
            rectTransform.anchoredPosition = offset;

            // set rotation to combat when we rotate the whole map content
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            layer.Name = name;
            layer._def = def;

            // load image
            layer.Image = go.AddComponent<Image>();
            layer.Image.sprite = TextureUtils.GetOrLoadCachedSprite(def.ImagePath);
            layer.Image.type = Image.Type.Simple;

            return layer;
        }

        private void OnDestroy()
        {
            OnLayerDisplayChanged = null;
        }

        public bool IsCoordinateInLayer(Vector3 coordinate)
        {
            foreach (var gameBound in _def.GameBounds)
            {
                if (coordinate.x > gameBound.Min.x && coordinate.x < gameBound.Max.x
                 && coordinate.y > gameBound.Min.y && coordinate.y < gameBound.Max.y
                 && coordinate.z > gameBound.Min.z && coordinate.z < gameBound.Max.z)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetMatchingBoundVolume(Vector3 coordinate)
        {
            // a bit scuffed formatting
            // this assumes that a layer wouldn't have multiple overlapping game bounds
            foreach (var gameBound in _def.GameBounds)
            {
                if (coordinate.x > gameBound.Min.x && coordinate.x < gameBound.Max.x
                 && coordinate.y > gameBound.Min.y && coordinate.y < gameBound.Max.y
                 && coordinate.z > gameBound.Min.z && coordinate.z < gameBound.Max.z)
                {
                    return (gameBound.Max.x - gameBound.Min.x) *
                           (gameBound.Max.y - gameBound.Min.y) *
                           (gameBound.Max.z - gameBound.Min.z);
                }
            }

            return float.MinValue;
        }

        public void OnTopLevelSelected(int newLevel)
        {
            var levelDelta = newLevel - Level;
            IsOnTopLevel = Level == newLevel;
            IsDisplayed = Level <= newLevel;

            // hide if not on the level or below
            gameObject.SetActive(IsDisplayed || IsOnDefaultLevel);

            // change alpha for default level if not only displayed because of it
            var a = 1f;
            if (Level > newLevel && IsOnDefaultLevel)
            {
                a = _defaultLevelFallbackAlpha;
            }

            // fade if level is lower than the new level according to difference in level
            var c = Mathf.Pow(_fadeMultiplierPerLayer, levelDelta);
            Image.color = new Color(c, c, c, a);

            OnLayerDisplayChanged?.Invoke(IsDisplayed, IsOnTopLevel);
        }
    }
}
