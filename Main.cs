using System;
using LSUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LeightonSands;

public class Main : Game
{
    private enum Screen
    {
        Title
    }

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private TitleScreen _titleScreen = null!;
    private Screen _currentScreen = Screen.Title;

    public Main()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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
        _titleScreen.LoadContent(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (_currentScreen == Screen.Title)
        {
            _titleScreen.Update(gameTime);
            if (_titleScreen.CloseRequested)
            {
                Exit();
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(20, 22, 26));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_currentScreen == Screen.Title)
        {
            _titleScreen.Draw(_spriteBatch);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
