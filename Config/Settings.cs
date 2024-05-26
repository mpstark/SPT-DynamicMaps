using System.Collections.Generic;
using BepInEx.Configuration;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QuickMoveToContainer
namespace DynamicMaps.Config
{
    internal class Settings
    {
        public static ConfigFile Config;
        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public const string MapTitle = "1. In Raid Map Settings";
        public static ConfigEntry<bool> ResetZoomOnCenter;
        public static ConfigEntry<float> CenteringZoomResetPoint;

        public static ConfigEntry<bool> AutoCenterOnPlayerMarker;
        public static ConfigEntry<bool> AutoSelectLevel;

        public const string DynamicMarkerTitle = "2. Dynamic Markers";
        public static ConfigEntry<bool> ShowPlayerMarker;

        public static ConfigEntry<bool> ShowFriendlyPlayerMarkers;
        public static ConfigEntry<bool> ShowEnemyPlayerMarkers;
        public static ConfigEntry<bool> ShowScavMarkers;

        public static ConfigEntry<bool> ShowLockedDoorStatus;

        public static ConfigEntry<bool> ShowQuestsInRaid;

        public static ConfigEntry<bool> ShowExtractsInRaid;
        public static ConfigEntry<bool> ShowExtractStatusInRaid;

        // public static ConfigEntry<KeyboardShortcut> KeyboardShortcut;

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(AutoSelectLevel = Config.Bind(
                MapTitle,
                "Auto Select Level",
                true,
                new ConfigDescription(
                    "If the level should be automatically selected based on the players position in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AutoCenterOnPlayerMarker = Config.Bind(
                MapTitle,
                "Auto Center On Player Marker",
                true,
                new ConfigDescription(
                    "If the player marker should be centered when showing the map in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ResetZoomOnCenter = Config.Bind(
                MapTitle,
                "Reset Zoom On Center",
                true,
                new ConfigDescription(
                    "If the zoom level should be reset each time that the map is opened while in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(CenteringZoomResetPoint = Config.Bind(
                MapTitle,
                "Centering On Player Zoom Level",
                0.25f,
                new ConfigDescription(
                    "What zoom level should be used as while centering on the player (0 is fully zoomed out, and 1 is fully zoomed in)",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowPlayerMarker = Config.Bind(
                DynamicMarkerTitle,
                "Show Player Marker",
                true,
                new ConfigDescription(
                    "If the player marker should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyPlayerMarkers = Config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Player Markers",
                true,
                new ConfigDescription(
                    "If friendly player markers should be shown",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowEnemyPlayerMarkers = Config.Bind(
                DynamicMarkerTitle,
                "Show Enemy Player Markers",
                false,
                new ConfigDescription(
                    "If enemy player markers should be shown",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowScavMarkers = Config.Bind(
                DynamicMarkerTitle,
                "Show Scav Markers",
                false,
                new ConfigDescription(
                    "If enemy scav markers should be shown",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowLockedDoorStatus = Config.Bind(
                DynamicMarkerTitle,
                "Show Locked Door Status",
                true,
                new ConfigDescription(
                    "If locked door markers should be updated with status based on key acquisition",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowQuestsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Quests In Raid",
                true,
                new ConfigDescription(
                    "If quests should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowExtractsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Extracts In Raid",
                true,
                new ConfigDescription(
                    "If extracts should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowExtractStatusInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Extracts Status In Raid",
                true,
                new ConfigDescription(
                    "If extracts should be colored according to their status in raid",
                    null,
                    new ConfigurationManagerAttributes { })));


            // ConfigEntries.Add(KeyboardShortcut = Config.Bind(
            //     GeneralTitle,
            //     "Keyboard Shortcut",
            //     new KeyboardShortcut(UnityEngine.KeyCode.M),
            //     new ConfigDescription(
            //         "The keyboard shortcut to open the map",
            //         null,
            //         new ConfigurationManagerAttributes { })));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                var attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
