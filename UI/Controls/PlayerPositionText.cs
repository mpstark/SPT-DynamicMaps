using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Controls
{
    public class PlayerPositionText : AbstractTextControl
    {
        public static PlayerPositionText Create(GameObject parent, float fontSize)
        {
            var text = Create<PlayerPositionText>(parent, "PlayerPositionText", fontSize);
            return text;
        }

        private void Update()
        {
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(player.Position);
            Text.text = $"Player: {mapPosition.x:F} {mapPosition.y:F} {mapPosition.z:F}";
        }
    }
}
