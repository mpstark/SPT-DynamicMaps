using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private static float _fadeMultiplierPerLayer = 0.5f;
        private static float _zoomScaler = 1.75f;
        private static float _zoomTweenTime = 0.25f;
        private static float _positionTweenTime = 0.25f;
        private static float _zoomMaxScaler = 10f;  // multiplier against zoomMin
        private static float _zoomMinScaler = 1.1f; // divider against ratio of screen

        private static Vector2 _markerSize = new Vector2(30, 30);
        private static Vector2 _levelSliderPosition = new Vector2(15f, 750f);
        private static Vector2 _mapSelectDropdownPosition = new Vector2(-780, -50);
        private static Vector2 _mapSelectDropdownSize = new Vector2(360, 31);

        private RectTransform _rectTransform;
        private RectTransform _parentTransform;
        private RectTransform _mapRectTransform;

        private GameObject _mapContentGO;
        private GameObject _mapLayersGO;
        private GameObject _mapMarkersGO;
        private ScrollRect _scrollRect;
        private Mask _scrollMask;

        private Dictionary<string, MapLayer> _layers = new Dictionary<string, MapLayer>();
        private Dictionary<string, MapMarker> _markers = new Dictionary<string, MapMarker>();

        private MapMapping _mapMapping;
        private List<MapDef> _mapDefs = new List<MapDef>();
        private MapDef _currentMapDef;

        private TextMeshProUGUI _playerPositionText;
        private TextMeshProUGUI _cursorPositionText;
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;

        private Vector2 _immediateMapAnchor = Vector2.zero;
        private float _zoomMin; // set when map loaded
        private float _zoomMax; // set when map loaded
        private float _zoomCurrent = 0.5f;
        private float _coordinateRotation = 0;

        private void Update()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                OnScroll(scroll);
            }

            if (Input.GetKeyDown(KeyCode.Semicolon))
            {
                if (_markers.ContainsKey("player"))
                {
                    var playerPosition = _markers["player"].RectTransform.anchoredPosition;
                    ShiftMapToCoord(playerPosition, _positionTweenTime);
                }
            }

            if (_cursorPositionText != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mapRectTransform, Input.mousePosition, null, out Vector2 mouseRelative);
                _cursorPositionText.text = $"Cursor: {mouseRelative.x:F} {mouseRelative.y:F}";
            }
        }

        private void OnScroll(float scroll)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapRectTransform, Input.mousePosition, null, out Vector2 mouseRelative);
            var rotatedRelative = MathUtils.GetRotatedVector2(mouseRelative, _coordinateRotation);

            var zoomDelta = scroll * _zoomCurrent * _zoomScaler;
            var zoomNew = Mathf.Clamp(_zoomCurrent + zoomDelta, _zoomMin, _zoomMax);
            var actualDelta = zoomNew - _zoomCurrent;

            // have to shift first, so that the tween is started in the shift first
            ShiftMap(-rotatedRelative * actualDelta, _zoomTweenTime);
            SetMapZoom(zoomNew, _zoomTweenTime);
        }

        public void SetMapZoom(float zoomNew, float tweenTime)
        {
            zoomNew = Mathf.Clamp(zoomNew, _zoomMin, _zoomMax);

            // already there
            if (zoomNew == _zoomCurrent)
            {
                return;
            }

            _zoomCurrent = zoomNew;

            // stop any movement that the scroll rect is doing because of momentum
            _scrollRect.StopMovement();

            // scale all map content up by scaling parent
            _mapRectTransform.DOScale(_zoomCurrent * Vector3.one, tweenTime);

            // inverse scale all map markers
            // FIXME: does this generate large amounts of garbage?
            var mapMarkers = _mapMarkersGO.transform.GetChildren();
            foreach (var mapMarker in mapMarkers)
            {
                mapMarker.DOScale(1 / _zoomCurrent * Vector3.one, tweenTime);
            }
        }

        public void ShiftMap(Vector2 shift, float tweenTime)
        {
            if (shift == Vector2.zero)
            {
                return;
            }

            // stop any movement that the scroll rect is doing because of momentum
            _scrollRect.StopMovement();

            // check if tweening to update _immediateMapAnchor, since the scroll rect might have moved the anchor
            if (!DOTween.IsTweening(_mapRectTransform, true))
            {
                _immediateMapAnchor = _mapRectTransform.anchoredPosition;
            }

            _immediateMapAnchor += shift;
            _mapRectTransform.DOAnchorPos(_immediateMapAnchor, tweenTime);
        }

        public void ShiftMapToCoord(Vector2 coord, float tweenTime)
        {
            var rotatedCoord = MathUtils.GetRotatedVector2(coord, _coordinateRotation);
            var currentCenter = _mapRectTransform.anchoredPosition / _zoomCurrent;
            ShiftMap((-rotatedCoord - currentCenter) * _zoomCurrent, tweenTime);
        }

        private void Awake()
        {
            _rectTransform = gameObject.GetRectTransform();
            _parentTransform = gameObject.transform.parent as RectTransform;

            // make our game object hierarchy
            var scrollRectGO = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGO = UIUtils.CreateUIGameObject(scrollRectGO, "ScrollMask");
            _mapContentGO = UIUtils.CreateUIGameObject(scrollMaskGO, "MapContent");
            _mapLayersGO = UIUtils.CreateUIGameObject(_mapContentGO, "MapLayers");
            _mapMarkersGO = UIUtils.CreateUIGameObject(_mapContentGO, "MapMarkers");
            _mapRectTransform = _mapContentGO.GetRectTransform();

            // set up mask; size will be set later in Raid/NoRaid
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            scrollMaskGO.GetRectTransform().sizeDelta = _rectTransform.sizeDelta - new Vector2(0, 80f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();

            // set up scroll rect
            scrollRectGO.GetRectTransform().sizeDelta = _rectTransform.sizeDelta;
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapRectTransform;

            CreatePositionTexts();

            // TODO: map mapping is dumb, load all json files in Maps\*.json instead and add map string to MapDef
            // load map mapping from file, load MapDefs, and load first one
            _mapMapping = MapMapping.LoadFromPath("maps.jsonc");
            foreach (var path in _mapMapping.GetMapDefPaths())
            {
                if (path.IsNullOrEmpty())
                {
                    continue;
                }

                _mapDefs.Add(MapDef.LoadFromPath(path));
            }

            // create map controls
            // level select slider
            var sliderPrefab = _parentTransform.Find("MapBlock/ZoomScroll").gameObject;
            _levelSelectSlider = LevelSelectSlider.Create(sliderPrefab, _rectTransform, _levelSliderPosition, SelectLayersByLevel);

            // map select dropdown, this will call LoadMap on the first option
            var selectPrefab = Singleton<CommonUI>.Instance.transform.Find(
                "Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            _mapSelectDropdown = MapSelectDropdown.Create(selectPrefab, _rectTransform, _mapSelectDropdownPosition, _mapSelectDropdownSize, LoadMap);
            _mapSelectDropdown.ChangeAvailableMapDefs(_mapDefs);
        }

        private void CreatePositionTexts()
        {
            var cursorPositionTextGO = UIUtils.CreateUIGameObject(gameObject, "CursorPositionText");
            _cursorPositionText = cursorPositionTextGO.AddComponent<TextMeshProUGUI>();
            _cursorPositionText.fontSize = 14;
            _cursorPositionText.GetRectTransform().anchorMin = new Vector2(0f, 1f);
            _cursorPositionText.GetRectTransform().anchorMax = new Vector2(0f, 1f);
            _cursorPositionText.GetRectTransform().anchoredPosition = new Vector2(15, -50);
            _cursorPositionText.alignment = TextAlignmentOptions.Left;

            var playerPositionTextGO = UIUtils.CreateUIGameObject(gameObject, "PlayerPositionText");
            _playerPositionText = playerPositionTextGO.AddComponent<TextMeshProUGUI>();
            _playerPositionText.fontSize = 14;
            _playerPositionText.GetRectTransform().anchorMin = new Vector2(0f, 1f);
            _playerPositionText.GetRectTransform().anchorMax = new Vector2(0f, 1f);
            _playerPositionText.GetRectTransform().anchoredPosition = new Vector2(15, -64);
            _playerPositionText.alignment = TextAlignmentOptions.Left;
        }

        private void LoadMap(MapDef mapDef)
        {
            if (mapDef == null || _currentMapDef == mapDef)
            {
                return;
            }

            if (_currentMapDef != null)
            {
                UnloadMap();
            }

            Plugin.Log.LogInfo($"Loading map {mapDef.DisplayName}");

            _currentMapDef = mapDef;
            _coordinateRotation = mapDef.CoordinateRotation;

            // set width and height for top level
            var size = MathUtils.GetBoundingRectangle(mapDef.Bounds);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, _coordinateRotation);
            _mapRectTransform.sizeDelta = rotatedSize;

            // set offset
            var offset = MathUtils.GetMidpoint(mapDef.Bounds);
            _mapRectTransform.anchoredPosition = offset;

            // set zoom min and max based on size of map and size of mask
            var maskSize = _scrollMask.GetRectTransform().sizeDelta;
            _zoomMin = Mathf.Min(maskSize.x / rotatedSize.x, maskSize.y / rotatedSize.y) / _zoomMinScaler;
            _zoomMax = _zoomMaxScaler * _zoomMin;

            // rotate all of the map content
            _mapRectTransform.localRotation = Quaternion.Euler(0, 0, _coordinateRotation);

            // load all layers
            foreach (var (layerName, layerDef) in mapDef.Layers)
            {
                _layers[layerName] = MapLayer.Create(_mapLayersGO, layerName, layerDef, -_coordinateRotation);
            }

            // set layer order
            int i = 0;
            foreach (var layer in _layers.Values.OrderBy(l => l.Level))
            {
                layer.RectTransform.SetSiblingIndex(i++);
            }

            foreach (var (name, markerDef) in mapDef.StaticMarkers)
            {
                _markers[name] = MapMarker.Create(_mapMarkersGO, name, markerDef, _markerSize, -_coordinateRotation);
            }

            _levelSelectSlider.OnMapLoaded(mapDef);
            _mapSelectDropdown.OnMapLoaded(mapDef);

            // this will set everything up for initial zoom
            SetMapZoom(_zoomMin, 0);

            // select layer by the default level
            SelectLayersByLevel(mapDef.DefaultLevel);

            // shift map by the offset to center it in the scroll mask
            ShiftMap(_scrollMask.GetRectTransform().anchoredPosition * _zoomCurrent, 0);
        }

        private void UnloadMap()
        {
            _immediateMapAnchor = Vector2.zero;

            // clear markers
            foreach (var marker in _markers.Values)
            {
                GameObject.Destroy(marker.gameObject);
            }
            _markers.Clear();

            // clear layers
            foreach (var layer in _layers.Values)
            {
                GameObject.Destroy(layer.gameObject);
            }
            _layers.Clear();
        }

        private void SelectLayersByLevel(int level)
        {
            // go through each layer and set fade color
            foreach (var layer in _layers.Values)
            {
                // show layer if at or below the current level
                layer.gameObject.SetActive(layer.Level <= level);

                // fade other layers according to difference in level
                var c = Mathf.Pow(_fadeMultiplierPerLayer, level - layer.Level);
                layer.Image.color = new Color(c, c, c, 1);
            }

            // go through all markers and call OnLayerSelect
            foreach (var marker in _markers.Values)
            {
                foreach (var (layerName, layer) in _layers)
                {
                    marker.OnLayerSelect(layerName, layer.Level == level);
                }
            }

            _levelSelectSlider.SelectedLevel = level;
        }

        private void SelectLayersByCoords(Vector2 coords, float height)
        {
            // TODO: better select that shows only layers in coords
            foreach(var layer in _layers.Values)
            {
                if (height > layer.HeightBounds.x && height < layer.HeightBounds.y)
                {
                    SelectLayersByLevel(layer.Level);
                    return;
                }
            }
        }

        private void ShowInRaid(LocalGame game)
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = new Vector2(0, -22);
            _scrollMask.GetRectTransform().sizeDelta = _rectTransform.sizeDelta - new Vector2(0, 40f);

            // hide map selector and make sure that current map is loaded
            _mapSelectDropdown.gameObject.SetActive(false);
            // TODO: make sure that the current map is loaded

            // TODO: this should be in another method
            // FIXME: on exit raid will struggle with transform not being valid
            // create player marker if one doesn't already exist
            var player = game.PlayerOwner.Player;
            // if (!_markers.ContainsKey("player"))
            // {
            //     _markers["player"] = TransformMarker.Create(player.CameraPosition, _mapMarkersGO, "Markers/arrow.png",
            //                                                 "players", _markerSize, 1/_zoomCurrent);
            //     _markers["player"].Image.color = Color.green;
            // }

            // test bot markers
            // var gameWorld = Singleton<GameWorld>.Instance;
            // if (gameWorld.AllAlivePlayersList.Count > 1)
            // {
            //     foreach (var person in gameWorld.AllAlivePlayersList)
            //     {
            //         if (person.IsYourPlayer || _markers.ContainsKey(person.name))
            //         {
            //             continue;
            //         }

            //         _markers[person.name] = TransformMarker.Create(person.CameraPosition, _mapMarkersGO, "Markers/arrow.png",
            //                                                        "bots", _markerSize, 1/_zoomCurrent);
            //         _markers[person.name].Image.color = Color.red;
            //     }
            // }


            // select layers to show
            var player3dPos = player.CameraPosition.position;
            var player2dPos = new Vector2(player3dPos.x, player3dPos.z);
            SelectLayersByCoords(player2dPos, player3dPos.y);

            // shift map to player position
            ShiftMapToCoord(player2dPos, 0);

            // show and update text
            _playerPositionText.gameObject.SetActive(true);
            _playerPositionText.text = $"Player: {player3dPos.x:F} {player3dPos.z:F} {player3dPos.y:F}";
        }

        private void ShowOutOfRaid()
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = new Vector2(0, -5);
            _scrollMask.GetRectTransform().sizeDelta = _rectTransform.sizeDelta - new Vector2(0, 70f);

            // show map selector
            _mapSelectDropdown.gameObject.SetActive(true);

            // hide player position text
            _playerPositionText.gameObject.SetActive(false);
        }

        internal void Show()
        {
            transform.parent.Find("MapBlock").gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock").gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);
            gameObject.SetActive(true);

            // check if raid
            var game = Singleton<AbstractGame>.Instance;
            if (game != null && game is LocalGame)
            {
                var localGame = game as LocalGame;
                ShowInRaid(localGame);
                return;
            }

            ShowOutOfRaid();
        }

        internal void Close()
        {
            _parentTransform.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        internal static ModdedMapScreen AttachTo(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");

            // set width and height based on parent
            var rect = parent.GetRectTransform().rect;
            go.GetRectTransform().sizeDelta = new Vector2(rect.width, rect.height);

            return go.AddComponent<ModdedMapScreen>();
        }
    }
}
