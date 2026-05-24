using System;
using System.Collections.Generic;
using LeightonSands.Maps;
using LeightonSands.Scenes;
using LSUI.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LeightonSands;

public class Main : Game
{
    private static readonly TimeSpan ActiveFrameTime = TimeSpan.FromSeconds(1.0 / 60.0);
    private static readonly TimeSpan InactiveFrameTime = TimeSpan.FromSeconds(1.0 / 10.0);
    private const string TitleSceneName = "title";
    private const string StartingRegionId = "cave1";

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private TitleScreen _titleScreen = null!;
    private readonly SceneManager _sceneManager = new();
    private readonly Dictionary<string, WorldMapScene> _worldScenes = new(StringComparer.OrdinalIgnoreCase);

    public Main()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.SynchronizeWithVerticalRetrace = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = ActiveFrameTime;
        InactiveSleepTime = InactiveFrameTime;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _titleScreen = new TitleScreen();
        _sceneManager.Initialize(Content, GraphicsDevice);
        _sceneManager.Register(TitleSceneName, _titleScreen);
        RegisterMapRegions();
        _sceneManager.ChangeScene(TitleSceneName);
    }

    protected override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            TargetElapsedTime = InactiveFrameTime;
            base.Update(gameTime);
            return;
        }

        TargetElapsedTime = ActiveFrameTime;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        _sceneManager.Update(gameTime);
        if (_titleScreen.NewGameRequested)
        {
            _titleScreen.ClearNewGameRequest();
            if (_worldScenes.TryGetValue(StartingRegionId, out var startingScene))
            {
                startingScene.StartNewGame(_titleScreen.SelectedCharacterId);
            }

            _sceneManager.TransitionTo(StartingRegionId, 0.45f);
        }

        if (_titleScreen.CloseRequested)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!IsActive || Window.ClientBounds.Width <= 0 || Window.ClientBounds.Height <= 0)
        {
            base.Draw(gameTime);
            return;
        }

        GraphicsDevice.Clear(new Color(20, 22, 26));

        _sceneManager.Draw(_spriteBatch);

        base.Draw(gameTime);
    }

    private void RegisterMapRegions()
    {
        var registry = Content.Load<MapRegionRegistry>("Maps/Regions");

        foreach (var region in registry.Regions)
        {
            if (string.IsNullOrWhiteSpace(region.Id) || string.IsNullOrWhiteSpace(region.Map))
            {
                continue;
            }

            var scene = new WorldMapScene(region);
            _worldScenes[region.Id] = scene;
            _sceneManager.Register(region.Id, scene);
        }
    }
}
