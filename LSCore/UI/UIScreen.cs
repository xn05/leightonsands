using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LeightonSands.UI;

public sealed class UIScreen
{
    private readonly List<UIControl> _controls = new();
    private readonly Dictionary<string, UIControl> _controlsByName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<UIControl> Controls => _controls;

    public void Add(UIControl control)
    {
        _controls.Add(control);
        if (!string.IsNullOrWhiteSpace(control.Name))
        {
            _controlsByName[control.Name] = control;
        }
    }

    public T Get<T>(string name) where T : UIControl
    {
        return (T)_controlsByName[name];
    }

    public void Update(MouseState mouse)
    {
        foreach (var control in _controls)
        {
            control.Update(mouse);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var control in _controls)
        {
            control.Draw(spriteBatch);
        }
    }
}
