using BepInEx.Configuration;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal class MapMiniComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        public RectTransform MapScreenTrueParent { get; set; }
        public RectTransform RectTransform { get; private set; }
        public KeyboardShortcut ActivateMiniMap { get; set; }
        public bool IsActive { get; private set; }

        internal static MapMiniComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MiniMap");
            go.GetRectTransform().sizeDelta = parent.GetRectTransform().sizeDelta;
            var component = go.AddComponent<MapMiniComponent>();
            return component;
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
        }

        private void Start()
        {
            BeginMiniMap();
        }

        private void Update()
        {
            if (ActivateMiniMap.BetterIsDown())
            {
                if (!IsActive)
                {
                    BeginMiniMap();
                }
                else
                {
                    EndMiniMap();
                }
            }
        }

        internal void BeginMiniMap()
        {
            IsActive = true;

            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show();
        }

        internal void EndMiniMap()
        {
            MapScreen.WasMinimapActive = MapScreen.IsMinimapActive;

            IsActive = false;
            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);
        }
    }
}
