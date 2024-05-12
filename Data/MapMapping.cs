using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace InGameMap.Data
{
    public class MapMapping
    {
        // TODO: have additional maps that can be alternates
        public Dictionary<string, List<string>> Maps { get; set; } = new Dictionary<string, List<string>>();

        public static MapMapping LoadFromPath(string relativePath)
        {
            var absolutePath = Path.Combine(Plugin.Path, relativePath);
            try
            {
                return JsonConvert.DeserializeObject<MapMapping>(File.ReadAllText(absolutePath));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Loading MapMappingDef failed from json at path: {absolutePath}");
                Plugin.Log.LogError($"Exception given was: {e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
                throw e;
            }
        }

        public IEnumerable<string> GetMapDefPaths()
        {
            var unique = new HashSet<string>();
            foreach (var (mapName, mapDefPaths) in Maps)
            {
                foreach (var mapDefPath in mapDefPaths)
                {
                    unique.Add(mapDefPath);
                }
            }

            return unique;
        }
    }
}
