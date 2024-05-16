using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using InGameMap.UI.Controls;
using InGameMap.UI.Components;
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
        private static float _positionTextFontSize = 15f;
        private static Vector2 _textAnchor = new Vector2(0f, 1f);
        private static Vector2 _cursorPositionTextOffset = new Vector2(15f, -52f);
        private static Vector2 _playerPositionTextOffset = new Vector2(15f, -68f);

        public RectTransform RectTransform => gameObject.GetRectTransform();
        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private MapView _mapView;

        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;
        private CursorPositionText _cursorPositionText;
        private PlayerPositionText _playerPositionText;

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
            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionInRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierInRaid;

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
                    botMarker.Color = (person.Fraction == ETagStatus.Scav)
                                    ? Color.Lerp(Color.yellow, Color.red, 0.5f)
                                    : Color.red;
                    person.OnIPlayerDeadOrUnspawn += (bot) => _otherPlayers.Remove(bot);
                    _otherPlayers[person] = botMarker;
                }
            }

            // select layers to show
            var mapPosition = MathUtils.UnityPositionToMapPosition(player.Position);
            _mapView.SelectLevelByCoords(mapPosition);

            // shift map to player position, Vector3 to Vector2 discards z
            // TODO: this is annoying, but need something like it
            // _mapView.ShiftMapToCoordinate(mapPosition, 0);

            // show player position text
            _playerPositionText.gameObject.SetActive(true);
        }

        private void ShowOutOfRaid()
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

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
