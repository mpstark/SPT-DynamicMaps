using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT.UI;
using InGameMap.Config;
using InGameMap.Patches;

namespace InGameMap
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.InGameMap", "InGameMap", BuildInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        internal void Awake()
        {
            Settings.Init(Config);
            // Config.SettingChanged += (x, y) => InGameMap?.ReadConfig();

            Instance = this;

            // patches
            new MapScreenShowPatch().Enable();
        }

        /// <summary>
        /// Attach tp the map
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen screen)
        {
        }
    }
}
