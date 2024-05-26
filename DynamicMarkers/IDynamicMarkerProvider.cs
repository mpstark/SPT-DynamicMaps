using DynamicMaps.Data;
using DynamicMaps.UI.Components;

namespace DynamicMaps.DynamicMarkers
{
    public interface IDynamicMarkerProvider
    {
        void OnShowOutOfRaid(MapView map);
        void OnHideOutOfRaid(MapView map);

        void OnShowInRaid(MapView map);
        void OnHideInRaid(MapView map);

        void OnRaidEnd(MapView map);

        void OnMapChanged(MapView map, MapDef mapDef);

        void OnDisable(MapView map);
    }
}
