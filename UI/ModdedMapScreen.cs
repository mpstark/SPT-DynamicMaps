using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.UI.Controls;
using DynamicMaps.Utils;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private const string _mapRelPath = "Maps";

        private static float _positionTweenTime = 0.25f;
        private static float _scrollZoomScaler = 1.75f;

        private static Vector2 _levelSliderPosition = new Vector2(15f, 750f);
        private static Vector2 _mapSelectDropdownPosition = new Vector2(-780f, -50f);
        private static Vector2 _mapSelectDropdownSize = new Vector2(360f, 31f);
        private static Vector2 _maskSizeModifierInRaid = new Vector2(0, -42f);
        private static Vector2 _maskPositionInRaid = new Vector2(0, -20f);
        private static Vector2 _maskSizeModifierOutOfRaid = new Vector2(0, -70f);
        private static Vector2 _maskPositionOutOfRaid = new Vector2(0, -5f);
        private static Vector2 _textAnchor = new Vector2(0f, 1f);
        private static Vector2 _cursorPositionTextOffset = new Vector2(15f, -52f);
        private static Vector2 _playerPositionTextOffset = new Vector2(15f, -68f);
        private static float _positionTextFontSize = 15f;

        public bool IsReplacingMapScreen = true;
        public RectTransform RectTransform => gameObject.GetRectTransform();

        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private bool _lastShownInRaid = false;
        private bool _isShown = false;

        // map and transport mechanism
        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private MapView _mapView;

        // map controls
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;
        private CursorPositionText _cursorPositionText;
        private PlayerPositionText _playerPositionText;

        // dynamic map marker providers
        private Dictionary<Type, IDynamicMarkerProvider> _dynamicMarkerProviders = new Dictionary<Type, IDynamicMarkerProvider>();

        // config
        private bool _autoCenterOnPlayerMarker = true;
        private bool _autoSelectLevel = true;
        private bool _resetZoomOnCenter = false;
        private float _centeringZoomResetPoint = 0f;
        private bool _showPlayerMarker = true;
        private bool _showFriendlyPlayerMarkers = true;
        private bool _showEnemyPlayerMarkers = false;
        private bool _showScavMarkers = false;
        private bool _showLockedDoorStatus = true;
        private bool _showQuestsInRaid = true;
        private bool _showExtractsInRaid = true;
        private bool _showExtractStatusInRaid = true;
        private bool _showAirdropsInRaid = true;
        private KeyboardShortcut _centerPlayerShortcut;
        private KeyboardShortcut _dumpShortcut;

        internal static ModdedMapScreen Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");

            // set width and height based on parent
            var rect = parent.GetRectTransform().rect;
            go.GetRectTransform().sizeDelta = new Vector2(rect.width, rect.height);

            return go.AddComponent<ModdedMapScreen>();
        }

        private void Awake()
        {
            // make our game object hierarchy
            var scrollRectGO = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGO = UIUtils.CreateUIGameObject(scrollRectGO, "ScrollMask");

            _mapView = MapView.Create(scrollMaskGO, "MapView");

            // set up mask; size will be set later in Raid/NoRaid
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();
            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

            // set up scroll rect
            scrollRectGO.GetRectTransform().sizeDelta = RectTransform.sizeDelta;
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapView.RectTransform;

            // create map controls

            // level select slider
            var sliderPrefab = _parentTransform.Find("MapBlock/ZoomScroll").gameObject;
            _levelSelectSlider = LevelSelectSlider.Create(sliderPrefab, RectTransform, _levelSliderPosition);
            _levelSelectSlider.OnLevelSelectedBySlider += _mapView.SelectTopLevel;
            _mapView.OnLevelSelected += (level) => _levelSelectSlider.SelectedLevel = level;

            // map select dropdown, this will call LoadMap on the first option
            var selectPrefab = Singleton<CommonUI>.Instance.transform.Find(
                "Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            _mapSelectDropdown = MapSelectDropdown.Create(selectPrefab, RectTransform, _mapSelectDropdownPosition, _mapSelectDropdownSize);
            _mapSelectDropdown.OnMapSelected += ChangeMap;

            // texts
            _cursorPositionText = CursorPositionText.Create(gameObject, _mapView.RectTransform, _positionTextFontSize);
            _cursorPositionText.RectTransform.anchorMin = _textAnchor;
            _cursorPositionText.RectTransform.anchorMax = _textAnchor;
            _cursorPositionText.RectTransform.anchoredPosition = _cursorPositionTextOffset;

            _playerPositionText = PlayerPositionText.Create(gameObject, _positionTextFontSize);
            _playerPositionText.RectTransform.anchorMin = _textAnchor;
            _playerPositionText.RectTransform.anchorMax = _textAnchor;
            _playerPositionText.RectTransform.anchoredPosition = _playerPositionTextOffset;
            _playerPositionText.gameObject.SetActive(false);

            // read config before setting up marker providers
            ReadConfig();

            GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
        }

        private void OnDestroy()
        {
            GameWorldOnDestroyPatch.OnRaidEnd -= OnRaidEnd;
        }

        private void Update()
        {
            // because we have a scroll rect, it seems to eat OnScroll via IScrollHandler
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                if (!_mapSelectDropdown.isActiveAndEnabled || !_mapSelectDropdown.IsDropdownOpen())
                {
                    OnScroll(scroll);
                }
            }

            if (_centerPlayerShortcut.IsDown())
            {
                var player = GameUtils.GetMainPlayer();
                if (player != null)
                {
                    _mapView.ShiftMapToCoordinate(MathUtils.ConvertToMapPosition(player.Position), _positionTweenTime);
                }
            }

            if (_dumpShortcut.IsDown())
            {
                DumpUtils.DumpExtracts();
                DumpUtils.DumpSwitches();
                DumpUtils.DumpLocks();
            }
        }

        private void OnDisable()
        {
            _mapSelectDropdown?.TryCloseDropdown();

            // close isn't called when hidden
            if (GameUtils.IsInRaid())
            {
                OnHideInRaid();
            }
            else
            {
                OnHideOutOfRaid();
            }

            _isShown = false;
        }

        internal void Show()
        {
            _isShown = true;

            // make sure that the BSG map is disabled
            transform.parent.Find("MapBlock").gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock").gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);
            gameObject.SetActive(true);

            // populate map select dropdown
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);

            if (GameUtils.IsInRaid())
            {
                Plugin.Log.LogInfo("Showing map in raid");
                OnShowInRaid();
            }
            else
            {
                Plugin.Log.LogInfo("Showing map out-of-raid");
                OnShowOutOfRaid();
            }
        }

        internal void Close()
        {
            // not called when hidden
            gameObject.SetActive(false);
        }

        internal void OnRaidEnd()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnRaidEnd(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnRaidEnd");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnShowInRaid()
        {
            if (!_lastShownInRaid)
            {
                // ony do the first time that is shown in raid until shown out of raid
                _lastShownInRaid = true;

                // adjust mask
                _scrollMask.GetRectTransform().anchoredPosition = _maskPositionInRaid;
                _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierInRaid;

                // show player position text
                _playerPositionText.gameObject.SetActive(true);

                // GetOrAddComponent is a BSG extension method under GClass reference
                // var dotSpawner = GameUtils.GetMainPlayer().gameObject.GetOrAddComponent<PlayerDotSpawner>();
                // dotSpawner.MapView = _mapView;
            }

            var mapInternalName = GameUtils.GetCurrentMapInternalName();
            if (string.IsNullOrEmpty(mapInternalName))
            {
                return;
            }

            // filter dropdown to only maps containing the internal map name
            // this forces the load of the first of those
            _mapSelectDropdown.FilterByInternalMapName(mapInternalName);

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnShowInRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnShowInRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }

            // rest of this function needs player
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(player.Position);

            // select layers to show
            if (_autoSelectLevel)
            {
                _mapView.SelectLevelByCoords(mapPosition);
            }

            if (_autoCenterOnPlayerMarker)
            {
                // change zoom to desired level
                if (_resetZoomOnCenter)
                {
                    _mapView.SetMapZoom(GetInRaidStartingZoom(), 0);
                }

                // shift map to player position, Vector3 to Vector2 discards z
                _mapView.ShiftMapToCoordinate(mapPosition, 0);
            }
        }

        private void OnHideInRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnHideInRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnHideInRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnShowOutOfRaid()
        {
            if (_lastShownInRaid)
            {
                // only do if adjusting from viewing in raid

                // adjust mask
                _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
                _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

                // clear filter on dropdown
                _mapSelectDropdown.ClearFilter();

                // hide player position text
                _playerPositionText.gameObject.SetActive(false);

                _lastShownInRaid = false;
            }

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnShowOutOfRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnShowOutOfRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnHideOutOfRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnHideOutOfRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnHideOutOfRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnScroll(float scrollAmount)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (scrollAmount > 0)
                {
                    _levelSelectSlider.ChangeLevelBy(1);
                }
                else
                {
                    _levelSelectSlider.ChangeLevelBy(-1);
                }

                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            var zoomDelta = scrollAmount * _mapView.ZoomCurrent * _scrollZoomScaler;
            _mapView.IncrementalZoomInto(zoomDelta, mouseRelative);
        }

        internal void ReadConfig()
        {
            IsReplacingMapScreen = Settings.Enabled.Value;
            _centerPlayerShortcut = Settings.CenterOnPlayerHotkey.Value;
            _dumpShortcut = Settings.DumpInfoHotkey.Value;

            _autoCenterOnPlayerMarker = Settings.AutoCenterOnPlayerMarker.Value;
            _autoSelectLevel = Settings.AutoSelectLevel.Value;

            _resetZoomOnCenter = Settings.ResetZoomOnCenter.Value;
            _centeringZoomResetPoint = Settings.CenteringZoomResetPoint.Value;

            _showPlayerMarker = Settings.ShowPlayerMarker.Value;

            _showFriendlyPlayerMarkers = Settings.ShowFriendlyPlayerMarkers.Value;
            _showEnemyPlayerMarkers = Settings.ShowEnemyPlayerMarkers.Value;
            _showScavMarkers = Settings.ShowScavMarkers.Value;

            _showQuestsInRaid = Settings.ShowQuestsInRaid.Value;

            _showLockedDoorStatus = Settings.ShowLockedDoorStatus.Value;

            _showExtractsInRaid = Settings.ShowExtractsInRaid.Value;
            _showExtractStatusInRaid = Settings.ShowExtractStatusInRaid.Value;

            _showAirdropsInRaid = Settings.ShowAirdropsInRaid.Value;

            AddRemoveMarkerProvider<PlayerMarkerProvider>(_showPlayerMarker);
            AddRemoveMarkerProvider<QuestMarkerProvider>(_showQuestsInRaid);
            AddRemoveMarkerProvider<LockedDoorMarkerMutator>(_showLockedDoorStatus);
            AddRemoveMarkerProvider<AirdropMarkerProvider>(_showAirdropsInRaid);

            // extracts
            AddRemoveMarkerProvider<ExtractMarkerProvider>(_showExtractsInRaid);
            if (_showExtractsInRaid)
            {
                var provider = GetMarkerProvider<ExtractMarkerProvider>();
                provider.ShowExtractStatusInRaid = _showExtractStatusInRaid;
            }

            // other player markers
            var needOtherPlayerMarkers = _showFriendlyPlayerMarkers || _showEnemyPlayerMarkers || _showScavMarkers;
            AddRemoveMarkerProvider<OtherPlayersMarkerProvider>(needOtherPlayerMarkers);

            if (needOtherPlayerMarkers)
            {
                var provider = GetMarkerProvider<OtherPlayersMarkerProvider>();
                provider.ShowFriendlyPlayers = _showFriendlyPlayerMarkers;
                provider.ShowEnemyPlayers = _showEnemyPlayerMarkers;
                provider.ShowScavs = _showScavMarkers;
            }
        }

        private void AddRemoveMarkerProvider<T>(bool status) where T : IDynamicMarkerProvider, new()
        {
            if (status && !_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)] = new T();

                // if the map is shown, need to call OnShowXXXX
                if (_isShown && GameUtils.IsInRaid())
                {
                    _dynamicMarkerProviders[typeof(T)].OnShowInRaid(_mapView);
                }
                else if (_isShown && !GameUtils.IsInRaid())
                {
                    _dynamicMarkerProviders[typeof(T)].OnShowOutOfRaid(_mapView);
                }
            }
            else if (!status && _dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)].OnDisable(_mapView);
                _dynamicMarkerProviders.Remove(typeof(T));
            }
        }

        private T GetMarkerProvider<T>() where T : IDynamicMarkerProvider
        {
            if (!_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                return default;
            }

            return (T)_dynamicMarkerProviders[typeof(T)];
        }

        private float GetInRaidStartingZoom()
        {
            var startingZoom = _mapView.ZoomMin;
            startingZoom += _centeringZoomResetPoint * (_mapView.ZoomMax - _mapView.ZoomMin);

            return startingZoom;
        }

        private void ChangeMap(MapDef mapDef)
        {
            if (mapDef == null || _mapView.CurrentMapDef == mapDef)
            {
                return;
            }

            Plugin.Log.LogInfo($"MapScreen: Loading map {mapDef.DisplayName}");

            _mapView.LoadMap(mapDef);

            _mapSelectDropdown.OnLoadMap(mapDef);
            _levelSelectSlider.OnLoadMap(mapDef, _mapView.SelectedLevel);

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnMapChanged(_mapView, mapDef);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in ChangeMap");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }
    }
}
