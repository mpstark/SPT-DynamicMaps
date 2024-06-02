using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QuickMoveToContainer
namespace DynamicMaps.Config
{
    internal class Settings
    {
        public static ConfigFile Config;
        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public const string GeneralTitle = "1. General";
        public static ConfigEntry<bool> ReplaceMapScreen;

        public static ConfigEntry<KeyboardShortcut> CenterOnPlayerHotkey;
        public static ConfigEntry<KeyboardShortcut> DumpInfoHotkey;

        public static ConfigEntry<KeyboardShortcut> MoveMapUpHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapDownHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapLeftHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapRightHotkey;
        public static ConfigEntry<float> MapMoveHotkeySpeed;

        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelUpHotkey;
        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelDownHotkey;

        public static ConfigEntry<KeyboardShortcut> ZoomMapInHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomMapOutHotkey;
        public static ConfigEntry<float> ZoomMapHotkeySpeed;

        public const string DynamicMarkerTitle = "2. Dynamic Markers";
        public static ConfigEntry<bool> ShowPlayerMarker;

        public static ConfigEntry<bool> ShowFriendlyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowEnemyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowScavMarkersInRaid;
        public static ConfigEntry<bool> ShowBossMarkersInRaid;

        public static ConfigEntry<bool> ShowLockedDoorStatus;

        public static ConfigEntry<bool> ShowQuestsInRaid;

        public static ConfigEntry<bool> ShowExtractsInRaid;
        public static ConfigEntry<bool> ShowExtractStatusInRaid;

        public static ConfigEntry<bool> ShowDroppedBackpackInRaid;

        public static ConfigEntry<bool> ShowBTRInRaid;

        public static ConfigEntry<bool> ShowAirdropsInRaid;

        public static ConfigEntry<bool> ShowFriendlyCorpsesInRaid;
        public static ConfigEntry<bool> ShowKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowFriendlyKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowBossCorpsesInRaid;
        public static ConfigEntry<bool> ShowOtherCorpsesInRaid;

        public const string InRaidTitle = "3. In-Raid";
        public static ConfigEntry<bool> ResetZoomOnCenter;
        public static ConfigEntry<float> CenteringZoomResetPoint;

        public static ConfigEntry<bool> AutoCenterOnPlayerMarker;
        public static ConfigEntry<bool> AutoSelectLevel;

        public static ConfigEntry<KeyboardShortcut> PeekShortcut;
        public static ConfigEntry<bool> HoldForPeek;

        // public static ConfigEntry<KeyboardShortcut> KeyboardShortcut;

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(ReplaceMapScreen = Config.Bind(
                GeneralTitle,
                "Replace Map Screen",
                true,
                new ConfigDescription(
                    "If the map should replace the BSG default map screen, requires swapping away from modded map to refresh",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(CenterOnPlayerHotkey = Config.Bind(
                GeneralTitle,
                "Center on Player Hotkey",
                new KeyboardShortcut(KeyCode.Semicolon),
                new ConfigDescription(
                    "Pressed while the map is open, centers the player",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapUpHotkey = Config.Bind(
                GeneralTitle,
                "Move Map Up Hotkey",
                new KeyboardShortcut(KeyCode.UpArrow),
                new ConfigDescription(
                    "Hotkey to move the map up",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapDownHotkey = Config.Bind(
                GeneralTitle,
                "Move Map Down Hotkey",
                new KeyboardShortcut(KeyCode.DownArrow),
                new ConfigDescription(
                    "Hotkey to move the map down",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapLeftHotkey = Config.Bind(
                GeneralTitle,
                "Move Map Left Hotkey",
                new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    "Hotkey to move the map left",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapRightHotkey = Config.Bind(
                GeneralTitle,
                "Move Map Right Hotkey",
                new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    "Hotkey to move the map right",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MapMoveHotkeySpeed = Config.Bind(
                GeneralTitle,
                "Move Map Hotkey Speed",
                0.25f,
                new ConfigDescription(
                    "How fast the map should move, units are map percent per second",
                    new AcceptableValueRange<float>(0.05f, 2f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ChangeMapLevelUpHotkey = Config.Bind(
                GeneralTitle,
                "Change Map Level Up Hotkey",
                new KeyboardShortcut(KeyCode.Period),
                new ConfigDescription(
                    "Hotkey to move the map level up (shift-scroll-up also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ChangeMapLevelDownHotkey = Config.Bind(
                GeneralTitle,
                "Change Map Level Down Hotkey",
                new KeyboardShortcut(KeyCode.Comma),
                new ConfigDescription(
                    "Hotkey to move the map level down (shift-scroll-down also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapInHotkey = Config.Bind(
                GeneralTitle,
                "Zoom Map In Hotkey",
                new KeyboardShortcut(KeyCode.Equals),
                new ConfigDescription(
                    "Hotkey to zoom the map in (scroll-up also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapOutHotkey = Config.Bind(
                GeneralTitle,
                "Zoom Map Out Hotkey",
                new KeyboardShortcut(KeyCode.Minus),
                new ConfigDescription(
                    "Hotkey to zoom the map out (scroll-down also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapHotkeySpeed = Config.Bind(
                GeneralTitle,
                "Zoom Map Hotkey Speed",
                2.5f,
                new ConfigDescription(
                    "How fast the map should zoom by hotkey",
                    new AcceptableValueRange<float>(1f, 10f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(DumpInfoHotkey = Config.Bind(
                GeneralTitle,
                "Dump Info Hotkey",
                new KeyboardShortcut(KeyCode.D, KeyCode.LeftShift, KeyCode.LeftAlt),
                new ConfigDescription(
                    "Pressed while the map is open, dumps json MarkerDefs for extracts, loot, and switches into root of plugin folder",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            ConfigEntries.Add(ShowPlayerMarker = Config.Bind(
                DynamicMarkerTitle,
                "Show Player Marker",
                true,
                new ConfigDescription(
                    "If the player marker should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyPlayerMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Player Markers",
                true,
                new ConfigDescription(
                    "If friendly player markers should be shown in-raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowEnemyPlayerMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Enemy Player Markers",
                false,
                new ConfigDescription(
                    "If enemy player markers should be shown in-raid (generally for debug)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowScavMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Scav Markers",
                false,
                new ConfigDescription(
                    "If enemy scav markers should be shown in-raid (generally for debug)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBossMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Boss Markers",
                false,
                new ConfigDescription(
                    "If enemy boss markers should be shown in-raid",
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

            ConfigEntries.Add(ShowDroppedBackpackInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Dropped Backpack In Raid",
                true,
                new ConfigDescription(
                    "If the player's dropped backpacks (not anyone elses) should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBTRInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show BTR In Raid",
                true,
                new ConfigDescription(
                    "If the BTR should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowAirdropsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Airdrops In Raid",
                true,
                new ConfigDescription(
                    "If airdrops should be shown in raid when they land",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Corpses In Raid",
                true,
                new ConfigDescription(
                    "If friendly corpses should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowKilledCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Player-killed Corpses In Raid",
                true,
                new ConfigDescription(
                    "If corpses killed by the player should be shown in raid, killed bosses will be shown in another color",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyKilledCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Friendly-killed Corpses In Raid",
                true,
                new ConfigDescription(
                    "If corpses killed by friendly players should be shown in raid, killed bosses will be shown in another color",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBossCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Boss Corpses In Raid",
                false,
                new ConfigDescription(
                    "If boss corpses (other than ones killed by the player) should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowOtherCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "Show Other Corpses In Raid",
                false,
                new ConfigDescription(
                    "If corpses (other than friendly ones or ones killed by the player) should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AutoSelectLevel = Config.Bind(
                InRaidTitle,
                "Auto Select Level",
                true,
                new ConfigDescription(
                    "If the level should be automatically selected based on the players position in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AutoCenterOnPlayerMarker = Config.Bind(
                InRaidTitle,
                "Auto Center On Player Marker",
                false,
                new ConfigDescription(
                    "If the player marker should be centered when showing the map in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ResetZoomOnCenter = Config.Bind(
                InRaidTitle,
                "Reset Zoom On Center",
                true,
                new ConfigDescription(
                    "If the zoom level should be reset each time that the map is opened while in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(CenteringZoomResetPoint = Config.Bind(
                InRaidTitle,
                "Centering On Player Zoom Level",
                0.15f,
                new ConfigDescription(
                    "What zoom level should be used as while centering on the player (0 is fully zoomed out, and 1 is fully zoomed in)",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PeekShortcut = Config.Bind(
                InRaidTitle,
                "Peek at Map Shortcut",
                new KeyboardShortcut(KeyCode.M),
                new ConfigDescription(
                    "The keyboard shortcut to peek at the map",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(HoldForPeek = Config.Bind(
                InRaidTitle,
                "Hold for Peek",
                true,
                new ConfigDescription(
                    "If the shortcut should be held to keep it open. If disabled, button toggles",
                    null,
                    new ConfigurationManagerAttributes { })));

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
