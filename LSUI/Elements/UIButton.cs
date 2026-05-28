using System;
using LeightonSands.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LSUI.Elements;

public enum UIButtonKind
{
    Normal,
    Toggle
}

public sealed class UIButton : UIControl
{
    private const float SpriteFontSize = 16f;

    private readonly Texture2D _normalTexture;
    private readonly Texture2D _hoveredTexture;
    private readonly Texture2D _pressedTexture;
    private readonly int _sliceColumns;

    private MouseState _previousMouse;
    private bool _pressStartedInside;

    public UIButton(
        string name,
        UIButtonKind kind,
        Texture2D normalTexture,
        Texture2D hoveredTexture,
        Texture2D pressedTexture,
        Rectangle bounds,
        int sliceColumns = 1)
    {
        Name = name;
        Kind = kind;
        _normalTexture = normalTexture;
        _hoveredTexture = hoveredTexture;
        _pressedTexture = pressedTexture;
        Bounds = bounds;
        _sliceColumns = Math.Max(1, sliceColumns);
    }

    public UIButtonKind Kind { get; }
    public string Text { get; set; } = string.Empty;
    public SpriteFont? Font { get; set; }
    public float FontSize { get; set; } = SpriteFontSize;
    public Color TextColor { get; set; } = Color.White;
    public bool IsHovered { get; private set; }
    public bool IsChecked { get; private set; }
    public bool WasClicked { get; private set; }
    public bool IsPressed => _pressStartedInside && IsHovered;

    public override void Update(MouseState mouse)
    {
        WasClicked = false;
        IsHovered = Bounds.Contains(mouse.Position);

        if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
        {
            _pressStartedInside = IsHovered;
        }

        if (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
        {
            WasClicked = _pressStartedInside && IsHovered;
            if (WasClicked && Kind == UIButtonKind.Toggle)
            {
                IsChecked = !IsChecked;
            }

            _pressStartedInside = false;
        }

        if (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Released)
        {
            _pressStartedInside = false;
        }

        _previousMouse = mouse;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible)
        {
            return;
        }

        var texture = ResolveTexture();
        if (_sliceColumns > 1)
        {
            DrawHorizontalSliceTexture(spriteBatch, texture, Bounds, _sliceColumns);
        }
        else
        {
            spriteBatch.Draw(texture, Bounds, Color.White);
        }

        DrawText(spriteBatch);
    }

    private Texture2D ResolveTexture()
    {
        if (IsPressed || IsChecked)
        {
            return _pressedTexture;
        }

        return IsHovered ? _hoveredTexture : _normalTexture;
    }

    private void DrawText(SpriteBatch spriteBatch)
    {
        if (Font == null || string.IsNullOrEmpty(Text))
        {
            return;
        }

        var textScale = FontSize / SpriteFontSize;
        var textSize = Font.MeasureString(Text);
        var scaledTextSize = textSize * textScale;
        var textPos = new Vector2(
            Bounds.X + (Bounds.Width - scaledTextSize.X) * 0.5f,
            Bounds.Y + (Bounds.Height - scaledTextSize.Y) * 0.5f);

        spriteBatch.DrawString(Font, Text, textPos, TextColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
    }

    private static void DrawHorizontalSliceTexture(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, int columns)
    {
        var sliceWidth = texture.Width / columns;
        var scaledSliceWidth = (int)MathF.Round(sliceWidth * (rect.Height / (float)texture.Height));
        var leftWidth = Math.Min(scaledSliceWidth, rect.Width / 2);
        var rightWidth = Math.Min(scaledSliceWidth, rect.Width - leftWidth);
        var centerWidth = Math.Max(0, rect.Width - leftWidth - rightWidth);
        var centerColumnCount = Math.Max(0, columns - 2);

        var sourceLeft = new Rectangle(0, 0, sliceWidth, texture.Height);
        var sourceCenter = new Rectangle(sliceWidth, 0, sliceWidth * centerColumnCount, texture.Height);
        var sourceRight = new Rectangle(sliceWidth * (columns - 1), 0, sliceWidth, texture.Height);

        var destLeft = new Rectangle(rect.X, rect.Y, leftWidth, rect.Height);
        var destCenter = new Rectangle(rect.X + leftWidth, rect.Y, centerWidth, rect.Height);
        var destRight = new Rectangle(rect.Right - rightWidth, rect.Y, rightWidth, rect.Height);

        spriteBatch.Draw(texture, destLeft, sourceLeft, Color.White);
        if (centerWidth > 0 && centerColumnCount > 0)
        {
            spriteBatch.Draw(texture, destCenter, sourceCenter, Color.White);
        }

        spriteBatch.Draw(texture, destRight, sourceRight, Color.White);
    }
}
