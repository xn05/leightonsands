using System;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _uiFont = null!;
    private Texture2D _titleTexture = null!;
    private Texture2D _leftArrowTexture = null!;
    private Texture2D _rightArrowTexture = null!;
    private Texture2D _buttonTexture = null!;
    private Texture2D _pixel = null!;
    private Screen _currentScreen = Screen.Title;
    private KeyboardState _previousKeyboard;
    private CharacterRegistry _characterRegistry = new();
    private CharacterDefinition? _selectedCharacter;
    private SpriteAnimation? _selectedIdleAnimation;
    private double _characterAnimTime;

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
        _uiFont = Content.Load<SpriteFont>("Font/Main");
        _titleTexture = Content.Load<Texture2D>("Textures/UI/titletext");
        _leftArrowTexture = Content.Load<Texture2D>("Textures/UI/Buttons/left");
        _rightArrowTexture = Content.Load<Texture2D>("Textures/UI/Buttons/right");
        _buttonTexture = Content.Load<Texture2D>("Textures/UI/Buttons/button_main_3x1");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        LoadCharacters();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (_currentScreen == Screen.Title)
        {
            UpdateTitle(gameTime);
        }

        _previousKeyboard = Keyboard.GetState();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(20, 22, 26));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_currentScreen == Screen.Title)
        {
            DrawTitle();
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateTitle(GameTime gameTime)
    {
        _characterAnimTime += gameTime.ElapsedGameTime.TotalSeconds;

        var keyboard = Keyboard.GetState();
        if (WasPressed(keyboard, Keys.Left))
        {
            SelectPreviousCharacter();
        }

        if (WasPressed(keyboard, Keys.Right))
        {
            SelectNextCharacter();
        }

        if (keyboard.IsKeyDown(Keys.Enter) || keyboard.IsKeyDown(Keys.Space))
        {
            // TODO: transition to the first playable screen when it exists.
        }
    }

    private void DrawTitle()
    {
        var viewport = GraphicsDevice.Viewport;
        var centerX = viewport.Width * 0.5f;

        var characterTop = viewport.Height * 0.07f;
        var titleScale = GetTitleScale(viewport.Width);
        var titleSize = new Vector2(_titleTexture.Width, _titleTexture.Height) * titleScale;
        var titleTop = viewport.Height * 0.32f;
        var titlePosition = new Vector2(centerX - titleSize.X * 0.5f, titleTop);

        DrawCharacterSelect(viewport, characterTop);
        _spriteBatch.Draw(_titleTexture, titlePosition, null, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        var buttonsTop = titlePosition.Y + titleSize.Y + viewport.Height * 0.06f;
        DrawMenuButtons(viewport, buttonsTop);
    }

    private void DrawCharacterSelect(Viewport viewport, float topY)
    {
        if (_selectedIdleAnimation == null || _selectedCharacter == null)
        {
            return;
        }

        var anim = _selectedIdleAnimation;
        var frame = anim.GetFrame("forward", _characterAnimTime);
        var scale = MathHelper.Clamp(_selectedCharacter.Scale, 0.1f, 4f);
        var frameSize = new Vector2(anim.FrameWidth, anim.FrameHeight) * scale;
        var centerX = viewport.Width * 0.5f;
        var spritePosition = new Vector2(centerX - frameSize.X * 0.5f, topY);

        _spriteBatch.Draw(anim.Texture, spritePosition, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        var arrowSpacing = frameSize.X * 0.5f + 32f;
        var arrowScale = 1f;
        var leftPos = new Vector2(centerX - arrowSpacing - _leftArrowTexture.Width * arrowScale, topY + frameSize.Y * 0.35f);
        var rightPos = new Vector2(centerX + arrowSpacing, topY + frameSize.Y * 0.35f);
        var arrowTint = _characterRegistry.Characters.Count > 1 ? Color.White : new Color(120, 120, 120);

        _spriteBatch.Draw(_leftArrowTexture, leftPos, null, arrowTint, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 0f);
        _spriteBatch.Draw(_rightArrowTexture, rightPos, null, arrowTint, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 0f);
    }

    private void DrawMenuButtons(Viewport viewport, float startY)
    {
        var buttonWidth = viewport.Width * 0.32f;
        var buttonHeight = viewport.Height * 0.10f;
        var gap = viewport.Height * 0.02f;
        var centerX = viewport.Width * 0.5f;
        var labels = new[] { "NEW GAME", "OPEN GAME", "SETTINGS" };

        for (var i = 0; i < labels.Length; i++)
        {
            var rect = new Rectangle(
                (int)(centerX - buttonWidth * 0.5f),
                (int)(startY + i * (buttonHeight + gap)),
                (int)buttonWidth,
                (int)buttonHeight);

            DrawButton(rect, labels[i]);
        }
    }

    private void DrawButton(Rectangle rect, string label)
    {
        DrawButtonTexture(rect);

        var textSize = _uiFont.MeasureString(label);
        var textPos = new Vector2(
            rect.X + (rect.Width - textSize.X) * 0.5f,
            rect.Y + (rect.Height - textSize.Y) * 0.5f);
        _spriteBatch.DrawString(_uiFont, label, textPos, Color.White);
    }

    private void DrawButtonTexture(Rectangle rect)
    {
        var sliceWidth = _buttonTexture.Width / 3;
        var leftWidth = sliceWidth;
        var rightWidth = sliceWidth;
        var centerWidth = Math.Max(0, rect.Width - leftWidth - rightWidth);

        var sourceLeft = new Rectangle(0, 0, sliceWidth, _buttonTexture.Height);
        var sourceCenter = new Rectangle(sliceWidth, 0, sliceWidth, _buttonTexture.Height);
        var sourceRight = new Rectangle(sliceWidth * 2, 0, sliceWidth, _buttonTexture.Height);

        var destLeft = new Rectangle(rect.X, rect.Y, leftWidth, rect.Height);
        var destCenter = new Rectangle(rect.X + leftWidth, rect.Y, centerWidth, rect.Height);
        var destRight = new Rectangle(rect.Right - rightWidth, rect.Y, rightWidth, rect.Height);

        _spriteBatch.Draw(_buttonTexture, destLeft, sourceLeft, Color.White);
        if (centerWidth > 0)
        {
            _spriteBatch.Draw(_buttonTexture, destCenter, sourceCenter, Color.White);
        }
        _spriteBatch.Draw(_buttonTexture, destRight, sourceRight, Color.White);
    }

    private void LoadCharacters()
    {
        var registryPath = GetContentPath("Characters/characters.json");
        if (!File.Exists(registryPath))
        {
            return;
        }

        var json = File.ReadAllText(registryPath);
        _characterRegistry = JsonSerializer.Deserialize<CharacterRegistry>(json, _jsonOptions) ?? new CharacterRegistry();
        var selectable = _characterRegistry.Characters.Where(character => character.Selectable).ToList();
        if (selectable.Count == 0)
        {
            return;
        }

        _characterRegistry.Characters = selectable;
        SelectCharacter(0);
    }

    private void SelectNextCharacter()
    {
        if (_characterRegistry.Characters.Count <= 1)
        {
            return;
        }

        var index = _characterRegistry.Characters.IndexOf(_selectedCharacter!);
        SelectCharacter((index + 1) % _characterRegistry.Characters.Count);
    }

    private void SelectPreviousCharacter()
    {
        if (_characterRegistry.Characters.Count <= 1)
        {
            return;
        }

        var index = _characterRegistry.Characters.IndexOf(_selectedCharacter!);
        var nextIndex = index - 1;
        if (nextIndex < 0)
        {
            nextIndex = _characterRegistry.Characters.Count - 1;
        }

        SelectCharacter(nextIndex);
    }

    private void SelectCharacter(int index)
    {
        if (index < 0 || index >= _characterRegistry.Characters.Count)
        {
            return;
        }

        _selectedCharacter = _characterRegistry.Characters[index];
        _characterAnimTime = 0;
        _selectedIdleAnimation = LoadIdleAnimation(_selectedCharacter);
    }

    private SpriteAnimation? LoadIdleAnimation(CharacterDefinition character)
    {
        if (!character.Animations.TryGetValue("idle", out var animationPath))
        {
            return null;
        }

        var definitionPath = GetContentPath(Path.Combine("Characters", animationPath));
        if (!File.Exists(definitionPath))
        {
            return null;
        }

        var json = File.ReadAllText(definitionPath);
        var definition = JsonSerializer.Deserialize<AnimationDefinition>(json, _jsonOptions);
        if (definition == null || string.IsNullOrWhiteSpace(definition.Texture))
        {
            return null;
        }

        var texture = LoadTextureFromContent(definition.Texture);
        if (texture == null)
        {
            return null;
        }

        return new SpriteAnimation(definition, texture);
    }

    private Texture2D? LoadTextureFromContent(string relativePath)
    {
        var texturePath = GetContentPath(relativePath);
        if (!File.Exists(texturePath))
        {
            return null;
        }

        using var stream = File.OpenRead(texturePath);
        return Texture2D.FromStream(GraphicsDevice, stream);
    }

    private string GetContentPath(string relativePath)
    {
        var safePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, safePath);
    }

    private bool WasPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    private float GetTitleScale(int viewportWidth)
    {
        if (_titleTexture.Width == 0)
        {
            return 1f;
        }

        var targetWidth = viewportWidth * 0.78f;
        var scale = targetWidth / _titleTexture.Width;
        return MathHelper.Clamp(scale, 0.1f, 3f);
    }
}