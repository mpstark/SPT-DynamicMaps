using System;
using System.Collections.Generic;
using EFT;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class IPlayerMapMarker : MapMarker
    {
        public event Action<MapMarker> OnDeathOrDespawn;
        public List<MapLayer> TraversableLayers { get; set; }

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

        public static IPlayerMapMarker Create(IPlayer player, GameObject parent, string imagePath, string category,
                                              Vector2 size, float scale = 1)
        {
            var name = $"{player.Profile.Nickname} marker";

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            rectTransform.sizeDelta = size;
            rectTransform.localScale = scale * Vector2.one;

            var marker = go.AddComponent<IPlayerMapMarker>();
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
            // move marker to follow transform
            var position3D = Player.Position;
            var position2D = new Vector2(position3D.x, position3D.z);

            MoveAndRotate(position2D, -Player.Rotation.x); // I'm unsure why negative rotation here
            LinkedLayer = FindBestLayer(position2D, position3D.y);
        }

        protected override void OnDestroy()
        {
            if (_player == null)
            {
                return;
            }

            _player.OnIPlayerDeadOrUnspawn -= HandleDeathOrDespawn;
            OnDeathOrDespawn = null;
            base.OnDestroy();
        }

        private MapLayer FindBestLayer(Vector2 coord, float height)
        {
            // FIXME: this is a duplicate of FindMatchingLayerByCoords in ModdedMapScreen
            // FIXME: what if there are multiple matching?
            // probably want to "select" the smaller bounds one in that case
            foreach(var layer in TraversableLayers)
            {
                if (layer.IsCoordInLayer(coord, height))
                {
                    return layer;
                }
            }

            return null;
        }

        protected override void OnLinkedLayerChanged(bool isDisplayed, bool isOnTopLevel)
        {
            // TODO: revisit this
            var color = Image.color;
            var alpha = 1f;
            if (!isDisplayed || !isOnTopLevel)
            {
                alpha = 0.25f;
            }

            var newColor = new Color(color.r, color.g, color.b, alpha);
            Image.color = newColor;
        }

        private void HandleDeathOrDespawn(IPlayer player)
        {
            if (_player != player)
            {
                return;
            }

            OnDeathOrDespawn?.Invoke(this);
        }
    }
}
