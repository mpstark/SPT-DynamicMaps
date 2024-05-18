using System.Collections.Generic;
using System.IO;
using Comfort.Common;
using EFT;
using InGameMap.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace InGameMap.Utils
{
    public static class GameUtils
    {
        public static bool IsInRaid()
        {
            var game = Singleton<AbstractGame>.Instance;
            return (game != null) && game.InRaid;
        }

        public static string GetCurrentMapInternalName()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer?.Location;
        }

        public static Player GetMainPlayer()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer;
        }

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

            var mapName = GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}.json"), dumpString);

            Plugin.Log.LogInfo("Dumped extracts");
        }
    }
}