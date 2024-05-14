using System.Collections.Generic;
using Comfort.Common;
using DG.Tweening;
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

        private static float _zoomScaler = 1.75f;
        private static float _zoomTweenTime = 0.25f;
        private static float _positionTweenTime = 0.25f;
        private static float _zoomMaxScaler = 10f;  // multiplier against zoomMin
        private static float _zoomMinScaler = 1.1f; // divider against ratio of screen

        private static Vector2 _levelSliderPosition = new Vector2(15f, 750f);
        private static Vector2 _mapSelectDropdownPosition = new Vector2(-780, -50);
        private static Vector2 _mapSelectDropdownSize = new Vector2(360, 31);

        public RectTransform RectTransform => gameObject.GetRectTransform();
        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private MapView _mapView;
        private GameObject _mapContentGO => _mapView.gameObject;
        private RectTransform _mapRectTransform => _mapView.GetRectTransform();
        private float _coordinateRotation => _mapView.CoordinateRotation;

        private ScrollRect _scrollRect;
        private Mask _scrollMask;

        private MapDef _currentMapDef;

        private TextMeshProUGUI _playerPositionText;
        private TextMeshProUGUI _cursorPositionText;
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;

        private Vector2 _immediateMapAnchor = Vector2.zero;
        private float _zoomMin; // set when map loaded
        private float _zoomMax; // set when map loaded
        private float _zoomCurrent = 0.5f;

        // TODO: remove this and put it somewhere else
        private IPlayerMapMarker _playerMarker;
        private Dictionary<IPlayer, IPlayerMapMarker> _otherPlayers = new Dictionary<IPlayer, IPlayerMapMarker>();

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
            // THIS SEEMS GROSS!
            // FIXME: does this generate large amounts of garbage?
            var mapMarkers = _mapView.MapMarkerContainer.transform.GetChildren();
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
            _scrollRect.content = _mapRectTransform;

            // TODO: make own components
            CreatePositionTexts();

            // TODO: load all json files in Maps\*.json instead and add map string to MapDef

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
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);
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
            if (mapDef == null || _currentMapDef == mapDef)
            {
                return;
            }

            if (_currentMapDef != null)
            {
                _immediateMapAnchor = Vector2.zero;
                _mapView.UnloadMap();
            }

            Plugin.Log.LogInfo($"MapScreen: Loading map {mapDef.DisplayName}");

            _currentMapDef = mapDef;

            _levelSelectSlider.OnMapLoading(mapDef);
            _mapSelectDropdown.OnMapLoading(mapDef);
            _mapView.LoadMap(mapDef);

            // set zoom min and max based on size of map and size of mask
            var maskSize = _scrollMask.GetRectTransform().sizeDelta;
            var mapSize = _mapView.RectTransform.sizeDelta;
            _zoomMin = Mathf.Min(maskSize.x / mapSize.x, maskSize.y / mapSize.y) / _zoomMinScaler;
            _zoomMax = _zoomMaxScaler * _zoomMin;

            // this will set everything up for initial zoom
            SetMapZoom(_zoomMin, 0);

            // shift map by the offset to center it in the scroll mask
            ShiftMap(_scrollMask.GetRectTransform().anchoredPosition * _zoomCurrent, 0);
        }

        private void ShowInRaid(LocalGame game)
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = new Vector2(0, -22);
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta - new Vector2(0, 40f);

            // TODO: make sure that the current map is loaded

            // TODO: don't hide, just show map selector for only this map
            // hide map selector
            // _mapSelectDropdown.gameObject.SetActive(false);

            // TODO: this should be in another place
            // create player marker if one doesn't already exist
            var player = game.PlayerOwner.Player;
            if (_playerMarker == null)
            {
                _playerMarker = _mapView.AddPlayerMarker(player);
                _playerMarker.Image.color = Color.green;
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

                    // var botMarker = IPlayerMapMarker.Create(person, _mapMarkersGO, "Markers/arrow.png",
                    //                                         "bots", _markerSize, 1/_zoomCurrent);
                    var botMarker = _mapView.AddPlayerMarker(person);
                    botMarker.Image.color = (person.Fraction == ETagStatus.Scav) ? Color.yellow : Color.red;
                    person.OnIPlayerDeadOrUnspawn += (bot) => _otherPlayers.Remove(bot);
                    _otherPlayers[person] = botMarker;
                }
            }

            // select layers to show
            var player3dPos = player.CameraPosition.position;
            var player2dPos = new Vector2(player3dPos.x, player3dPos.z);
            _mapView.SelectLevelByCoords(player2dPos, player3dPos.y);

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
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta - new Vector2(0, 70f);

            // show map selector
            // _mapSelectDropdown.gameObject.SetActive(true);

            // hide player position text
            _playerPositionText.gameObject.SetActive(false);

            // TODO: remove this
            if (_playerMarker != null)
            {
                _mapView.RemoveMapMarker(_playerMarker);
            }
            foreach (var (bot, botMarker) in _otherPlayers)
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

            // FIXME: this is gross
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);

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
