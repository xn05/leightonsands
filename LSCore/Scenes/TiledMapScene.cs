using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeightonSands.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TiledMap = MonoGame.Extended.Tiled.TiledMap;
using TiledMapObject = MonoGame.Extended.Tiled.TiledMapObject;
using TiledMapRenderer = MonoGame.Extended.Tiled.Renderers.TiledMapRenderer;

namespace LeightonSands.Scenes;

public class TiledMapScene : IGameScene
{
    private readonly string _mapPath;
    private TiledMapRenderer? _renderer;
    private GraphicsDevice? _graphicsDevice;

    public TiledMapScene(string mapPath)
    {
        _mapPath = mapPath;
    }

    public Camera2D Camera { get; } = new();
    public TiledMap? Map { get; private set; }
    public bool IsLoaded { get; private set; }

    public virtual void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        if (IsLoaded)
        {
            return;
        }

        _graphicsDevice = graphicsDevice;
        Map = content.Load<TiledMap>(GetContentName(_mapPath));
        _renderer = new TiledMapRenderer(graphicsDevice, Map);
        IsLoaded = true;
    }

    public virtual void Update(GameTime gameTime)
    {
        _renderer?.Update(gameTime);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (_renderer == null || _graphicsDevice == null)
        {
            return;
        }

        var previousBlendState = _graphicsDevice.BlendState;
        var previousSamplerState = _graphicsDevice.SamplerStates[0];
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        _renderer.Draw(Camera.GetViewMatrix());
        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.SamplerStates[0] = previousSamplerState;
    }

    protected IEnumerable<TiledMapObject> GetObjects(string type)
    {
        if (Map == null)
        {
            return [];
        }

        return Map.ObjectLayers
            .Where(layer => layer.IsVisible)
            .SelectMany(layer => layer.Objects)
            .Where(mapObject => mapObject.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    protected TiledMapObject? FindTransitionTrigger(Rectangle actorBounds)
    {
        foreach (var trigger in GetObjects("Transition"))
        {
            if (Intersects(trigger, actorBounds))
            {
                return trigger;
            }
        }

        return null;
    }

    private static bool Intersects(TiledMapObject mapObject, Rectangle bounds)
    {
        var position = mapObject.Position;
        var size = mapObject.Size;
        return bounds.Left < position.X + size.Width &&
            position.X < bounds.Right &&
            bounds.Top < position.Y + size.Height &&
            position.Y < bounds.Bottom;
    }

    private static string GetContentName(string mapPath)
    {
        var safePath = mapPath.Replace('\\', '/');
        return Path.ChangeExtension(safePath, null) ?? safePath;
    }
}
