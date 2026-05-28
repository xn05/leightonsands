using LeightonSands.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LSUI.Elements;

public sealed class UIImage : UIControl
{
    private readonly Texture2D _texture;

    public UIImage(string name, Texture2D texture, Rectangle bounds)
    {
        Name = name;
        _texture = texture;
        Bounds = bounds;
    }

    public Color Tint { get; set; } = Color.White;

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible)
        {
            return;
        }

        spriteBatch.Draw(_texture, Bounds, Tint);
    }
}
