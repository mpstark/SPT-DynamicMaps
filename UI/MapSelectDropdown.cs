using System;
using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.UI
{
    internal class MapSelectDropdown
    {
        public GameObject GameObject { get; private set; }
        public RectTransform RectTransform => GameObject.transform as RectTransform;

        private DropDownBox _dropdown;
        private List<MapDef> _mapDefs;
        private Action<MapDef> _onSelected;
        private bool _hasBindInitiallyCalled = false;

        internal MapSelectDropdown(GameObject prefab, Transform parent, Vector2 position, Vector2 size,
                                   List<MapDef> mapDefs, Action<MapDef> onSelected)
        {
            GameObject = GameObject.Instantiate(prefab);
            GameObject.name = "MapSelectDropdown";

            var rectTransform = GameObject.GetRectTransform();
            rectTransform.SetParent(parent);
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position; // this is lazy, prob should adjust all of the anchors

            _dropdown = GameObject.GetComponentInChildren<DropDownBox>();
            _dropdown.SetLabelText("Select a Map");

            _onSelected = onSelected;

            ChangeAvailableMapDefs(mapDefs);
        }

        private void OnSelectDropdownMap(int index)
        {
            if (!_hasBindInitiallyCalled)
            {
                _hasBindInitiallyCalled = true;
                return;
            }

            _onSelected(_mapDefs[index]);
        }

        internal void ChangeAvailableMapDefs(List<MapDef> mapDefs)
        {
            _hasBindInitiallyCalled = false;
            _mapDefs = mapDefs;
            _dropdown.Show(_mapDefs.Select(def => def.DisplayName));
            _dropdown.OnValueChanged.Bind(OnSelectDropdownMap, 0);
        }

        internal void OnMapLoaded(MapDef mapDef)
        {
            _dropdown.UpdateValue(_mapDefs.IndexOf(mapDef));
        }
    }
}
