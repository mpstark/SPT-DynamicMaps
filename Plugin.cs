using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;
using InGameMap.Config;
using InGameMap.Patches;
using InGameMap.UI;

namespace InGameMap
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.InGameMap", "InGameMap", BuildInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private FieldInfo AppMainMenuController = typeof(TarkovApplication).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First(f => f.FieldType == typeof(MainMenuController));
        private FieldInfo InventoryControllerTab = AccessTools.Field(typeof(InventoryScreen.GClass3116), nameof(InventoryScreen.GClass3116.InventoryTab));

        public ModdedMapScreen Map;

        internal void Awake()
        {
            Settings.Init(Config);
            // Config.SettingChanged += (x, y) => InGameMap?.ReadConfig();

            Instance = this;

            // patches
            new MapScreenShowPatch().Enable();
        }

        /* waste of time m shortcut
        internal void Update()
        {
            if (Settings.KeyboardShortcut.Value.IsDown())
            {
                // check if we have a current selected game object that should block, like inputfield
                var selected = EventSystem.current?.currentSelectedGameObject;
                if (selected != null && selected.TryGetComponent<TMP_InputField>(out var component))
                {
                    return;
                }

                OpenMapScreen();
            }
        }

        internal void OpenMapScreen()
        {
            // mainmenu => InventoryScreen.GClass3117
            // hideout => InventoryScreen.GClass3118, though 3117 seems to work
            // raid => InventoryScreen.GClass3120

            var mapScreen = Singleton<CommonUI>.Instance.GetComponentInChildren<MapScreen>();
            if (mapScreen.gameObject.activeInHierarchy)
            {
                return;
            }

            InventoryScreen.GClass3116 inventoryController;

            var game = Singleton<AbstractGame>.Instance;
            if (game != null && game is LocalGame)
            {
                // player in raid, use that inventory screen controller
                var owner = (game as LocalGame).PlayerOwner;
                var viewType = (owner.Player.BtrState == EPlayerBtrState.Inside) ? EItemViewType.InventoryWithoutDiscard : EItemViewType.Inventory;
                inventoryController = new InventoryScreen.GClass3120(
                    owner.Session,
                    owner.Player.Profile,
                    owner.Player.HealthController,
                    owner.Player.GClass2761_0,
                    owner.Player.GClass3204_0,
                    owner.Player.GClass3208_0,
                    null,
                    InventoryScreen.EInventoryTab.Map,
                    false,
                    viewType);

                owner.Player.SetInventoryOpened(true);
                inventoryController.OnClose += () => owner.Player.SetInventoryOpened(false);
            }
            else
            {
                // use main menu inventory screen controller
                var mainMenuController = AppMainMenuController.GetValue(ClientAppUtils.GetMainApp()) as MainMenuController;
                inventoryController = mainMenuController.method_23();
                InventoryControllerTab.SetValue(inventoryController, InventoryScreen.EInventoryTab.Map);
            }

            inventoryController.ShowScreen(EFT.UI.Screens.EScreenState.Queued);
        }
        */

        /// <summary>
        /// Attach to the map
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen screen)
        {
            if (Map != null)
            {
                return;
            }

            Map = ModdedMapScreen.AttachTo(screen.gameObject);
        }
    }
}
