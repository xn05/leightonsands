using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LeightonSands.Maps;

public sealed class TiledMap
{
    public string Name { get; init; } = string.Empty;
    public string Directory { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public int TileWidth { get; init; }
    public int TileHeight { get; init; }
    public List<TiledTileset> Tilesets { get; } = new();
    public List<TiledTileLayer> TileLayers { get; } = new();
    public List<TiledObjectLayer> ObjectLayers { get; } = new();

    public int PixelWidth => Width * TileWidth;
    public int PixelHeight => Height * TileHeight;

    public TiledTileset? FindTileset(uint gid)
    {
        if (gid == 0)
        {
            return null;
        }

        for (var i = Tilesets.Count - 1; i >= 0; i--)
        {
            if (gid >= Tilesets[i].FirstGid)
            {
                return Tilesets[i];
            }
        }

        return null;
    }
}

public sealed class TiledTileset
{
    public uint FirstGid { get; init; }
    public string Name { get; init; } = string.Empty;
    public int TileWidth { get; init; }
    public int TileHeight { get; init; }
    public int TileCount { get; init; }
    public int Columns { get; init; }
    public int Spacing { get; init; }
    public int Margin { get; init; }
    public string ImageSource { get; init; } = string.Empty;
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }

    public Rectangle GetSourceRectangle(uint gid)
    {
        var localId = (int)(gid - FirstGid);
        var columns = Columns > 0 ? Columns : Math.Max(1, ImageWidth / Math.Max(1, TileWidth));
        var column = localId % columns;
        var row = localId / columns;
        var x = Margin + column * (TileWidth + Spacing);
        var y = Margin + row * (TileHeight + Spacing);

        return new Rectangle(x, y, TileWidth, TileHeight);
    }
}

public sealed class TiledTileLayer
{
    public string Name { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public bool Visible { get; init; } = true;
    public float Opacity { get; init; } = 1f;
    public TiledTile[] Tiles { get; init; } = [];

    public TiledTile GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return TiledTile.Empty;
        }

        return Tiles[y * Width + x];
    }
}

public sealed class TiledObjectLayer
{
    public string Name { get; init; } = string.Empty;
    public bool Visible { get; init; } = true;
    public float Opacity { get; init; } = 1f;
    public List<TiledMapObject> Objects { get; } = new();
}

public sealed class TiledMapObject
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public RectangleF Bounds { get; init; }
    public Dictionary<string, string> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Intersects(Rectangle bounds)
    {
        return Bounds.Intersects(bounds);
    }

    public string GetProperty(string name, string fallback = "")
    {
        return Properties.TryGetValue(name, out var value) ? value : fallback;
    }

    public int GetIntProperty(string name, int fallback = 0)
    {
        return Properties.TryGetValue(name, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}

public readonly record struct RectangleF(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Intersects(Rectangle rectangle)
    {
        return rectangle.Left < Right &&
            Left < rectangle.Right &&
            rectangle.Top < Bottom &&
            Top < rectangle.Bottom;
    }
}

public readonly record struct TiledTile(
    uint Gid,
    bool FlippedHorizontally,
    bool FlippedVertically,
    bool FlippedDiagonally)
{
    public static readonly TiledTile Empty = new(0, false, false, false);
    public bool IsEmpty => Gid == 0;
}
