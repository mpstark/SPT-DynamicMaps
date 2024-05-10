using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace InGameMap.Data
{
    public class MapMappingDef
    {
        Dictionary<string, string> MapToMapDefPath { get; set; } = new Dictionary<string, string>();

        public static MapMappingDef LoadFromPath(string relativePath)
        {
            var absolutePath = Path.Combine(Plugin.Path, relativePath);
            try
            {
                return JsonConvert.DeserializeObject<MapMappingDef>(File.ReadAllText(absolutePath));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Loading MapMappingDef failed from json at path: {absolutePath}");
                Plugin.Log.LogError($"Exception given was: {e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
                throw e;
            }
        }
    }
}
