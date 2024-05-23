using System.Collections.Generic;
using BepInEx.Configuration;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QuickMoveToContainer
namespace DynamicMaps.Config
{
    internal class Settings
    {
        public static ConfigFile Config;
        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public const string GeneralTitle = "1. General";
        public static ConfigEntry<KeyboardShortcut> KeyboardShortcut;

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(KeyboardShortcut = Config.Bind(
                GeneralTitle,
                "Keyboard Shortcut",
                new KeyboardShortcut(UnityEngine.KeyCode.M),
                new ConfigDescription(
                    "The keyboard shortcut to open the map",
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
