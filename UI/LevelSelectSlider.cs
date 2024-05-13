using System;
using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    internal class LevelSelectSlider : MonoBehaviour
    {
        public RectTransform RectTransform => gameObject.transform as RectTransform;
        public Action<int> OnLevelSelected { get; set; }
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
        private int _selectedLevel = int.MinValue;

        internal static LevelSelectSlider Create(GameObject prefab, Transform parent, Vector2 position, Action<int> onLevelSelected)
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
            slider.OnLevelSelected = onLevelSelected;
            return slider;
        }

        private void Awake()
        {
            // create layer text
            var slidingArea = gameObject.transform.Find("Scrollbar/Sliding Area/Handle").gameObject;
            var layerTextGO = UIUtils.CreateUIGameObject(slidingArea, "SlidingLayerText");
            _text = layerTextGO.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 14;
            _text.alignment = TextAlignmentOptions.Left;
            _text.GetRectTransform().offsetMin = Vector2.zero;
            _text.GetRectTransform().offsetMax = Vector2.zero;
            _text.GetRectTransform().anchoredPosition = new Vector2(10, 0);

            // setup the scrollbar component
            var actualScrollbarGO = gameObject.transform.Find("Scrollbar").gameObject;
            _scrollbar = actualScrollbarGO.GetComponent<Scrollbar>();
            _scrollbar.direction = Scrollbar.Direction.BottomToTop;
            _scrollbar.onValueChanged.AddListener(OnScrollbarChanged);

            // initially hide until map loaded
            gameObject.SetActive(false);
        }

        private void OnScrollbarChanged(float newValue)
        {
            var levelIndex = Mathf.RoundToInt(newValue * (_levels.Count - 1));
            var level = _levels[levelIndex];
            if (_selectedLevel != level)
            {
                OnLevelSelected(level);
            }
        }

        internal void OnMapLoaded(MapDef mapDef)
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
            gameObject.SetActive(true);
        }
    }
}
