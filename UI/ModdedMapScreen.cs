using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using InGameMap.UI.Controls;
using InGameMap.UI.Components;
using TMPro;
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
        private static Vector2 _mapSelectDropdownPosition = new Vector2(-780, -50);
        private static Vector2 _mapSelectDropdownSize = new Vector2(360, 31);

        public RectTransform RectTransform => gameObject.GetRectTransform();
        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private MapView _mapView;

        private TextMeshProUGUI _playerPositionText;
        private TextMeshProUGUI _cursorPositionText;
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;

        // TODO: remove this and put it somewhere else
        private PlayerMapMarker _playerMarker;
        private Dictionary<IPlayer, PlayerMapMarker> _otherPlayers = new Dictionary<IPlayer, PlayerMapMarker>();

        private void Update()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                OnScroll(scroll);
            }

            if (Input.GetKeyDown(KeyCode.Semicolon))
            {
                if (_playerMarker != null)
                {
                    var playerPosition = _playerMarker.RectTransform.anchoredPosition;
                    _mapView.ShiftMapToCoordinate(playerPosition, _positionTweenTime);
                }
            }

            if (_cursorPositionText != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);
                _cursorPositionText.text = $"Cursor: {mouseRelative.x:F} {mouseRelative.y:F}";
            }
        }

        private void OnScroll(float scrollAmount)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            var zoomDelta = scrollAmount * _mapView.ZoomCurrent * _scrollZoomScaler;
            _mapView.IncrementalZoomInto(zoomDelta, mouseRelative);
        }

        private void Awake()
        {
            // make our game object hierarchy
            var scrollRectGO = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGO = UIUtils.CreateUIGameObject(scrollRectGO, "ScrollMask");

            _mapView = MapView.Create(scrollMaskGO, "MapContent");

            // set up mask; size will be set later in Raid/NoRaid
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            scrollMaskGO.GetRectTransform().sizeDelta = RectTransform.sizeDelta - new Vector2(0, 80f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();

            // set up scroll rect
            scrollRectGO.GetRectTransform().sizeDelta = RectTransform.sizeDelta;
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapView.RectTransform;

            // TODO: make own components
            CreatePositionTexts();

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
        }

        private void ShowInRaid()
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = new Vector2(0, -22);
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta - new Vector2(0, 40f);

            // filter dropdown to only maps containing the internal map name
            // this forces the load of the first of those
            _mapSelectDropdown.FilterByInternalMapName(GameUtils.GetCurrentMap());

            // TODO: this should be in another place
            // create player marker if one doesn't already exist
            var player = GameUtils.GetPlayer();
            if (_playerMarker == null)
            {
                _playerMarker = _mapView.AddPlayerMarker(player, "player");
                _playerMarker.Color = Color.green;
            }

            // TODO: remove this
            // test bot markers
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld.AllAlivePlayersList.Count > 1)
            {
                foreach (var person in gameWorld.AllAlivePlayersList)
                {
                    if (person.IsYourPlayer || _otherPlayers.ContainsKey(person))
                    {
                        continue;
                    }

                    var botMarker = _mapView.AddPlayerMarker(person, "bots");
                    botMarker.Color = (person.Fraction == ETagStatus.Scav) ? (Color.yellow + Color.red)/2 : Color.red;
                    person.OnIPlayerDeadOrUnspawn += (bot) => _otherPlayers.Remove(bot);
                    _otherPlayers[person] = botMarker;
                }
            }

            // select layers to show
            var mapPosition = MathUtils.TransformToMapPosition(player.CameraPosition);
            _mapView.SelectLevelByCoords(mapPosition);

            // shift map to player position, Vector3 to Vector2 discards z
            // TODO: this is annoying, but need something like it
            // _mapView.ShiftMapToCoordinate(mapPosition, 0);

            // show and update text
            _playerPositionText.gameObject.SetActive(true);
            _playerPositionText.text = $"Player: {mapPosition.x:F} {mapPosition.y:F} {mapPosition.z:F}";
        }

        private void ShowOutOfRaid()
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = new Vector2(0, -5);
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta - new Vector2(0, 70f);

            // clear filter on dropdown
            _mapSelectDropdown.ClearFilter();

            // hide player position text
            _playerPositionText.gameObject.SetActive(false);

            // TODO: remove this
            if (_playerMarker != null)
            {
                _mapView.RemoveMapMarker(_playerMarker);
            }
            foreach (var botMarker in _otherPlayers.Values)
            {
                _mapView.RemoveMapMarker(botMarker);
            }
            _otherPlayers.Clear();
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

            // check if raid
            var game = Singleton<AbstractGame>.Instance;
            if (game != null && game.InRaid)
            {
                ShowInRaid();
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
