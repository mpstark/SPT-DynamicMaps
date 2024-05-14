using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EFT;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.UI.Components
{
    public class MapView : MonoBehaviour
    {
        private static Vector2 _markerSize = new Vector2(30, 30);
        private static float _zoomTweenTime = 0.25f;
        private static float _zoomMaxScaler = 10f;  // multiplier against zoomMin
        private static float _zoomMinScaler = 1.1f; // divider against ratio of a provided rect

        public event Action<int> OnLevelSelected;

        // TODO: do we need these?
        // public event Action<MapMarker> OnMarkerAdded;
        // public event Action OnMarkerRemoved;
        // public event Action<MapDef> OnMapLoaded;
        // public event Action<MapDef> OnMapUnloaded;

        public RectTransform RectTransform => gameObject.transform as RectTransform;
        public MapDef CurrentMapDef { get; private set; }
        public float CoordinateRotation { get; private set; }
        public int SelectedLevel { get; private set; }

        public GameObject MapMarkerContainer { get; private set; }
        public GameObject MapLabelsContainer { get; private set; }
        public GameObject MapLayerContainer { get; private set; }

        public float ZoomMin { get; private set; }      // set when map loaded
        public float ZoomMax { get; private set; }      // set when map loaded
        public float ZoomCurrent { get; private set; }  // set when map loaded

        private Vector2 _immediateMapAnchor = Vector2.zero;

        private List<MapMarker> _markers = new List<MapMarker>();
        private List<MapLayer> _layers = new List<MapLayer>();
        // private List<MapLabel> _labels = new List<MapLabel>();

        public static MapView Create(GameObject parent, string name)
        {
            var go = UIUtils.CreateUIGameObject(parent, name);
            var view = go.AddComponent<MapView>();
            return view;
        }

        private void Awake()
        {
            MapLayerContainer = UIUtils.CreateUIGameObject(gameObject, "MapLayers");
            MapLabelsContainer = UIUtils.CreateUIGameObject(gameObject, "MapLabels");
            MapMarkerContainer = UIUtils.CreateUIGameObject(gameObject, "MapMarkers");
        }

        public void AddMapMarker(MapMarker marker)
        {
            if (_markers.Contains(marker))
            {
                return;
            }

            marker.gameObject.transform.localScale = (1 / ZoomCurrent) * Vector3.one;

            // hook marker position changed event up, so that when markers change position, they get notified
            // about layer status
            marker.OnPositionChanged += UpdateMarkerLayerStatus;
            UpdateMarkerLayerStatus(marker);  // call immediately

            _markers.Add(marker);
        }

        public MapMarker AddMapMarker(string name, MapMarkerDef markerDef)
        {
            var marker = MapMarker.Create(MapMarkerContainer, name, markerDef, _markerSize, -CoordinateRotation);

            AddMapMarker(marker);
            return marker;
        }

        public PlayerMapMarker AddPlayerMarker(IPlayer player)
        {
            var marker = PlayerMapMarker.Create(player, MapMarkerContainer, "Markers/arrow.png",
                                                 "players", _markerSize);
            marker.OnDeathOrDespawn += RemoveMapMarker;

            AddMapMarker(marker);
            return marker;
        }

        public void UpdateMarkerLayerStatus(MapMarker marker)
        {
            var layer = FindMatchingLayerByCoordinate(marker.Position);
            marker.OnContainingLayerChanged(layer.IsDisplayed, layer.IsOnTopLevel);
        }

        public void UpdateMarkersLayer()
        {
            // TODO: revisit
            foreach (var marker in _markers)
            {
                UpdateMarkerLayerStatus(marker);
            }
        }

        public void ChangeMarkerCategoryStatus(string category, bool status)
        {
            foreach (var marker in _markers)
            {
                if (marker.Category != category)
                {
                    continue;
                }

                marker.gameObject.SetActive(status);
            }
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
            if (mapDef == null || CurrentMapDef == mapDef)
            {
                return;
            }

            if (CurrentMapDef != null)
            {
                UnloadMap();
            }

            CurrentMapDef = mapDef;
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

            // set min/max zoom based on parent's rect transform
            SetMinMaxZoom(transform.parent as RectTransform);

            // load all layers in the order of level
            // BSG has extension method deconstruct for KVP, so have to do this
            foreach (var pair in mapDef.Layers.OrderBy(pair => pair.Value.Level))
            {
                var layerName = pair.Key;
                var layerDef = pair.Value;
                var layer = MapLayer.Create(MapLayerContainer, layerName, layerDef, -CoordinateRotation);
                _layers.Add(layer);
            }

            // select layer by the default level
            SelectTopLevel(mapDef.DefaultLevel);

            // load all static map markers
            // BSG has extension method deconstruct for KVP, so have to do this
            foreach (var pair in mapDef.StaticMarkers)
            {
                var markerName = pair.Key;
                var markerDef = pair.Value;
                AddMapMarker(markerName, markerDef);
            }

            // TODO: load all static labels
        }

        public void UnloadMap()
        {
            if (CurrentMapDef == null)
            {
                return;
            }

            // remove all markers and reset to empty
            var markersCopy = _markers.ToList();
            foreach (var marker in markersCopy)
            {
                RemoveMapMarker(marker);
            }
            markersCopy.Clear();
            _markers.Clear();

            // clear layers and reset to empty
            foreach (var layer in _layers)
            {
                GameObject.Destroy(layer.gameObject);
            }
            _layers.Clear();

            _immediateMapAnchor = Vector2.zero;
            CurrentMapDef = null;
        }

        public void SelectTopLevel(int level)
        {
            // go through each layer and change top level
            foreach (var layer in _layers)
            {
                layer.OnTopLevelSelected(level);
            }

            SelectedLevel = level;

            UpdateMarkersLayer();

            OnLevelSelected?.Invoke(level);
        }

        public void SelectLevelByCoords(Vector3 coords)
        {
            var matchingLayer = FindMatchingLayerByCoordinate(coords);
            if (matchingLayer == null)
            {
                return;
            }

            SelectTopLevel(matchingLayer.Level);
        }

        public void SetMinMaxZoom(RectTransform parentTransform)
        {
            // set zoom min and max based on size of map and size of mask
            var mapSize = RectTransform.sizeDelta;
            ZoomMin = Mathf.Min(parentTransform.sizeDelta.x / mapSize.x, parentTransform.sizeDelta.y / mapSize.y) / _zoomMinScaler;
            ZoomMax = _zoomMaxScaler * ZoomMin;

            // this will set everything up for initial zoom
            SetMapZoom(ZoomMin, 0);

            // shift map by the offset to center it in the scroll mask
            ShiftMap(parentTransform.anchoredPosition * ZoomCurrent, 0);
        }

        public void SetMapZoom(float zoomNew, float tweenTime)
        {
            zoomNew = Mathf.Clamp(zoomNew, ZoomMin, ZoomMax);

            // already there
            if (zoomNew == ZoomCurrent)
            {
                return;
            }

            ZoomCurrent = zoomNew;

            // scale all map content up by scaling parent
            RectTransform.DOScale(ZoomCurrent * Vector3.one, tweenTime);

            // inverse scale all map markers
            // THIS SEEMS GROSS!
            // FIXME: does this generate large amounts of garbage?
            foreach (var marker in _markers)
            {
                marker.RectTransform.DOScale(1 / ZoomCurrent * Vector3.one, tweenTime);
            }
        }

        public void IncrementalZoomInto(float zoomDelta, Vector2 rectPoint)
        {
            var zoomNew = Mathf.Clamp(ZoomCurrent + zoomDelta, ZoomMin, ZoomMax);
            var actualDelta = zoomNew - ZoomCurrent;
            var rotatedPoint = MathUtils.GetRotatedVector2(rectPoint, CoordinateRotation);

            // have to shift first, so that the tween is started in the shift first
            ShiftMap(-rotatedPoint * actualDelta, _zoomTweenTime);
            SetMapZoom(zoomNew, _zoomTweenTime);
        }

        public void ShiftMap(Vector2 shift, float tweenTime)
        {
            if (shift == Vector2.zero)
            {
                return;
            }

            // check if tweening to update _immediateMapAnchor, since the scroll rect might have moved the anchor
            if (!DOTween.IsTweening(RectTransform, true))
            {
                _immediateMapAnchor = RectTransform.anchoredPosition;
            }

            _immediateMapAnchor += shift;
            RectTransform.DOAnchorPos(_immediateMapAnchor, tweenTime);
        }

        public void ShiftMapToCoordinate(Vector2 coord, float tweenTime)
        {
            var rotatedCoord = MathUtils.GetRotatedVector2(coord, CoordinateRotation);
            var currentCenter = RectTransform.anchoredPosition / ZoomCurrent;
            ShiftMap((-rotatedCoord - currentCenter) * ZoomCurrent, tweenTime);
        }

        private MapLayer FindMatchingLayerByCoordinate(Vector3 coordinate)
        {
            // TODO: what if there are multiple matching?
            // probably want to "select" the smaller bounds one in that case
            return _layers.FirstOrDefault(l => l.IsCoordinateInLayer(coordinate));
        }
    }
}
