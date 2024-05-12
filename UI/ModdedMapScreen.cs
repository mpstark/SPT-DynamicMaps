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
        private static Vector2 _markerSize = new Vector2(16, 16);

        private RectTransform _rectTransform;
        private RectTransform _parentTransform;
        private RectTransform _mapRectTransform;

        private GameObject _mapContentGO;
        private GameObject _mapLayersGO;
        private GameObject _mapMarkersGO;
        private ScrollRect _scrollRect;
        private Mask _scrollMask;

        private DropDownBox _mapSelectDropdown;
        private MapMapping _mapMapping;
        private List<MapDef> _mapDefs = new List<MapDef>();
        private MapDef _currentMapDef;

        private TextMeshProUGUI _playerPositionText;
        private TextMeshProUGUI _cursorPositionText;

        private Dictionary<string, MapLayer> _layers = new Dictionary<string, MapLayer>();
        private Dictionary<string, MapMarker> _markers = new Dictionary<string, MapMarker>();

        private Scrollbar _mapLevelScrollbar;
        private TextMeshProUGUI _levelText;
        private List<int> _levels = new List<int>();
        private int _selectedLevel = int.MinValue;

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
            // scrollRectGO.AddComponent<CanvasRenderer>();
            scrollRectGO.GetRectTransform().sizeDelta = _rectTransform.sizeDelta;
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapRectTransform;

            // create map controls
            CreateMapSelectDropdown();
            CreateLevelSelectScrollbar();
            CreatePositionTexts();

            // TODO: map mapping is dumb, load all json files in Maps\*.json instead and add map string to mapdef
            // load map mapping from file, load mapdefs, and load first one
            _mapMapping = MapMapping.LoadFromPath("maps.jsonc");
            foreach (var path in _mapMapping.GetMapDefPaths())
            {
                if (path.IsNullOrEmpty())
                {
                    continue;
                }

                _mapDefs.Add(MapDef.LoadFromPath(path));
            }

            RefreshMapSelectDropdown();
            _mapSelectDropdown.OnValueChanged.Bind(OnSelectDropdownMap, 0);
        }

        private void CreateLevelSelectScrollbar()
        {
            var prefab = _parentTransform.Find("MapBlock/ZoomScroll").gameObject;
            var scrollbarGO = Instantiate(prefab);
            scrollbarGO.name = "LevelSelectScrollbar";
            scrollbarGO.transform.SetParent(_rectTransform);
            scrollbarGO.transform.localScale = Vector3.one;

            // position to top left
            var oldPosition = scrollbarGO.GetRectTransform().anchoredPosition;
            scrollbarGO.GetRectTransform().anchoredPosition = new Vector2(oldPosition.x, 750f);

            // remove useless component
            Destroy(scrollbarGO.GetComponent<MapZoomer>());

            // create layer text
            var slidingArea = scrollbarGO.transform.Find("Scrollbar/Sliding Area/Handle").gameObject;
            var layerTextGO = UIUtils.CreateUIGameObject(slidingArea, "SlidingLayerText");
            _levelText = layerTextGO.AddComponent<TextMeshProUGUI>();
            _levelText.fontSize = 14;
            _levelText.GetRectTransform().anchoredPosition = new Vector2(110, -17);

            // setup the scrollbar component
            var actualScrollbarGO = scrollbarGO.transform.Find("Scrollbar").gameObject;
            _mapLevelScrollbar = actualScrollbarGO.GetComponent<Scrollbar>();
            _mapLevelScrollbar.direction = Scrollbar.Direction.BottomToTop;
            _mapLevelScrollbar.onValueChanged.AddListener(OnLevelScrollValueChanged);
        }

        private void OnLevelScrollValueChanged(float newValue)
        {
            var levelIndex = Mathf.RoundToInt(newValue * (_levels.Count - 1));
            var level = _levels[levelIndex];
            if (_selectedLevel != level)
            {
                SelectLayersByLevel(level);
            }
        }

        private void SetLevelScrollValue(int level)
        {
            _mapLevelScrollbar.value = _levels.IndexOf(level) / (_levels.Count - 1f);
            _levelText.text = $"Level {level}";
        }

        private void CreateMapSelectDropdown()
        {
            // TODO: this
            var prefab = Singleton<CommonUI>.Instance.transform.Find("Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            var dropdownGO = Instantiate(prefab);
            dropdownGO.name = "MapSelectDropdown";

            var rectTransform = dropdownGO.GetRectTransform();
            rectTransform.SetParent(_rectTransform);
            rectTransform.localScale = Vector3.one;
            var oldSize = rectTransform.sizeDelta;
            rectTransform.sizeDelta = new Vector2(360, oldSize.y);
            rectTransform.anchoredPosition = new Vector2(-780, -50);  // this is lazy, prob should adjust all of the anchors

            _mapSelectDropdown = dropdownGO.GetComponentInChildren<DropDownBox>();
        }

        private void RefreshMapSelectDropdown()
        {
            _mapSelectDropdown.Show(_mapDefs.Select(def => def.DisplayName));
        }

        private void ChangeMapSelectedInDropdown(MapDef selected)
        {
            _mapSelectDropdown.UpdateValue(_mapDefs.IndexOf(selected));
        }

        private void OnSelectDropdownMap(int index)
        {
            LoadMap(_mapDefs[index]);
        }

        private void CreatePositionTexts()
        {
            var cursorPositionTextGO = UIUtils.CreateUIGameObject(gameObject, "CursorPositionText");
            _cursorPositionText = cursorPositionTextGO.AddComponent<TextMeshProUGUI>();
            _cursorPositionText.fontSize = 14;
            _cursorPositionText.GetRectTransform().anchoredPosition = new Vector2(-850, 485);
            _cursorPositionText.alignment = TextAlignmentOptions.Left;

            var playerPositionTextGO = UIUtils.CreateUIGameObject(gameObject, "PlayerPositionText");
            _playerPositionText = playerPositionTextGO.AddComponent<TextMeshProUGUI>();
            _playerPositionText.fontSize = 14;
            _playerPositionText.GetRectTransform().anchoredPosition = new Vector2(-850, 471);
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
            ChangeMapSelectedInDropdown(mapDef);
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
                _layers[layerName] = new MapLayer(_mapLayersGO, layerName, layerDef, -_coordinateRotation);

                // FIXME: this probably allocates more than I want?
                if (!_levels.Contains(layerDef.Level))
                {
                    _levels.Add(layerDef.Level);
                }
            }

            // set layer order
            int i = 0;
            foreach (var layer in _layers.Values.OrderBy(l => l.Level))
            {
                layer.RectTransform.SetSiblingIndex(i++);
            }

            // set number of levels into the level scrollbar
            _levels.Sort();
            _mapLevelScrollbar.numberOfSteps = _levels.Count();

            foreach (var (name, markerDef) in mapDef.StaticMarkers)
            {
                _markers[name] = new MapMarker(_mapMarkersGO, name, markerDef, _markerSize, -_coordinateRotation);
            }

            // this will set everything up for initial zoom
            SetMapZoom(_zoomMin, 0);

            // select layer by the default level
            SelectLayersByLevel(mapDef.DefaultLevel);

            // shift map by the offset to center it in the scroll mask
            ShiftMap(_scrollMask.GetRectTransform().anchoredPosition * _zoomCurrent, 0);
        }

        private void UnloadMap()
        {
            _levels.Clear();
            _selectedLevel = int.MinValue;
            _immediateMapAnchor = Vector2.zero;

            // clear markers
            foreach (var (name, marker) in _markers)
            {
                marker.Destroy();
            }
            _markers.Clear();

            // clear layers
            foreach (var (name, layer) in _layers)
            {
                layer.Destroy();
            }
            _layers.Clear();
        }

        private void SelectLayersByLevel(int level)
        {
            if (_selectedLevel == level)
            {
                return;
            }

            _selectedLevel = level;
            SetLevelScrollValue(_selectedLevel);

            // go through each layer and set fade color
            foreach (var (layerName, layer) in _layers)
            {
                // show layer if at or below the current level
                layer.GameObject.SetActive(layer.Level <= level);

                // fade other layers according to difference in level
                var c = Mathf.Pow(_fadeMultiplierPerLayer, level - layer.Level);
                layer.Image.color = new Color(c, c, c, 1);
            }

            // go through all markers and call OnLayerSelect
            foreach (var (markerName, marker) in _markers)
            {
                foreach (var (layerName, layer) in _layers)
                {
                    marker.OnLayerSelect(layerName, layer.Level == level);
                }
            }
        }

        private void SelectLayersByCoords(Vector2 coords, float height)
        {
            // TODO: better select that shows only layers in coords
            foreach(var (name, layer) in _layers)
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

            // create player marker if one doesn't already exist
            var player = game.PlayerOwner.Player;
            if (!_markers.ContainsKey("player"))
            {
                // TODO: this seems gross
                _markers["player"] = new MapMarker(_mapMarkersGO, "player", "player", "Markers\\arrow.png",
                                                    new Vector2(0f, 0f), _markerSize, -_coordinateRotation, 1 / _zoomCurrent);
                _markers["player"].Image.color = Color.green;
            }

            // move player marker
            var player3dPos = player.CameraPosition.position;
            var player2dPos = new Vector2(player3dPos.x, player3dPos.z);
            var angles = player.CameraPosition.eulerAngles;
            _markers["player"].MoveAndRotate(player2dPos, -angles.y); // I'm unsure why negative rotation here

            // select layers to show
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
