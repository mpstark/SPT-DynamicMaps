using BepInEx.Configuration;
using DynamicMaps.Utils;
using EFT.UI.Map;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        public RectTransform MapScreenTrueParent { get; set; }

        public RectTransform RectTransform { get; private set; }
        public KeyboardShortcut PeekShortcut { get; set; }
        public bool HoldForPeek { get; set; }  // opposite is peek toggle
        public bool IsPeeking { get; private set; }

        internal static MapPeekComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            go.GetRectTransform().sizeDelta = parent.GetRectTransform().sizeDelta;

            var component = go.AddComponent<MapPeekComponent>();

            return component;
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
            IsPeeking = false;
        }

        private void Update()
        {
            if (HoldForPeek && PeekShortcut.BetterIsPressed() != IsPeeking)
            {
                // hold for peek
                if (PeekShortcut.BetterIsPressed())
                {
                    BeginPeek();
                }
                else
                {
                    EndPeek();
                }
            }
            else if (!HoldForPeek && PeekShortcut.BetterIsDown())
            {
                // toggle peek
                if (!IsPeeking)
                {
                    BeginPeek();
                }
                else
                {
                    EndPeek();
                }
            }
        }

        internal void BeginPeek()
        {
            if (IsPeeking)
            {
                return;
            }
            MapScreen.WasMinimapActive = MapScreen.IsMinimapActive;

            if (MapScreen.IsMinimapActive && MapScreen.MiniMapComponent != null)
            {
                MapScreen.MiniMapComponent.EndMiniMap();
            }

            // just in case something else is attached and tries to be in front
            transform.SetAsLastSibling();

            IsPeeking = true;

            // attach map screen to peek mask
            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show();
        }

        internal void EndPeek()
        {
            if (!IsPeeking)
            {
                return;
            }

            IsPeeking = false;

            // un-attach map screen and re-attach to true parent
            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);

            if (MapScreen.WasMinimapActive && MapScreen.MiniMapComponent != null)
            {
                MapScreen.MiniMapComponent.BeginMiniMap();
            }

            MapScreen.WasMinimapActive = MapScreen.IsMinimapActive;
        }
    }
}
