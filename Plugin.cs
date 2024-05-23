using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT.UI.Map;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.UI;

namespace DynamicMaps
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.DynamicMaps", "DynamicMaps", BuildInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ModdedMapScreen Map;

        internal void Awake()
        {
            // TODO: implement config
            // Settings.Init(Config);
            // Config.SettingChanged += (x, y) => DynamicMaps?.ReadConfig();

            Instance = this;

            // patches
            new MapScreenShowPatch().Enable();
            // new GameWorldOnGameStartedPatch().Enable();
            new GameWorldOnDestroyPatch().Enable();
        }

        /// <summary>
        /// Attach to the map screen
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen screen)
        {
            if (Map != null)
            {
                return;
            }

            Map = ModdedMapScreen.Create(screen.gameObject);
        }
    }
}
