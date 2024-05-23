using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DynamicMaps.Data
{
    public class BoundingRectangle
    {
        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }
    }

    public class BoundingRectangularSolid
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
    }

    public class MapLayerDef
    {
        [JsonRequired]
        public int Level { get; set; }

        [JsonRequired]
        public string ImagePath { get; set; }

        [JsonRequired]
        public BoundingRectangle ImageBounds { get; set; }

        [JsonRequired]
        public List<BoundingRectangularSolid> GameBounds { get; set; }
    }

    public class MapMarkerDef
    {
        public string Text { get; set; } = "";

        [JsonRequired]
        public string ImagePath { get; set; }

        [JsonRequired]
        public Vector3 Position { get; set; }

        public bool ShowInRaid { get; set; } = true;
        public string Category { get; set; } = "None";
        public Color Color { get; set; } = Color.white;
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public string AssociatedItemId { get; set; } = "";
    }

    public class MapLabelDef
    {
        [JsonRequired]
        public string Text { get; set; }

        [JsonRequired]
        public Vector3 Position { get; set; }

        public float FontSize { get; set; } = 14f;
        public float DegreesRotation { get; set; } = 0f;
        public Color Color { get; set; } = Color.white;
        public string Category { get; set; } = "None";
    }

    public class MapDef
    {
        [JsonRequired]
        public string DisplayName { get; set; }

        [JsonRequired]
        public BoundingRectangle Bounds { get; set; }

        [JsonRequired]
        public Dictionary<string, MapLayerDef> Layers { get; set; } = new Dictionary<string, MapLayerDef>();

        public List<string> MapInternalNames { get; set; } = new List<string>();
        public float CoordinateRotation { get; set; } = 0;
        public List<MapLabelDef> Labels { get; set; } = new List<MapLabelDef>();
        public List<MapMarkerDef> StaticMarkers { get; set; } = new List<MapMarkerDef>();
        public int DefaultLevel { get; set; } = 0;

        public string Author { get; set; }
        public string AuthorLink { get; set; }

        public static MapDef LoadFromPath(string absolutePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<MapDef>(File.ReadAllText(absolutePath));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Loading MapMappingDef failed from json at path: {absolutePath}");
                Plugin.Log.LogError($"Exception given was: {e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }

            return null;
        }
    }
}
