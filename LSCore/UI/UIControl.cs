using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LeightonSands.UI;

public abstract class UIControl
{
    public string Name { get; init; } = string.Empty;
    public Rectangle Bounds { get; set; }
    public bool IsVisible { get; set; } = true;

    public virtual void Update(MouseState mouse)
    {
    }

    public abstract void Draw(SpriteBatch spriteBatch);
}
