using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands;

public sealed class UIDraw
{
    private readonly Texture2D _texture;

    public UIDraw(ContentManager content, string resourceName, Rectangle bounds, bool stretchToFit = true, float layerDepth = 0f)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new ArgumentException("A content resource name is required.", nameof(resourceName));
        }

        _texture = content.Load<Texture2D>(NormalizeResourceName(resourceName));
        Bounds = bounds;
        StretchToFit = stretchToFit;
        LayerDepth = layerDepth;
    }

    public UIDraw(Texture2D texture, Rectangle bounds, bool stretchToFit = true, float layerDepth = 0f)
    {
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        Bounds = bounds;
        StretchToFit = stretchToFit;
        LayerDepth = layerDepth;
    }

    public Texture2D Texture => _texture;
    public Rectangle Bounds { get; set; }
    public bool StretchToFit { get; set; }
    public float LayerDepth { get; set; }
    public Color Tint { get; set; } = Color.White;

    public int X
    {
        get => Bounds.X;
        set => Bounds = new Rectangle(value, Bounds.Y, Bounds.Width, Bounds.Height);
    }

    public int Y
    {
        get => Bounds.Y;
        set => Bounds = new Rectangle(Bounds.X, value, Bounds.Width, Bounds.Height);
    }

    public int Width
    {
        get => Bounds.Width;
        set => Bounds = new Rectangle(Bounds.X, Bounds.Y, value, Bounds.Height);
    }

    public int Height
    {
        get => Bounds.Height;
        set => Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, value);
    }

    public void SetPosition(int x, int y)
    {
        Bounds = new Rectangle(x, y, Bounds.Width, Bounds.Height);
    }

    public void SetSize(int width, int height)
    {
        Bounds = new Rectangle(Bounds.X, Bounds.Y, width, height);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (spriteBatch == null)
        {
            throw new ArgumentNullException(nameof(spriteBatch));
        }

        spriteBatch.Draw(
            _texture,
            ResolveDestination(),
            sourceRectangle: null,
            color: Tint,
            rotation: 0f,
            origin: Vector2.Zero,
            effects: SpriteEffects.None,
            layerDepth: LayerDepth);
    }

    private Rectangle ResolveDestination()
    {
        if (StretchToFit || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return Bounds;
        }

        var scale = MathF.Min(Bounds.Width / (float)_texture.Width, Bounds.Height / (float)_texture.Height);
        var width = Math.Max(1, (int)MathF.Round(_texture.Width * scale));
        var height = Math.Max(1, (int)MathF.Round(_texture.Height * scale));
        var x = Bounds.X + (Bounds.Width - width) / 2;
        var y = Bounds.Y + (Bounds.Height - height) / 2;

        return new Rectangle(x, y, width, height);
    }

    private static string NormalizeResourceName(string resourceName)
    {
        var safeName = resourceName.Replace('\\', '/');
        return safeName.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase)
            ? safeName[..^4]
            : safeName;
    }
}
