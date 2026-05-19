using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands.Scenes;

public sealed class SceneManager
{
    private enum TransitionState
    {
        None,
        FadingOut,
        FadingIn
    }

    private readonly Dictionary<string, IGameScene> _scenes = new(StringComparer.OrdinalIgnoreCase);
    private ContentManager? _content;
    private GraphicsDevice? _graphicsDevice;
    private IGameScene? _currentScene;
    private Texture2D? _fadeTexture;
    private TransitionState _transitionState;
    private string _pendingSceneName = string.Empty;
    private float _fadeAlpha;
    private float _fadeTimer;
    private float _fadeDuration = 0.35f;

    public string CurrentSceneName { get; private set; } = string.Empty;
    public IGameScene? CurrentScene => _currentScene;

    public void Initialize(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _fadeTexture = new Texture2D(graphicsDevice, 1, 1);
        _fadeTexture.SetData([Color.White]);
    }

    public void Register(string name, IGameScene scene)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Scene name is required.", nameof(name));
        }

        _scenes[name] = scene ?? throw new ArgumentNullException(nameof(scene));
    }

    public void ChangeScene(string name)
    {
        ChangeSceneImmediate(name);
        _transitionState = TransitionState.None;
        _fadeAlpha = 0f;
        _fadeTimer = 0f;
        _pendingSceneName = string.Empty;
    }

    public void TransitionTo(string name, float fadeDurationSeconds = 0.35f)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Scene name is required.", nameof(name));
        }

        if (_transitionState != TransitionState.None || name.Equals(CurrentSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _pendingSceneName = name;
        _fadeDuration = Math.Max(0.01f, fadeDurationSeconds);
        _fadeTimer = 0f;
        _fadeAlpha = 0f;
        _transitionState = TransitionState.FadingOut;
    }

    public void Update(GameTime gameTime)
    {
        _currentScene?.Update(gameTime);
        UpdateTransition(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _currentScene?.Draw(spriteBatch);
        DrawTransition(spriteBatch);
    }

    private void ChangeSceneImmediate(string name)
    {
        if (_content == null || _graphicsDevice == null)
        {
            throw new InvalidOperationException("SceneManager must be initialized before changing scenes.");
        }

        if (!_scenes.TryGetValue(name, out var scene))
        {
            throw new KeyNotFoundException($"Scene '{name}' was not registered.");
        }

        _currentScene = scene;
        CurrentSceneName = name;
        scene.LoadContent(_content, _graphicsDevice);
    }

    private void UpdateTransition(GameTime gameTime)
    {
        if (_transitionState == TransitionState.None)
        {
            return;
        }

        _fadeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        var progress = Math.Clamp(_fadeTimer / _fadeDuration, 0f, 1f);

        if (_transitionState == TransitionState.FadingOut)
        {
            _fadeAlpha = progress;
            if (progress >= 1f)
            {
                ChangeSceneImmediate(_pendingSceneName);
                _transitionState = TransitionState.FadingIn;
                _fadeTimer = 0f;
            }

            return;
        }

        _fadeAlpha = 1f - progress;
        if (progress >= 1f)
        {
            _transitionState = TransitionState.None;
            _fadeAlpha = 0f;
            _fadeTimer = 0f;
            _pendingSceneName = string.Empty;
        }
    }

    private void DrawTransition(SpriteBatch spriteBatch)
    {
        if (_fadeTexture == null || _graphicsDevice == null || _fadeAlpha <= 0f)
        {
            return;
        }

        spriteBatch.Draw(_fadeTexture, _graphicsDevice.Viewport.Bounds, Color.Black * _fadeAlpha);
    }
}
