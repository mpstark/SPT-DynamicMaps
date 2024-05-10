using System.Collections.Generic;
using UnityEngine;

namespace InGameMap.Data
{
    public class LayerDef
    {
        public int LayerNumber { get; set; }
        public string ImagePath { get; set; }
        public List<Vector2> Bounds { get; set; } = new List<Vector2>();
    }
}
