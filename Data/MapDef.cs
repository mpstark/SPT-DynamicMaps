using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace InGameMap.Data
{
    public class MapLayerDef
    {
        public int Level { get; set; }
        public string ImagePath { get; set; }

        // 3d points, z is heights -- this is different than how unity does it
        public List<Vector3> Bounds { get; set; } = new List<Vector3>();
    }

    public class MapMarkerDef
    {
        public string ImagePath { get; set; }
        public Vector2 Position { get; set; }
        public string Category { get; set; }
        public string LinkedLayer { get; set; }
    }

    public class MapDef
    {
        public string DisplayName { get; set; }
        public List<string> MapInternalNames { get; set; } = new List<string>();
        public float CoordinateRotation { get; set; }
        public Dictionary<string, MapLayerDef> Layers { get; set; } = new Dictionary<string, MapLayerDef>();
        public Dictionary<string, MapMarkerDef> StaticMarkers { get; set; } = new Dictionary<string, MapMarkerDef>();
        public List<Vector2> Bounds { get; set; } = new List<Vector2>();
        public int DefaultLevel { get; set; } = 0;

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
