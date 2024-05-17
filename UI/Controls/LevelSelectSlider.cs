using System;
using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Controls
{
    public class LevelSelectSlider : MonoBehaviour
    {
        private static float _levelTextSize = 15f;
        private static Vector2 _levelTextOffset = new Vector2(10f, 0f);

        public event Action<int> OnLevelSelectedBySlider;
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private int _selectedLevel = int.MinValue;
        public int SelectedLevel
        {
            get
            {
                return _selectedLevel;
            }

            set
            {
                if (_selectedLevel == value)
                {
                    return;
                }

                _scrollbar.value = _levels.IndexOf(value) / (_levels.Count - 1f);
                _text.text = $"Level {value}";
                _selectedLevel = value;
            }
        }

        private TextMeshProUGUI _text;
        private Scrollbar _scrollbar;
        private List<int> _levels = new List<int>();
        private bool _hasSetOutline = false;

        public static LevelSelectSlider Create(GameObject prefab, Transform parent, Vector2 position)
        {
            var go = GameObject.Instantiate(prefab);
            go.name = "LevelSelectScrollbar";
            go.transform.SetParent(parent);
            go.transform.localScale = Vector3.one;

            // position to top left
            var oldPosition = go.GetRectTransform().anchoredPosition;
            go.GetRectTransform().anchoredPosition = position;

            // remove useless component
            GameObject.Destroy(go.GetComponent<MapZoomer>());

            var slider = go.AddComponent<LevelSelectSlider>();
            return slider;
        }

        private void Awake()
        {
            // create layer text
            var slidingArea = gameObject.transform.Find("Scrollbar/Sliding Area/Handle").gameObject;
            var layerTextGO = UIUtils.CreateUIGameObject(slidingArea, "SlidingLayerText");
            _text = layerTextGO.AddComponent<TextMeshProUGUI>();
            _text.fontSize = _levelTextSize;
            _text.alignment = TextAlignmentOptions.Left;
            _text.GetRectTransform().offsetMin = Vector2.zero;
            _text.GetRectTransform().offsetMax = Vector2.zero;
            _text.GetRectTransform().anchoredPosition = _levelTextOffset;

            _hasSetOutline = UIUtils.TrySetTMPOutline(_text);

            // setup the scrollbar component
            var actualScrollbarGO = gameObject.transform.Find("Scrollbar").gameObject;
            _scrollbar = actualScrollbarGO.GetComponent<Scrollbar>();
            _scrollbar.direction = Scrollbar.Direction.BottomToTop;
            _scrollbar.onValueChanged.AddListener(OnScrollbarChanged);
            _scrollbar.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);

            // setup the +/- buttons
            var plusButton = gameObject.transform.Find("Plus").gameObject.GetComponent<Button>();
            plusButton.onClick.AddListener(() => ChangeLevelBy(1));
            plusButton.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);

            var minusButton = gameObject.transform.Find("Minus").gameObject.GetComponent<Button>();
            minusButton.onClick.AddListener(() => ChangeLevelBy(-1));
            minusButton.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);

            // initially hide until map loaded
            gameObject.SetActive(false);

        }

        private void OnEnable()
        {
            if (_hasSetOutline || _text == null)
            {
                return;
            }

            _hasSetOutline = UIUtils.TrySetTMPOutline(_text);
        }

        private void OnDestroy()
        {
            OnLevelSelectedBySlider = null;
        }

        private void OnScrollbarChanged(float newValue)
        {
            var levelIndex = Mathf.RoundToInt(newValue * (_levels.Count - 1));
            var level = _levels[levelIndex];
            if (_selectedLevel != int.MinValue && _selectedLevel != level)
            {
                OnLevelSelectedBySlider?.Invoke(level);
            }
        }

        private void ChangeLevelBy(int delta)
        {
            var newIndex = _levels.IndexOf(_selectedLevel) + delta;
            if (newIndex < 0 || newIndex >= _levels.Count)
            {
                return;
            }

            OnLevelSelectedBySlider?.Invoke(_levels[newIndex]);
        }

        public void OnLoadMap(MapDef mapDef, int initialLevel)
        {
            _levels.Clear();
            _selectedLevel = int.MinValue;

            // extract the unique levels from mapDef layers
            var uniqueLevels = new HashSet<int>();
            foreach (var layerDef in mapDef.Layers.Values)
            {
                uniqueLevels.Add(layerDef.Level);
            }

            _levels = uniqueLevels.ToList();
            _levels.Sort();

            _scrollbar.numberOfSteps = _levels.Count();
            SelectedLevel = initialLevel;

            gameObject.SetActive(true);
        }
    }
}
