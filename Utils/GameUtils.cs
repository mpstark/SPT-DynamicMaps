using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Vehicle;
using HarmonyLib;

namespace DynamicMaps.Utils
{
    public static class GameUtils
    {
        // reflection
        private static FieldInfo _playerCorpseField = AccessTools.Field(typeof(Player), "Corpse");
        private static FieldInfo _playerLastAggressorField = AccessTools.Field(typeof(Player), "LastAggressor");
        private static Type _profileInterface = typeof(ISession).GetInterfaces().First(i =>
            {
                var properties = i.GetProperties();
                return properties.Length == 2 &&
                       properties.Any(p => p.Name == "Profile");
            });
        private static PropertyInfo _sessionProfileProperty = AccessTools.Property(_profileInterface, "Profile");
        public static ISession Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
        public static Profile PlayerProfile => _sessionProfileProperty.GetValue(Session) as Profile;
        //

        private static HashSet<WildSpawnType> _trackedBosses = new HashSet<WildSpawnType>
        {
            WildSpawnType.bossBoar,             // Kaban
            WildSpawnType.bossBully,            // Reshala
            WildSpawnType.bossGluhar,           // Glukhar
            WildSpawnType.bossKilla,
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossKojaniy,          // Shturman
            WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla,
            WildSpawnType.bossZryachiy,
            WildSpawnType.gifter,               // Santa
            WildSpawnType.arenaFighterEvent,    // Blood Hounds
            WildSpawnType.sectantPriest,        // Cultist Priest
            (WildSpawnType) 4206927,            // Punisher
            (WildSpawnType) 199,                // Legion
        };

        public static bool IsInRaid()
        {
            var game = Singleton<AbstractGame>.Instance;
            var botGame = Singleton<IBotGame>.Instance;

            return ((game != null) && game.InRaid)
                || ((botGame != null) && botGame.Status != GameStatus.Stopped
                                      && botGame.Status != GameStatus.Stopping
                                      && botGame.Status != GameStatus.SoftStopping);
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

        public static Profile GetPlayerProfile()
        {
            return PlayerProfile;
        }

        public static BTRView GetBTRView()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.BtrController?.BtrView;
        }

        public static bool IsScavRaid()
        {
            var player = GetMainPlayer();
            return IsInRaid() && (player != null) && player.Side == EPlayerSide.Savage;
        }

        public static string BSGLocalized(this string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "";
            }

            // TODO: use reflection to get rid of this gclass reference
            return id.Localized();
        }

        public static bool IsGroupedWithMainPlayer(this IPlayer player)
        {
            var mainPlayerGroupId = GetMainPlayer().GroupId;
            return !string.IsNullOrEmpty(mainPlayerGroupId) && player.GroupId == mainPlayerGroupId;
        }

        public static bool IsTrackedBoss(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage && _trackedBosses.Contains(player.Profile.Info.Settings.Role);
        }

        public static bool IsPMC(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Bear || player.Profile.Side == EPlayerSide.Usec;
        }

        public static bool DidMainPlayerKill(this IPlayer player)
        {
            var aggressor = _playerLastAggressorField.GetValue(player) as IPlayer;
            if (aggressor == null)
            {
                return false;
            }

            var mainPlayer = GetMainPlayer();
            if (aggressor.ProfileId == mainPlayer.ProfileId)
            {
                return true;
            }

            return false;
        }

        public static bool DidTeammateKill(this IPlayer player)
        {
            var aggressor = _playerLastAggressorField.GetValue(player) as IPlayer;
            if (aggressor == null || string.IsNullOrEmpty(aggressor.GroupId))
            {
                return false;
            }

            var mainPlayer = GetMainPlayer();
            if (aggressor.ProfileId != mainPlayer.ProfileId && aggressor.GroupId == GetMainPlayer().GroupId)
            {
                return true;
            }

            return false;
        }

        public static bool IsBTRShooter(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage
                && player.Profile.Info.Settings.Role == WildSpawnType.shooterBTR;
        }

        public static bool HasCorpse(this Player player)
        {
            return _playerCorpseField.GetValue(player) != null;
        }
    }
}
