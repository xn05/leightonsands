using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LSUI.Elements;

public enum MainButtonTextureShape
{
    OneByOne = 1,
    TwoByOne = 2,
    ThreeByOne = 3,
    FourByOne = 4
}

public enum MainButtonActivationMode
{
    OnRelease,
    OnPress
}

public sealed class MainButton
{
    private const float SpriteFontSize = 16f;

    private readonly Texture2D _normalTexture;
    private readonly Texture2D _hoveredTexture;
    private readonly Texture2D _pressedTexture;
    private readonly int _textureColumns;
    private readonly bool _useHorizontalSlices;

    private MouseState _previousMouse;
    private bool _pressStartedInside;

    private MainButton(
        Texture2D normalTexture,
        Texture2D hoveredTexture,
        Texture2D pressedTexture,
        Rectangle bounds,
        int textureColumns,
        bool useHorizontalSlices)
    {
        _normalTexture = normalTexture;
        _hoveredTexture = hoveredTexture;
        _pressedTexture = pressedTexture;
        Bounds = bounds;
        _textureColumns = textureColumns;
        _useHorizontalSlices = useHorizontalSlices;
    }

    public Rectangle Bounds { get; set; }
    public string Text { get; set; } = string.Empty;
    public SpriteFont? Font { get; set; }
    public float FontSize { get; set; } = SpriteFontSize;
    public Color TextColor { get; set; } = Color.White;
    public MainButtonActivationMode ActivationMode { get; set; } = MainButtonActivationMode.OnRelease;
    public bool IsHovered { get; private set; }
    public bool IsPressed => _pressStartedInside && IsHovered;

    public static MainButton FromMainTexture(
        ContentManager content,
        Rectangle bounds,
        MainButtonTextureShape shape,
        string text = "",
        SpriteFont? font = null,
        MainButtonActivationMode activationMode = MainButtonActivationMode.OnRelease,
        float fontSize = SpriteFontSize)
    {
        var textureName = $"Textures/UI/Buttons/button_main_{(int)shape}x1";
        var button = new MainButton(
            content.Load<Texture2D>(textureName),
            content.Load<Texture2D>($"{textureName}_hovered"),
            content.Load<Texture2D>($"{textureName}_pressed"),
            bounds,
            (int)shape,
            shape != MainButtonTextureShape.OneByOne);

        button.Text = text;
        button.Font = font;
        button.ActivationMode = activationMode;
        button.FontSize = fontSize;
        return button;
    }

    public static MainButton FromTexture(
        ContentManager content,
        Rectangle bounds,
        string textureName,
        MainButtonActivationMode activationMode = MainButtonActivationMode.OnRelease)
    {
        var button = new MainButton(
            content.Load<Texture2D>(textureName),
            content.Load<Texture2D>($"{textureName}_hovered"),
            content.Load<Texture2D>($"{textureName}_pressed"),
            bounds,
            1,
            false);

        button.ActivationMode = activationMode;
        return button;
    }

    public bool Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);

        var activated = false;
        if (WasPressed(mouse))
        {
            _pressStartedInside = IsHovered;
            activated = ActivationMode == MainButtonActivationMode.OnPress && _pressStartedInside;
        }

        if (WasReleased(mouse))
        {
            activated = ActivationMode == MainButtonActivationMode.OnRelease && _pressStartedInside && IsHovered;
            _pressStartedInside = false;
        }

        if (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Released)
        {
            _pressStartedInside = false;
        }

        _previousMouse = mouse;
        return activated;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var texture = ResolveTexture();
        if (_useHorizontalSlices)
        {
            DrawHorizontalSliceTexture(spriteBatch, texture, Bounds, _textureColumns);
        }
        else
        {
            spriteBatch.Draw(texture, Bounds, Color.White);
        }

        DrawText(spriteBatch);
    }

    private Texture2D ResolveTexture()
    {
        if (IsPressed)
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

    private bool WasPressed(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    }

    private bool WasReleased(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed;
    }
}
