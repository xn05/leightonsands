using System;
using LeightonSands.Entities;
using LeightonSands.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands.Scenes;

public sealed class WorldMapScene : TiledMapScene
{
    private readonly MapRegionProperties _properties;
    private readonly PlayerCharacter _player = new();
    private TiledCollisionMap? _collisionMap;
    private GraphicsDevice? _graphicsDevice;
    private Texture2D? _overlayTexture;

    public WorldMapScene(MapRegionProperties properties) : base(properties.Map)
    {
        _properties = properties;
    }

    public void StartNewGame(string playerId)
    {
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            _properties.Player = playerId;
        }

        _player.SetCharacter(_properties.Player);
        if (Map != null)
        {
            _player.SpawnAtTile(Map, _properties.SpawnTileX, _properties.SpawnTileY);
        }
    }

    public override void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        base.LoadContent(content, graphicsDevice);

        if (_collisionMap != null)
        {
            return;
        }

        _graphicsDevice = graphicsDevice;
        if (Map == null)
        {
            return;
        }

        _collisionMap = new TiledCollisionMap(Map);
        Camera.Zoom = _properties.CameraZoom > 0f ? _properties.CameraZoom : 3f;
        _overlayTexture = new Texture2D(graphicsDevice, 1, 1);
        _overlayTexture.SetData([Color.White]);
        _player.HeightTiles = _properties.PlayerHeightTiles > 0f ? _properties.PlayerHeightTiles : 1.8f;
        _player.SpeedTilesPerSecond = _properties.Speed > 0f ? _properties.Speed : 5.5f;
        _player.LoadContent(content, _properties.Player);
        _player.SpawnAtTile(Map, _properties.SpawnTileX, _properties.SpawnTileY);
        CenterCamera(graphicsDevice.Viewport);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Map == null || _collisionMap == null)
        {
            return;
        }

        _player.Update(gameTime, Map, _collisionMap);
        if (_graphicsDevice != null)
        {
            CenterCamera(_graphicsDevice.Viewport);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (Map != null)
        {
            _player.Draw(spriteBatch, Camera, Map);
        }

        DrawRegionOverlay(spriteBatch);
        spriteBatch.End();
    }

    private void CenterCamera(Viewport viewport)
    {
        if (Map == null)
        {
            return;
        }

        var center = _player.FeetPosition - new Vector2(0f, Map.TileHeight * 0.5f);
        var visibleWidth = viewport.Width / MathHelper.Max(0.01f, Camera.Zoom);
        var visibleHeight = viewport.Height / MathHelper.Max(0.01f, Camera.Zoom);
        Camera.Position = new Vector2(
            MathHelper.Clamp(center.X - visibleWidth * 0.5f, 0f, Math.Max(0, Map.WidthInPixels - visibleWidth)),
            MathHelper.Clamp(center.Y - visibleHeight * 0.5f, 0f, Math.Max(0, Map.HeightInPixels - visibleHeight)));
    }

    private void DrawRegionOverlay(SpriteBatch spriteBatch)
    {
        if (_overlayTexture == null || _graphicsDevice == null)
        {
            return;
        }

        var fogOpacity = MathHelper.Clamp(_properties.FogOpacity, 0f, 1f);
        if (fogOpacity > 0f)
        {
            spriteBatch.Draw(_overlayTexture, _graphicsDevice.Viewport.Bounds, ParseColor(_properties.FogColor) * fogOpacity);
        }

        var darkness = 1f - MathHelper.Clamp(_properties.Brightness, 0f, 1f);
        if (darkness > 0f)
        {
            spriteBatch.Draw(_overlayTexture, _graphicsDevice.Viewport.Bounds, Color.Black * darkness);
        }
    }

    private static Color ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Color.Black;
        }

        var hex = value.Trim().TrimStart('#');
        if (hex.Length == 6 &&
            byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return new Color(r, g, b);
        }

        return Color.Black;
    }
}
