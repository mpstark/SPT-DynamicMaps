using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.UI;
using EFT.UI;
using EFT.UI.Map;

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
            Settings.Init(Config);
            Config.SettingChanged += (x, y) => Map?.ReadConfig();

            Instance = this;

            // patches
            new CommonUIAwakePatch().Enable();
            new MapScreenShowPatch().Enable();
            new MapScreenClosePatch().Enable();
            new BattleUIScreenShowPatch().Enable();
            new GameWorldOnDestroyPatch().Enable();
            new AirdropBoxOnBoxLandPatch().Enable();
        }

        /// <summary>
        /// Attach to the map screen
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen mapScreen)
        {
            if (Map != null)
            {
                return;
            }

            Log.LogInfo("Trying to attach to MapScreen");

            // attach to common UI first to call awake and set things up, then attach to sleeping map screen
            Map = ModdedMapScreen.Create(Singleton<CommonUI>.Instance.gameObject);
            Map.transform.SetParent(mapScreen.transform);
        }

        /// <summary>
        /// Attach the peek component
        /// </summary>
        internal void TryAttachToBattleUIScreen(BattleUIScreen battleUI)
        {
            if (Map == null)
            {
                return;
            }

            Map.TryAddPeekComponent(battleUI);
        }
    }
}
