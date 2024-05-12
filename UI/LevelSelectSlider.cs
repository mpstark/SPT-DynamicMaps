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
    public class LevelSelectSlider
    {
        public int SelectedLevel { get; private set; }
        public GameObject GameObject { get; private set; }
        public RectTransform RectTransform => GameObject.transform as RectTransform;

        private TextMeshProUGUI _text;
        private Scrollbar _scrollbar;

        private List<int> _levels = new List<int>();
        private Action<int> _onLevelSelected;

        public LevelSelectSlider(GameObject prefab, Transform parent, Vector2 position, Action<int> onLevelSelected)
        {
            GameObject = UnityEngine.Object.Instantiate(prefab);
            GameObject.name = "LevelSelectScrollbar";
            GameObject.transform.SetParent(parent);
            GameObject.transform.localScale = Vector3.one;

            // position to top left
            var oldPosition = GameObject.GetRectTransform().anchoredPosition;
            GameObject.GetRectTransform().anchoredPosition = position;

            // remove useless component
            UnityEngine.Object.Destroy(GameObject.GetComponent<MapZoomer>());

            // create layer text
            var slidingArea = GameObject.transform.Find("Scrollbar/Sliding Area/Handle").gameObject;
            var layerTextGO = UIUtils.CreateUIGameObject(slidingArea, "SlidingLayerText");
            _text = layerTextGO.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 14;
            _text.alignment = TextAlignmentOptions.Left;
            _text.GetRectTransform().anchoredPosition = new Vector2(0, -17);

            // setup the scrollbar component
            var actualScrollbarGO = GameObject.transform.Find("Scrollbar").gameObject;
            _scrollbar = actualScrollbarGO.GetComponent<Scrollbar>();
            _scrollbar.direction = Scrollbar.Direction.BottomToTop;
            _scrollbar.onValueChanged.AddListener(OnScrollbarChanged);

            _onLevelSelected = onLevelSelected;
        }

        private void OnScrollbarChanged(float newValue)
        {
            var levelIndex = Mathf.RoundToInt(newValue * (_levels.Count - 1));
            var level = _levels[levelIndex];
            if (SelectedLevel != level)
            {
                _onLevelSelected(level);
            }
        }

        public void OnLevelSelected(int level)
        {
            if (SelectedLevel == level)
            {
                return;
            }

            _scrollbar.value = _levels.IndexOf(level) / (_levels.Count - 1f);
            _text.text = $"Level {level}";
            SelectedLevel = level;
        }

        public void OnMapLoaded(MapDef mapDef)
        {
            _levels.Clear();
            SelectedLevel = int.MinValue;

            // extract the unique levels from mapDef layers
            var uniqueLevels = new HashSet<int>();
            foreach (var layerDef in mapDef.Layers.Values)
            {
                uniqueLevels.Add(layerDef.Level);
            }

            _levels = uniqueLevels.ToList();
            _levels.Sort();

            _scrollbar.numberOfSteps = _levels.Count();
        }
    }
}
