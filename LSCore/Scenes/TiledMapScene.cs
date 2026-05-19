using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeightonSands.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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
        Map = TiledMapLoader.Load(GetMapPath(content.RootDirectory, _mapPath));
        _renderer = new TiledMapRenderer(Map, content);
        IsLoaded = true;
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (_renderer == null || _graphicsDevice == null)
        {
            return;
        }

        _renderer.Draw(spriteBatch, Camera, _graphicsDevice.Viewport);
    }

    protected IEnumerable<TiledMapObject> GetObjects(string type)
    {
        if (Map == null)
        {
            return [];
        }

        return Map.ObjectLayers
            .Where(layer => layer.Visible)
            .SelectMany(layer => layer.Objects)
            .Where(mapObject => mapObject.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    protected TiledMapObject? FindTransitionTrigger(Rectangle actorBounds)
    {
        foreach (var trigger in GetObjects("Transition"))
        {
            if (trigger.Intersects(actorBounds))
            {
                return trigger;
            }
        }

        return null;
    }

    private static string GetMapPath(string contentRootDirectory, string mapPath)
    {
        if (Path.IsPathRooted(mapPath))
        {
            return mapPath;
        }

        var contentRoot = Path.IsPathRooted(contentRootDirectory)
            ? contentRootDirectory
            : Path.Combine(AppContext.BaseDirectory, contentRootDirectory);

        return Path.GetFullPath(Path.Combine(contentRoot, mapPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
