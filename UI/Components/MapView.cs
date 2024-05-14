using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.UI.Components
{
    public class MapView : MonoBehaviour
    {
        private static Vector2 _markerSize = new Vector2(30, 30);

        public event Action<int> OnLevelSelected;
        // public event Action<MapMarker> OnMarkerAdded;
        // public event Action OnMarkerRemoved;
        // public event Action<MapDef> OnMapLoaded;
        // public event Action<MapDef> OnMapUnloaded;

        public RectTransform RectTransform => gameObject.transform as RectTransform;
        public float CoordinateRotation { get; private set; }
        public int SelectedLevel { get; private set; }

        public GameObject MapMarkerContainer { get; private set; }
        public GameObject MapLabelsContainer { get; private set; }
        public GameObject MapLayerContainer { get; private set; }

        private Dictionary<string, GameObject> _mapMarkersCategories = new Dictionary<string, GameObject>();

        private List<MapMarker> _markers = new List<MapMarker>();
        private List<MapLayer> _layers = new List<MapLayer>();
        // private List<MapLabel> _labels = new List<MapLabel>();

        private MapDef _currentMapDef;

        public static MapView Create(GameObject parent, string name)
        {
            var go = UIUtils.CreateUIGameObject(parent, name);

            // TODO: this

            var view = go.AddComponent<MapView>();
            return view;
        }

        private void Awake()
        {
            MapLayerContainer = UIUtils.CreateUIGameObject(gameObject, "MapLayers");
            MapLabelsContainer = UIUtils.CreateUIGameObject(gameObject, "MapLabels");
            MapMarkerContainer = UIUtils.CreateUIGameObject(gameObject, "MapMarkers");

            // TODO: this
        }

        public void AddMapMarker(MapMarker marker)
        {
            if (_markers.Contains(marker))
            {
                return;
            }

            // TODO: create category go

            _markers.Add(marker);
        }

        public MapMarker AddMapMarker(string name, MapMarkerDef markerDef)
        {
            var marker = MapMarker.Create(MapMarkerContainer, name, markerDef, _markerSize, -CoordinateRotation);
            marker.LinkedLayer = _layers.FirstOrDefault(l => l.Name == markerDef.LinkedLayer);

            AddMapMarker(marker);
            return marker;
        }

        public IPlayerMapMarker AddPlayerMarker(IPlayer player)
        {
            var marker = IPlayerMapMarker.Create(player, MapMarkerContainer, "Markers/arrow.png",
                                                 "players", _markerSize);
            marker.TraversableLayers = _layers;
            marker.OnDeathOrDespawn += RemoveMapMarker;

            AddMapMarker(marker);

            return marker;
        }

        public void HideMapMarkerCategory(string category)
        {
            // TODO: this
        }

        public void RemoveMapMarker(MapMarker marker)
        {
            if (!_markers.Contains(marker))
            {
                return;
            }

            _markers.Remove(marker);
            marker.gameObject.SetActive(false);  // destroy not guaranteed to be called immediately
            GameObject.Destroy(marker.gameObject);
        }

        // TODO: this
        // public void AddMapLabel(MapLabel label)
        // public void RemoveMapLabel(MapLabel label)

        public void LoadMap(MapDef mapDef)
        {
            if (mapDef == null || _currentMapDef == mapDef)
            {
                return;
            }

            if (_currentMapDef != null)
            {
                UnloadMap();
            }

            _currentMapDef = mapDef;
            CoordinateRotation = mapDef.CoordinateRotation;

            // set width and height for top level
            var size = MathUtils.GetBoundingRectangle(mapDef.Bounds);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, CoordinateRotation);
            RectTransform.sizeDelta = rotatedSize;

            // set offset
            var offset = MathUtils.GetMidpoint(mapDef.Bounds);
            RectTransform.anchoredPosition = offset;

            // rotate all of the map content
            RectTransform.localRotation = Quaternion.Euler(0, 0, CoordinateRotation);

            // load all layers in the order of level
            foreach (var (layerName, layerDef) in mapDef.Layers.OrderBy(pair => pair.Value.Level))
            {
                var layer = MapLayer.Create(MapLayerContainer, layerName, layerDef, -CoordinateRotation);
                _layers.Add(layer);
            }

            // load all static map markers
            foreach (var (markerName, markerDef) in mapDef.StaticMarkers)
            {
                AddMapMarker(markerName, markerDef);
            }

            // TODO: load all static labels

            // select layer by the default level
            SelectTopLevel(mapDef.DefaultLevel);
        }

        public void UnloadMap()
        {
            if (_currentMapDef == null)
            {
                return;
            }

            var markersCopy = _markers.ToList();
            foreach (var marker in markersCopy)
            {
                RemoveMapMarker(marker);
            }
            markersCopy.Clear();

            // clear layers
            foreach (var layer in _layers)
            {
                GameObject.Destroy(layer.gameObject);
            }
            _layers.Clear();

            _currentMapDef = null;
        }

        public void SelectTopLevel(int level)
        {
            // go through each layer and change top level
            foreach (var layer in _layers)
            {
                layer.OnTopLevelSelected(level);
            }

            SelectedLevel = level;
            OnLevelSelected?.Invoke(level);
        }

        public void SelectLevelByCoords(Vector2 coords, float height)
        {
            var matchingLayer = FindMatchingLayerByCoords(coords, height);
            if (matchingLayer == null)
            {
                return;
            }

            SelectTopLevel(matchingLayer.Level);
        }

        private MapLayer FindMatchingLayerByCoords(Vector2 coord, float height)
        {
            // TODO: what if there are multiple matching?
            // probably want to "select" the smaller bounds one in that case
            return _layers.FirstOrDefault(l => l.IsCoordInLayer(coord, height));
        }
    }
}
