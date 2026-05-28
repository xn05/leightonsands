using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands.UI;

public static class XamlUIScreenLoader
{
    public static Func<ContentManager, XElement, UIControl>? ImageFactory { get; set; }
    public static Func<ContentManager, XElement, SpriteFont?, UIControl>? ButtonFactory { get; set; }

    public static UIScreen LoadFromResource<TMarker>(ContentManager content, string resourceName, SpriteFont? defaultFont = null)
    {
        var assembly = typeof(TMarker).Assembly;
        var fullResourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith($".{resourceName}", StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidOperationException($"UI resource '{resourceName}' was not found.");
        using var stream = assembly.GetManifestResourceStream(fullResourceName) ??
            throw new InvalidOperationException($"UI resource '{fullResourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        return Load(content, XDocument.Parse(reader.ReadToEnd()), defaultFont);
    }

    private static UIScreen Load(ContentManager content, XDocument document, SpriteFont? defaultFont)
    {
        var screen = new UIScreen();
        var root = document.Root ?? throw new InvalidDataException("UI XAML is missing a root element.");

        foreach (var element in root.Elements())
        {
            if (element.Name.LocalName.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                if (ImageFactory == null)
                {
                    throw new InvalidOperationException("No UI image factory has been registered.");
                }

                screen.Add(ImageFactory(content, element));
            }
            else if (element.Name.LocalName.Equals("Button", StringComparison.OrdinalIgnoreCase))
            {
                if (ButtonFactory == null)
                {
                    throw new InvalidOperationException("No UI button factory has been registered.");
                }

                screen.Add(ButtonFactory(content, element, defaultFont));
            }
        }

        return screen;
    }

    public static Rectangle ReadBounds(XElement element)
    {
        return new Rectangle(
            ReadInt(element, "X"),
            ReadInt(element, "Y"),
            ReadInt(element, "Width"),
            ReadInt(element, "Height"));
    }

    public static string ReadString(XElement element, string name, string? fallback = null)
    {
        var value = (string?)element.Attribute(name);
        if (value != null)
        {
            return value;
        }

        if (fallback != null)
        {
            return fallback;
        }

        throw new InvalidDataException($"Missing required '{name}' attribute on '{element.Name.LocalName}'.");
    }

    public static int ReadInt(XElement element, string name, int fallback = 0)
    {
        var value = (string?)element.Attribute(name);
        return value == null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }

    public static float ReadFloat(XElement element, string name, float fallback)
    {
        var value = (string?)element.Attribute(name);
        return value == null ? fallback : float.Parse(value, CultureInfo.InvariantCulture);
    }

    public static Color ReadColor(XElement element, string name, Color fallback)
    {
        var value = (string?)element.Attribute(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var hex = value.Trim().TrimStart('#');
        if (hex.Length == 6 &&
            byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
            byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
            byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return new Color(r, g, b);
        }

        return fallback;
    }
}
