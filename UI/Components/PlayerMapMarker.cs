using System;
using EFT;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class PlayerMapMarker : MapMarker
    {
        private static float _maxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public event Action<MapMarker> OnDeathOrDespawn;

        private IPlayer _player;
        public IPlayer Player
        {
            get
            {
                return _player;
            }

            private set
            {
                if (_player == value)
                {
                    return;
                }

                if (_player != null)
                {
                    _player.OnIPlayerDeadOrUnspawn -= HandleDeathOrDespawn;
                }

                _player = value;
                _player.OnIPlayerDeadOrUnspawn += HandleDeathOrDespawn;
            }
        }

        private float _callbackTime = _maxCallbackTime;  // make sure to start with a callback

        public static PlayerMapMarker Create(IPlayer player, GameObject parent, string imagePath, string category,
                                             Vector2 size, float degreesRotation, float scale)
        {
            var name = $"{player.Profile.Nickname}";
            var marker = Create<PlayerMapMarker>(parent, name, category, imagePath, Vector3.zero,
                                                 size, _pivot, degreesRotation, scale);
            marker.IsDynamic = true;
            marker.Player = player;

            return marker;
        }

        private void Update()
        {
            if (Player == null)
            {
                return;
            }

            // throttle callback, since that leads to a layer search which might be expensive
            _callbackTime += Time.deltaTime;
            var callback = _callbackTime >= _maxCallbackTime;
            if (callback)
            {
                _callbackTime = 0f;
            }

            MoveAndRotate(MathUtils.UnityPositionToMapPosition(Player.Position),
                          -Player.Rotation.x, callback); // TODO: I'm unsure why negative rotation here
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_player != null)
            {
                _player.OnIPlayerDeadOrUnspawn -= HandleDeathOrDespawn;
            }
        }

        private void HandleDeathOrDespawn(IPlayer player)
        {
            if (_player != player)
            {
                return;
            }

            OnDeathOrDespawn?.Invoke(this);
        }

        protected override Color GetLayerAdjustedImageColor(bool isLayerDisplayed, bool isLayerOnTop)
        {
            var alpha = (!isLayerDisplayed || !isLayerOnTop)
                        ? 0.25f
                        : 1.0f;
            return new Color(Image.color.r, Image.color.g, Image.color.b, alpha);
        }
    }
}
