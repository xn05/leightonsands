using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace LeightonSands.Maps;

public sealed class TiledCollisionMap
{
    private readonly TiledMap _map;

    public TiledCollisionMap(TiledMap map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
    }

    public bool IntersectsBlockedArea(Rectangle bounds)
    {
        return IntersectsBlockedTile(bounds) || IntersectsBlockedObject(bounds);
    }

    private bool IntersectsBlockedTile(Rectangle bounds)
    {
        var startX = Math.Clamp(bounds.Left / _map.TileWidth, 0, _map.Width - 1);
        var startY = Math.Clamp(bounds.Top / _map.TileHeight, 0, _map.Height - 1);
        var endX = Math.Clamp((bounds.Right - 1) / _map.TileWidth, 0, _map.Width - 1);
        var endY = Math.Clamp((bounds.Bottom - 1) / _map.TileHeight, 0, _map.Height - 1);

        foreach (var layer in _map.TileLayers.Where(IsCollisionLayer))
        {
            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    if (!layer.GetTile(x, y).IsEmpty)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IntersectsBlockedObject(Rectangle bounds)
    {
        foreach (var layer in _map.ObjectLayers)
        {
            var collisionLayer = IsCollisionName(layer.Name);
            foreach (var mapObject in layer.Objects)
            {
                if (IsBlockingObject(collisionLayer, mapObject) && mapObject.Intersects(bounds))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsBlockingObject(bool collisionLayer, TiledMapObject mapObject)
    {
        if (IsTransitionObject(mapObject))
        {
            return false;
        }

        return collisionLayer || string.IsNullOrWhiteSpace(mapObject.Type) || IsCollisionName(mapObject.Type);
    }

    private static bool IsTransitionObject(TiledMapObject mapObject)
    {
        return mapObject.Type.Equals("Transition", StringComparison.OrdinalIgnoreCase) ||
            mapObject.Properties.ContainsKey("TargetScene") ||
            mapObject.Properties.ContainsKey("TargetRegion");
    }

    private static bool IsCollisionLayer(TiledTileLayer layer)
    {
        return IsCollisionName(layer.Name);
    }

    private static bool IsCollisionName(string value)
    {
        return value.Equals("Collision", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Collisions", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Solid", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Blocker", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("NoPass", StringComparison.OrdinalIgnoreCase);
    }
}
