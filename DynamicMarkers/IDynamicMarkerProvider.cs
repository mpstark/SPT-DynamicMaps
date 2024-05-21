using InGameMap.Data;
using InGameMap.UI.Components;

namespace InGameMap.DynamicMarkers
{
    public interface IDynamicMarkerProvider
    {
        void OnShowInRaid(MapView map, string mapInternalName);
        void OnHideInRaid(MapView map);

        void OnRaidEnd(MapView map);

        void OnMapChanged(MapView map, MapDef mapDef);
    }
}
