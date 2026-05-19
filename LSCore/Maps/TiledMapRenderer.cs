using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands.Maps;

public sealed class TiledMapRenderer
{
    private readonly TiledMap _map;
    private readonly Dictionary<TiledTileset, Texture2D> _tilesetTextures = new();

    public TiledMapRenderer(TiledMap map, ContentManager content)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        foreach (var tileset in map.Tilesets)
        {
            _tilesetTextures[tileset] = content.Load<Texture2D>(GetContentName(content.RootDirectory, tileset.ImageSource));
        }
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, Viewport viewport)
    {
        if (spriteBatch == null)
        {
            throw new ArgumentNullException(nameof(spriteBatch));
        }

        if (camera == null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        var visibleWorld = camera.GetVisibleWorldBounds(viewport);
        var startX = Math.Clamp(visibleWorld.Left / _map.TileWidth, 0, _map.Width);
        var startY = Math.Clamp(visibleWorld.Top / _map.TileHeight, 0, _map.Height);
        var endX = Math.Clamp((visibleWorld.Right / _map.TileWidth) + 1, 0, _map.Width);
        var endY = Math.Clamp((visibleWorld.Bottom / _map.TileHeight) + 1, 0, _map.Height);
        var zoom = MathHelper.Max(0.01f, camera.Zoom);
        var offset = -camera.Position;

        foreach (var layer in _map.TileLayers)
        {
            if (!layer.Visible || layer.Opacity <= 0f)
            {
                continue;
            }

            DrawLayer(spriteBatch, layer, startX, startY, endX, endY, offset, zoom);
        }
    }

    private void DrawLayer(SpriteBatch spriteBatch, TiledTileLayer layer, int startX, int startY, int endX, int endY, Vector2 offset, float zoom)
    {
        var tint = Color.White * Math.Clamp(layer.Opacity, 0f, 1f);

        for (var y = startY; y < endY; y++)
        {
            for (var x = startX; x < endX; x++)
            {
                var tile = layer.GetTile(x, y);
                if (tile.IsEmpty)
                {
                    continue;
                }

                var tileset = _map.FindTileset(tile.Gid);
                if (tileset == null || !_tilesetTextures.TryGetValue(tileset, out var texture))
                {
                    continue;
                }

                var destination = new Rectangle(
                    (int)MathF.Round(((x * _map.TileWidth) + offset.X) * zoom),
                    (int)MathF.Round(((y * _map.TileHeight) + offset.Y) * zoom),
                    (int)MathF.Round(tileset.TileWidth * zoom),
                    (int)MathF.Round(tileset.TileHeight * zoom));

                spriteBatch.Draw(texture, destination, tileset.GetSourceRectangle(tile.Gid), tint, 0f, Vector2.Zero, GetEffects(tile), 0f);
            }
        }
    }

    private static SpriteEffects GetEffects(TiledTile tile)
    {
        var effects = SpriteEffects.None;
        if (tile.FlippedHorizontally)
        {
            effects |= SpriteEffects.FlipHorizontally;
        }

        if (tile.FlippedVertically)
        {
            effects |= SpriteEffects.FlipVertically;
        }

        return effects;
    }

    private static string GetContentName(string contentRootDirectory, string imageSource)
    {
        var contentRoot = Path.IsPathRooted(contentRootDirectory)
            ? Path.GetFullPath(contentRootDirectory)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, contentRootDirectory));
        var imagePath = Path.GetFullPath(imageSource);
        var relativePath = Path.GetRelativePath(contentRoot, imagePath);
        var withoutExtension = Path.ChangeExtension(relativePath, null) ?? relativePath;

        return withoutExtension.Replace('\\', '/');
    }
}
