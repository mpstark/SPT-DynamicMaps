using System.Collections.Generic;
using System.IO;
using Comfort.Common;
using DynamicMaps.Data;
using EFT;
using EFT.Interactive;
using Newtonsoft.Json;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class DumpUtils
    {
        private const string _extractCategory = "Extract";
        private const string _extractImagePath = "Markers/exit.png";
        private static Color _extractScavColor = Color.Lerp(Color.yellow, Color.red, 0.5f);
        private static Color _extractPMCColor = Color.green;

        private const string _switchCategory = "Switch";
        private const string _switchImagePath = "Markers/lever.png";

        private const string _lockedDoorCategory = "Locked Door";
        private const string _lockedDoorImagePath = "Markers/door_with_lock.png";
        private static Color _lockedDoorColor = Color.yellow;

        public static void DumpExtracts()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var scavExfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints;
            var pmcExfils = gameWorld.ExfiltrationController.ExfiltrationPoints;

            var dump = new List<MapMarkerDef>();

            foreach (var scavExfil in scavExfils)
            {
                var dumped = new MapMarkerDef
                {
                    Category = _extractCategory,
                    ShowInRaid = false,
                    ImagePath = _extractImagePath,
                    Text = scavExfil.Settings.Name.BSGLocalized(),
                    Position = MathUtils.ConvertToMapPosition(scavExfil.transform),
                    Color = _extractScavColor
                };

                dump.Add(dumped);
            }

            foreach (var pmcExfil in pmcExfils)
            {
                var dumped = new MapMarkerDef
                {
                    Category = _extractCategory,
                    ShowInRaid = false,
                    ImagePath = _extractImagePath,
                    Text = pmcExfil.Settings.Name.BSGLocalized(),
                    Position = MathUtils.ConvertToMapPosition(pmcExfil.transform),
                    Color = _extractPMCColor
                };

                dump.Add(dumped);
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump, Formatting.Indented);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}-extracts.json"), dumpString);

            Plugin.Log.LogInfo("Dumped extracts");
        }

        public static void DumpSwitches()
        {
            var switches = GameObject.FindObjectsOfType<Switch>();
            var dump = new List<MapMarkerDef>();

            foreach (var @switch in switches)
            {
                if (!@switch.Operatable || !@switch.HasAuthority)
                {
                    continue;
                }

                var dumped = new MapMarkerDef
                {
                    Category = _switchCategory,
                    ImagePath = _switchImagePath,
                    Text = @switch.name,
                    Position = MathUtils.ConvertToMapPosition(@switch.transform)
                };

                dump.Add(dumped);
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump, Formatting.Indented);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}-switches.json"), dumpString);

            Plugin.Log.LogInfo("Dumped switches");
        }

        public static void DumpLocks()
        {
            var objects = GameObject.FindObjectsOfType<WorldInteractiveObject>();
            var dump = new List<MapMarkerDef>();
            var i = 1;

            foreach (var locked in objects)
            {
                if (string.IsNullOrEmpty(locked.KeyId) || !locked.Operatable)
                {
                    continue;
                }

                var dumped = new MapMarkerDef
                {
                    Text = $"door {i++}",
                    Category = _lockedDoorCategory,
                    ImagePath = _lockedDoorImagePath,
                    Position = MathUtils.ConvertToMapPosition(locked.transform),
                    AssociatedItemId = locked.KeyId,
                    Color = _lockedDoorColor
                };

                dump.Add(dumped);
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump, Formatting.Indented);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}-locked.json"), dumpString);

            Plugin.Log.LogInfo("Dumped locks");
        }
    }
}
