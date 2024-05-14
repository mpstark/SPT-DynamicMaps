using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT.UI;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.UI.Controls
{
    public class MapSelectDropdown : MonoBehaviour
    {
        private static HashSet<string> _acceptableExtensions = new HashSet<string>{ "json", "jsonc" };

        public event Action<MapDef> OnMapSelected;
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private DropDownBox _dropdown;
        private List<MapDef> _mapDefs = new List<MapDef>();
        private bool _hasBindInitiallyCalled = false;

        public static MapSelectDropdown Create(GameObject prefab, Transform parent, Vector2 position, Vector2 size)
        {
            var go = GameObject.Instantiate(prefab);
            go.name = "MapSelectDropdown";

            var rectTransform = go.GetRectTransform();
            rectTransform.SetParent(parent);
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position; // this is lazy, prob should adjust all of the anchors

            var dropdown = go.AddComponent<MapSelectDropdown>();
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

            OnMapSelected?.Invoke(_mapDefs[index]);
        }

        private void ChangeAvailableMapDefs()
        {
            _hasBindInitiallyCalled = false;
            _dropdown.Show(_mapDefs.Select(def => def.DisplayName));
            _dropdown.OnValueChanged.Bind(OnSelectDropdownMap, 0);
        }

        public void LoadMapDefsFromPath(string relPath)
        {
            _mapDefs.Clear();

            var absolutePath = Path.Combine(Plugin.Path, relPath);
            var paths = Directory.EnumerateFiles(absolutePath, "*.*", SearchOption.AllDirectories)
                .Where(p => _acceptableExtensions.Contains(Path.GetExtension(p).TrimStart('.').ToLowerInvariant()));

            foreach (var path in paths)
            {
                var mapDef = MapDef.LoadFromPath(path);
                if (mapDef != null)
                {
                    _mapDefs.Add(mapDef);
                }
            }

            ChangeAvailableMapDefs();
        }

        public void OnMapLoading(MapDef mapDef)
        {
            _dropdown.UpdateValue(_mapDefs.IndexOf(mapDef));
        }
    }
}
