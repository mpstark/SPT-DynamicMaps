using Comfort.Common;
using EFT;

namespace InGameMap.Utils
{
    public static class GameUtils
    {
        public static string GetCurrentMap()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer?.Location;
        }

        public static Player GetPlayer()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer;
        }
    }
}