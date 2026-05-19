using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LeightonSands.Maps;

public static class TiledMapLoader
{
    private const uint FlippedHorizontallyFlag = 0x80000000;
    private const uint FlippedVerticallyFlag = 0x40000000;
    private const uint FlippedDiagonallyFlag = 0x20000000;
    private const uint HexagonalRotationFlag = 0x10000000;
    private const uint TileIdMask = ~(FlippedHorizontallyFlag | FlippedVerticallyFlag | FlippedDiagonallyFlag | HexagonalRotationFlag);

    public static TiledMap Load(string mapPath)
    {
        if (string.IsNullOrWhiteSpace(mapPath))
        {
            throw new ArgumentException("TMX map path is required.", nameof(mapPath));
        }

        var document = XDocument.Load(mapPath, LoadOptions.SetBaseUri);
        var root = document.Root ?? throw new InvalidDataException("TMX file is missing the map root element.");
        var orientation = (string?)root.Attribute("orientation") ?? "orthogonal";
        if (!orientation.Equals("orthogonal", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Only orthogonal Tiled maps are supported right now. Map uses '{orientation}'.");
        }

        var map = new TiledMap
        {
            Name = Path.GetFileNameWithoutExtension(mapPath),
            Directory = Path.GetDirectoryName(Path.GetFullPath(mapPath)) ?? string.Empty,
            Width = ReadRequiredInt(root, "width"),
            Height = ReadRequiredInt(root, "height"),
            TileWidth = ReadRequiredInt(root, "tilewidth"),
            TileHeight = ReadRequiredInt(root, "tileheight")
        };

        foreach (var tilesetElement in root.Elements("tileset"))
        {
            map.Tilesets.Add(LoadTileset(map.Directory, tilesetElement));
        }

        foreach (var layerElement in root.Elements("layer"))
        {
            map.TileLayers.Add(LoadLayer(layerElement, map.Width, map.Height));
        }

        foreach (var objectGroupElement in root.Elements("objectgroup"))
        {
            map.ObjectLayers.Add(LoadObjectLayer(objectGroupElement));
        }

        return map;
    }

    private static TiledTileset LoadTileset(string mapDirectory, XElement tilesetElement)
    {
        var firstGid = ReadRequiredUInt(tilesetElement, "firstgid");
        var source = (string?)tilesetElement.Attribute("source");
        var root = tilesetElement;
        var tilesetDirectory = mapDirectory;

        if (!string.IsNullOrWhiteSpace(source))
        {
            var tilesetPath = Path.GetFullPath(Path.Combine(mapDirectory, source.Replace('/', Path.DirectorySeparatorChar)));
            var document = XDocument.Load(tilesetPath);
            root = document.Root ?? throw new InvalidDataException($"TSX file '{tilesetPath}' is missing the tileset root element.");
            tilesetDirectory = Path.GetDirectoryName(tilesetPath) ?? mapDirectory;
        }

        var image = root.Element("image") ?? throw new InvalidDataException($"Tileset '{(string?)root.Attribute("name")}' is missing an image.");
        var imageSource = ((string?)image.Attribute("source") ?? string.Empty).Replace('\\', '/');
        var absoluteImageSource = Path.GetFullPath(Path.Combine(tilesetDirectory, imageSource.Replace('/', Path.DirectorySeparatorChar)));

        return new TiledTileset
        {
            FirstGid = firstGid,
            Name = (string?)root.Attribute("name") ?? string.Empty,
            TileWidth = ReadRequiredInt(root, "tilewidth"),
            TileHeight = ReadRequiredInt(root, "tileheight"),
            TileCount = ReadInt(root, "tilecount", 0),
            Columns = ReadInt(root, "columns", 0),
            Spacing = ReadInt(root, "spacing", 0),
            Margin = ReadInt(root, "margin", 0),
            ImageSource = absoluteImageSource,
            ImageWidth = ReadInt(image, "width", 0),
            ImageHeight = ReadInt(image, "height", 0)
        };
    }

    private static TiledTileLayer LoadLayer(XElement layerElement, int defaultWidth, int defaultHeight)
    {
        var width = ReadInt(layerElement, "width", defaultWidth);
        var height = ReadInt(layerElement, "height", defaultHeight);
        var data = layerElement.Element("data") ?? throw new InvalidDataException($"Layer '{(string?)layerElement.Attribute("name")}' is missing tile data.");
        var encoding = (string?)data.Attribute("encoding");
        var compression = (string?)data.Attribute("compression");

        if (!string.IsNullOrWhiteSpace(compression))
        {
            throw new NotSupportedException("Compressed TMX layer data is not supported yet. In Tiled, use CSV layer data for now.");
        }

        var gids = encoding?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true
            ? ReadCsvGids(data.Value)
            : ReadXmlTileGids(data);

        var expectedCount = width * height;
        if (gids.Count != expectedCount)
        {
            throw new InvalidDataException($"Layer '{(string?)layerElement.Attribute("name")}' has {gids.Count} tiles, expected {expectedCount}.");
        }

        return new TiledTileLayer
        {
            Name = (string?)layerElement.Attribute("name") ?? string.Empty,
            Width = width,
            Height = height,
            Visible = ReadInt(layerElement, "visible", 1) != 0,
            Opacity = ReadFloat(layerElement, "opacity", 1f),
            Tiles = gids.Select(ParseGid).ToArray()
        };
    }

    private static TiledObjectLayer LoadObjectLayer(XElement objectGroupElement)
    {
        var layer = new TiledObjectLayer
        {
            Name = (string?)objectGroupElement.Attribute("name") ?? string.Empty,
            Visible = ReadInt(objectGroupElement, "visible", 1) != 0,
            Opacity = ReadFloat(objectGroupElement, "opacity", 1f)
        };

        foreach (var objectElement in objectGroupElement.Elements("object"))
        {
            var mapObject = new TiledMapObject
            {
                Id = ReadInt(objectElement, "id", 0),
                Name = (string?)objectElement.Attribute("name") ?? string.Empty,
                Type = (string?)objectElement.Attribute("type") ??
                    (string?)objectElement.Attribute("class") ??
                    string.Empty,
                Bounds = new RectangleF(
                    ReadFloat(objectElement, "x", 0f),
                    ReadFloat(objectElement, "y", 0f),
                    ReadFloat(objectElement, "width", 0f),
                    ReadFloat(objectElement, "height", 0f))
            };

            foreach (var propertyElement in objectElement.Element("properties")?.Elements("property") ?? [])
            {
                var name = (string?)propertyElement.Attribute("name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                mapObject.Properties[name] = (string?)propertyElement.Attribute("value") ?? propertyElement.Value;
            }

            layer.Objects.Add(mapObject);
        }

        return layer;
    }

    private static List<uint> ReadCsvGids(string csv)
    {
        return csv
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => uint.Parse(value, CultureInfo.InvariantCulture))
            .ToList();
    }

    private static List<uint> ReadXmlTileGids(XElement data)
    {
        return data
            .Elements("tile")
            .Select(tile => ReadRequiredUInt(tile, "gid"))
            .ToList();
    }

    private static TiledTile ParseGid(uint rawGid)
    {
        return new TiledTile(
            rawGid & TileIdMask,
            (rawGid & FlippedHorizontallyFlag) != 0,
            (rawGid & FlippedVerticallyFlag) != 0,
            (rawGid & FlippedDiagonallyFlag) != 0);
    }

    private static int ReadRequiredInt(XElement element, string name)
    {
        var value = (string?)element.Attribute(name);
        return value == null
            ? throw new InvalidDataException($"Missing required '{name}' attribute on '{element.Name}'.")
            : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static uint ReadRequiredUInt(XElement element, string name)
    {
        var value = (string?)element.Attribute(name);
        return value == null
            ? throw new InvalidDataException($"Missing required '{name}' attribute on '{element.Name}'.")
            : uint.Parse(value, CultureInfo.InvariantCulture);
    }

    private static int ReadInt(XElement element, string name, int fallback)
    {
        var value = (string?)element.Attribute(name);
        return value == null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float ReadFloat(XElement element, string name, float fallback)
    {
        var value = (string?)element.Attribute(name);
        return value == null ? fallback : float.Parse(value, CultureInfo.InvariantCulture);
    }
}
