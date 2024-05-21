using Comfort.Common;
using EFT;

namespace InGameMap.Utils
{
    public static class GameUtils
    {
        public static bool IsInRaid()
        {
            var game = Singleton<AbstractGame>.Instance;
            return (game != null) && game.InRaid;
        }

        public static string GetCurrentMapInternalName()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer?.Location;
        }

        public static Player GetMainPlayer()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer;
        }

        public static bool IsScavRaid()
        {
            var game = Singleton<AbstractGame>.Instance;
            var player = GetMainPlayer();
            return (game != null) && game.InRaid
                && (player != null) && player.Side == EPlayerSide.Savage;
        }

        public static string BSGLocalized(this string id)
        {
            // TODO: use reflection to get rid of this gclass reference
            return id.Localized();
        }
    }
}
