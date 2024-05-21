using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using InGameMap.Data;
using InGameMap.DynamicMarkers;
using InGameMap.UI.Components;
using InGameMap.UI.Controls;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
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

        public RectTransform RectTransform => gameObject.GetRectTransform();
        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private bool _lastShownInRaid = false;

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
        private List<IDynamicMarkerProvider> _dynamicMarkerProviders = new List<IDynamicMarkerProvider>();

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

            // add dynamic marker providers
            _dynamicMarkerProviders.Add(new PlayerMarkerProvider());
            _dynamicMarkerProviders.Add(new OtherPlayersMarkerProvider());
            _dynamicMarkerProviders.Add(new ExtractMarkerProvider());
        }

        private void Update()
        {
            // because we have a scroll rect, it seems to eat OnScroll via IScrollHandler
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                OnScroll(scroll);
            }

            if (Input.GetKeyDown(KeyCode.Semicolon))
            {
                var player = GameUtils.GetMainPlayer();
                if (player != null)
                {
                    _mapView.ShiftMapToCoordinate(MathUtils.ConvertToMapPosition(player.Position), _positionTweenTime);
                }
            }

            if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt))
            {
                DumpUtils.DumpExtracts();
                DumpUtils.DumpSwitches();
                DumpUtils.DumpLocks();
            }
        }

        private void OnDisable()
        {
            // close isn't called when hidden
            if (GameUtils.IsInRaid())
            {
                OnHideInRaid();
            }
            else
            {
                OnHideOutOfRaid();
            }
        }

        internal void Show()
        {
            // make sure that the BSG map is disabled
            transform.parent.Find("MapBlock").gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock").gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);
            gameObject.SetActive(true);

            // populate map select dropdown
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);

            if (GameUtils.IsInRaid())
            {
                OnShowInRaid();
            }
            else
            {
                OnShowOutOfRaid();
            }
        }

        internal void Close()
        {
            // not called when hidden
            _parentTransform.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        internal void OnRaidEnd()
        {
            Plugin.Log.LogInfo($"OnRaidEnd");

            foreach (var dynamicProvider in _dynamicMarkerProviders)
            {
                dynamicProvider.OnRaidEnd(_mapView);
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

                // GetOrAddComponent is a BSG extension method under gclass reference
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

            foreach (var dynamicProvider in _dynamicMarkerProviders)
            {
                dynamicProvider.OnShowInRaid(_mapView, mapInternalName);
            }

            // rest of this function needs player
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            // select layers to show
            _mapView.SelectLevelByCoords(MathUtils.ConvertToMapPosition(player.Position));

            // shift map to player position, Vector3 to Vector2 discards z
            // TODO: this is annoying, but need something like it
            // _mapView.ShiftMapToCoordinate(mapPosition, 0);
        }

        private void OnHideInRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders)
            {
                dynamicProvider.OnHideInRaid(_mapView);
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

            foreach (var dynamicProvider in _dynamicMarkerProviders)
            {
                dynamicProvider.OnShowOutOfRaid(_mapView);
            }
        }

        private void OnHideOutOfRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders)
            {
                dynamicProvider.OnHideOutOfRaid(_mapView);
            }
        }

        private void OnScroll(float scrollAmount)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            var zoomDelta = scrollAmount * _mapView.ZoomCurrent * _scrollZoomScaler;
            _mapView.IncrementalZoomInto(zoomDelta, mouseRelative);
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

            foreach (var dynamicProvider in _dynamicMarkerProviders)
            {
                dynamicProvider.OnMapChanged(_mapView, mapDef);
            }
        }
    }
}
