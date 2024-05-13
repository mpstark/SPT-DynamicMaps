using System;
using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.UI
{
    internal class MapSelectDropdown : MonoBehaviour
    {
        public Action<MapDef> OnSelected { get; set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private DropDownBox _dropdown;
        private List<MapDef> _mapDefs;
        private bool _hasBindInitiallyCalled = false;

        internal static MapSelectDropdown Create(GameObject prefab, Transform parent, Vector2 position, Vector2 size,
                                                 Action<MapDef> onSelected)
        {
            var go = GameObject.Instantiate(prefab);
            go.name = "MapSelectDropdown";

            var rectTransform = go.GetRectTransform();
            rectTransform.SetParent(parent);
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position; // this is lazy, prob should adjust all of the anchors

            var dropdown = go.AddComponent<MapSelectDropdown>();
            dropdown.OnSelected = onSelected;

            return dropdown;
        }

        private void Awake()
        {
            _dropdown = gameObject.GetComponentInChildren<DropDownBox>();
            _dropdown.SetLabelText("Select a Map");
        }

        private void OnSelectDropdownMap(int index)
        {
            if (!_hasBindInitiallyCalled)
            {
                _hasBindInitiallyCalled = true;
                return;
            }

            OnSelected(_mapDefs[index]);
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
