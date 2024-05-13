using System.Linq;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class MapLayer : MonoBehaviour
    {
        private static float _fadeMultiplierPerLayer = 0.5f;

        public delegate void LayerDisplayChangeHandler(bool isDisplayed, bool isOnTopLevel);
        public event LayerDisplayChangeHandler OnLayerDisplayChanged;

        public string Name { get; private set; }
        public Image Image { get; private set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public int Level => _def.Level;
        public bool IsDisplayed { get; private set; }
        public bool IsOnTopLevel { get; private set; }

        private MapLayerDef _def = new MapLayerDef();

        public static MapLayer Create(GameObject parent, string name, MapLayerDef def, float degreesRotation)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            var layer = go.AddComponent<MapLayer>();

            // convert 3d bounds to 2d ones
            var bounds2d = def.Bounds.Select(p => new Vector2(p.x, p.y));

            // set layer size
            var size = MathUtils.GetBoundingRectangle(bounds2d);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            rectTransform.sizeDelta = rotatedSize;

            // set layer offset
            var offset = MathUtils.GetMidpoint(bounds2d);
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

        public bool IsCoordInLayer(Vector2 coords, float height)
        {
            // TODO: there is probably a better way to do this
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            foreach (var bound in _def.Bounds)
            {
                minX = Mathf.Min(minX, bound.x);
                maxX = Mathf.Max(maxX, bound.x);
                minY = Mathf.Min(minY, bound.y);
                maxY = Mathf.Max(maxY, bound.y);
                minZ = Mathf.Min(minZ, bound.z);
                maxZ = Mathf.Max(maxZ, bound.z);
            }

            return coords.x > minX && coords.x < maxX
                && coords.y > minY && coords.y < maxY
                && height > minZ && height < maxZ;
        }

        public void OnTopLevelSelected(int newLevel)
        {
            var levelDelta = newLevel - Level;
            IsOnTopLevel = Level == newLevel;
            IsDisplayed = Level <= newLevel;

            // hide if not on the level or below
            gameObject.SetActive(IsDisplayed);

            // fade if level is lower than the new level according to difference in level
            var c = Mathf.Pow(_fadeMultiplierPerLayer, levelDelta);
            Image.color = new Color(c, c, c, 1);

            OnLayerDisplayChanged?.Invoke(IsDisplayed, IsOnTopLevel);
        }
    }
}
