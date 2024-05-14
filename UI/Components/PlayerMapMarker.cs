using System;
using System.Collections.Generic;
using EFT;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class PlayerMapMarker : MapMarker
    {
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

        public static PlayerMapMarker Create(IPlayer player, GameObject parent, string imagePath, string category, Vector2 size)
        {
            var name = $"{player.Profile.Nickname} marker";

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            rectTransform.sizeDelta = size;

            var marker = go.AddComponent<PlayerMapMarker>();
            marker.Player = player;
            marker.Name = player.Profile.Nickname;
            marker.Category = category;
            marker.Image = go.AddComponent<Image>();
            marker.Image.sprite = TextureUtils.GetOrLoadCachedSprite(imagePath);
            marker.Image.type = Image.Type.Simple;

            return marker;
        }

        private void Update()
        {
            MoveAndRotate(MathUtils.UnityPositionToMapPosition(Player.Position),
                          -Player.Rotation.x); // TODO: I'm unsure why negative rotation here
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

        public override void OnContainingLayerChanged(bool isLayerDisplayed, bool isLayerOnTopLevel)
        {
            // TODO: revisit this
            var color = Image.color;
            var alpha = 1f;
            if (!isLayerDisplayed || !isLayerOnTopLevel)
            {
                alpha = 0.25f;
            }

            var newColor = new Color(color.r, color.g, color.b, alpha);
            Image.color = newColor;
        }
    }
}
