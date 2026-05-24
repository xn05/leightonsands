using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands.Maps;

public sealed class Camera2D
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 1f;

    public Rectangle GetVisibleWorldBounds(Viewport viewport)
    {
        var zoom = MathHelper.Max(0.01f, Zoom);
        var width = (int)MathF.Ceiling(viewport.Width / zoom);
        var height = (int)MathF.Ceiling(viewport.Height / zoom);

        return new Rectangle((int)MathF.Floor(Position.X), (int)MathF.Floor(Position.Y), width, height);
    }

    public Matrix GetViewMatrix()
    {
        var zoom = MathHelper.Max(0.01f, Zoom);
        return Matrix.CreateTranslation(new Vector3(-Position, 0f)) * Matrix.CreateScale(zoom);
    }
}
