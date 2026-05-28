using System;
using System.Xml.Linq;
using LeightonSands.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LSUI.Elements;

public static class UIElementFactory
{
    private static bool _isRegistered;

    public static void Register()
    {
        if (_isRegistered)
        {
            return;
        }

        XamlUIScreenLoader.ImageFactory = CreateImage;
        XamlUIScreenLoader.ButtonFactory = CreateButton;
        _isRegistered = true;
    }

    private static UIControl CreateImage(ContentManager content, XElement element)
    {
        return new UIImage(
            XamlUIScreenLoader.ReadString(element, "Name"),
            content.Load<Texture2D>(XamlUIScreenLoader.ReadString(element, "Texture")),
            XamlUIScreenLoader.ReadBounds(element));
    }

    private static UIControl CreateButton(ContentManager content, XElement element, SpriteFont? defaultFont)
    {
        var kind = Enum.TryParse<UIButtonKind>(
                XamlUIScreenLoader.ReadString(element, "Type", "Normal"),
                ignoreCase: true,
                out var parsedKind)
            ? parsedKind
            : UIButtonKind.Normal;

        var button = new UIButton(
            XamlUIScreenLoader.ReadString(element, "Name"),
            kind,
            content.Load<Texture2D>(XamlUIScreenLoader.ReadString(element, "Texture")),
            content.Load<Texture2D>(XamlUIScreenLoader.ReadString(element, "HoveredTexture")),
            content.Load<Texture2D>(XamlUIScreenLoader.ReadString(element, "PressedTexture")),
            XamlUIScreenLoader.ReadBounds(element),
            XamlUIScreenLoader.ReadInt(element, "SliceColumns", 1));

        button.Text = XamlUIScreenLoader.ReadString(element, "Text", string.Empty);
        button.Font = defaultFont;
        button.FontSize = XamlUIScreenLoader.ReadFloat(element, "FontSize", 16f);
        button.TextColor = XamlUIScreenLoader.ReadColor(element, "TextColor", Color.White);
        return button;
    }
}
