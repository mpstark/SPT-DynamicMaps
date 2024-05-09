using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QuickMoveToContainer
namespace SimpleCrosshair.Config
{
    internal class Settings
    {
        public static ConfigFile Config;
        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public const string GeneralTitle = "1. General";
        // public static ConfigEntry<string> ImageFileName;

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            // ConfigEntries.Add(Show = Config.Bind(
            //     GeneralTitle,
            //     "Show Crosshair",
            //     true,
            //     new ConfigDescription(
            //         "If the crosshair should be displayed on raid load, can still be toggled on by Toggle keybind",
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
