using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace InGameMap.Data
{
    public class MapDef
    {
        public float Rotation { get; set; }
        public Dictionary<string, MapLayerDef> Layers { get; set; } = new Dictionary<string, MapLayerDef>();
        public Dictionary<string, MapMarkerDef> StaticMarkers { get; set; } = new Dictionary<string, MapMarkerDef>();
        public List<Vector2> Bounds { get; set; } = new List<Vector2>();

        public static MapDef LoadFromPath(string relativePath)
        {
            var absolutePath = Path.Combine(Plugin.Path, relativePath);
            try
            {
                return JsonConvert.DeserializeObject<MapDef>(File.ReadAllText(absolutePath));
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
