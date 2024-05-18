using System.Collections.Generic;
using System.IO;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using InGameMap.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace InGameMap.Utils
{
    public static class DumpUtils
    {
        public static void DumpExtracts()
        {
            Plugin.Log.LogInfo("Trying to dump extracts");

            var gameWorld = Singleton<GameWorld>.Instance;
            var scavExfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints;
            var pmcExfils = gameWorld.ExfiltrationController.ExfiltrationPoints;

            var dump = new List<MapMarkerDef>();

            foreach (var scavExfil in scavExfils)
            {
                var dumped = new MapMarkerDef
                {
                    Category = "ScavExtract-OutOfRaid",
                    ImagePath = "Markers/exit.png",
                    Text = scavExfil.Settings.Name.Localized(),
                    Position = MathUtils.TransformToMapPosition(scavExfil.transform),
                    Color = Color.Lerp(Color.yellow, Color.red, 0.5f)
                };

                dump.Add(dumped);
            }

            foreach (var pmcExfil in pmcExfils)
            {
                var dumped = new MapMarkerDef
                {
                    Category = "PMCExtract-OutOfRaid",
                    ImagePath = "Markers/exit.png",
                    Text = pmcExfil.Settings.Name.Localized(),
                    Position = MathUtils.TransformToMapPosition(pmcExfil.transform),
                    Color = Color.green
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
                    Category = "Switch",
                    ImagePath = "Markers/lever.png",
                    Text = @switch.name,
                    Position = MathUtils.TransformToMapPosition(@switch.transform)
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

            foreach (var @object in objects)
            {
                if (@object.KeyId.IsNullOrEmpty())
                {
                    continue;
                }

                bool isDoor = @object.TypeKey.Contains("door");

                var dumped = new MapMarkerDef
                {
                    Text = @object.name,
                    Category = isDoor ? "LockedDoor" : "Locked",
                    ImagePath = isDoor ? "Markers/locked_door.png" : "Markers/lock.png",
                    Position = MathUtils.TransformToMapPosition(@object.transform),
                    ExtraInfo = @object.KeyId,
                    Color = Color.yellow
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
