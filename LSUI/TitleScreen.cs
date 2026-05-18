using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LeightonSands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LSUI;

public sealed class TitleScreen
{
    private const int TitleWidth = 726;
    private const int TitleHeight = 55;
    private const int TitleX = 277;
    private const int TitleY = 285;
    private const int MainCharacterAreaSize = 240;
    private const int PreviewCharacterAreaSize = 200;
    private const int MainCharacterX = 520;
    private const int MainCharacterY = 14;
    private const int LeftPreviewX = 289;
    private const int LeftPreviewY = 34;
    private const int RightPreviewX = 789;
    private const int RightPreviewY = 34;
    private const int ArrowWidth = 39;
    private const int ArrowHeight = 64;
    private const int LeftArrowX = 490;
    private const int LeftArrowY = 122;
    private const int RightArrowX = 751;
    private const int RightArrowY = 122;
    private const int ButtonWidth = 340;
    private const int ButtonHeight = 85;
    private const int ButtonX = 470;
    private const int NewGameButtonY = 392;
    private const int OpenGameButtonY = 492;
    private const int SettingsButtonY = 592;
    private const float GDevelopFontSize = 16f;
    private const int CloseButtonSize = 64;
    private const int CloseButtonX = 21;
    private const int CloseButtonY = 638;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private SpriteFont _uiFont = null!;
    private Texture2D _titleTexture = null!;
    private ContentManager _content = null!;
    private string _contentRootDirectory = "Content";
    private CharacterRegistry _characterRegistry = new();
    private CharacterDefinition? _selectedCharacter;
    private SpriteAnimation? _selectedIdleAnimation;
    private readonly Dictionary<string, SpriteAnimation?> _idleAnimations = new();
    private double _characterAnimTime;
    private KeyboardState _previousKeyboard;
    private int _selectedIndex;
    private MainButton _leftArrowButton = null!;
    private MainButton _rightArrowButton = null!;
    private MainButton _newGameButton = null!;
    private MainButton _openGameButton = null!;
    private MainButton _settingsButton = null!;
    private MainButton _closeButton = null!;

    public bool CloseRequested { get; private set; }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content;
        _contentRootDirectory = content.RootDirectory;

        _uiFont = content.Load<SpriteFont>("Font/Main");
        _titleTexture = content.Load<Texture2D>("Textures/UI/titletext");

        LoadButtons(content);
        LoadCharacters();
    }

    public void Update(GameTime gameTime)
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

        var mouse = Mouse.GetState();
        if (_leftArrowButton.Update(mouse))
        {
            SelectPreviousCharacter();
        }

        if (_rightArrowButton.Update(mouse))
        {
            SelectNextCharacter();
        }

        _newGameButton.Update(mouse);
        _openGameButton.Update(mouse);
        _settingsButton.Update(mouse);
        if (_closeButton.Update(mouse))
        {
            CloseRequested = true;
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        DrawCharacterSelect(spriteBatch);
        spriteBatch.Draw(_titleTexture, new Rectangle(TitleX, TitleY, TitleWidth, TitleHeight), Color.White);

        _newGameButton.Draw(spriteBatch);
        _openGameButton.Draw(spriteBatch);
        _settingsButton.Draw(spriteBatch);
        _closeButton.Draw(spriteBatch);
    }

    private void LoadButtons(ContentManager content)
    {
        _leftArrowButton = MainButton.FromTexture(
            content,
            new Rectangle(LeftArrowX, LeftArrowY, ArrowWidth, ArrowHeight),
            "Textures/UI/Buttons/left");
        _rightArrowButton = MainButton.FromTexture(
            content,
            new Rectangle(RightArrowX, RightArrowY, ArrowWidth, ArrowHeight),
            "Textures/UI/Buttons/right");

        _newGameButton = CreateMenuButton("NEW GAME", NewGameButtonY);
        _openGameButton = CreateMenuButton("OPEN GAME", OpenGameButtonY);
        _settingsButton = CreateMenuButton("SETTINGS", SettingsButtonY);

        _closeButton = MainButton.FromMainTexture(
            content,
            new Rectangle(CloseButtonX, CloseButtonY, CloseButtonSize, CloseButtonSize),
            MainButtonTextureShape.OneByOne,
            "X",
            _uiFont,
            fontSize: GDevelopFontSize);
    }

    private MainButton CreateMenuButton(string text, int y)
    {
        return MainButton.FromMainTexture(
            _content,
            new Rectangle(ButtonX, y, ButtonWidth, ButtonHeight),
            MainButtonTextureShape.ThreeByOne,
            text,
            _uiFont,
            fontSize: GDevelopFontSize);
    }

    private void DrawCharacterSelect(SpriteBatch spriteBatch)
    {
        if (_selectedIdleAnimation == null || _selectedCharacter == null)
        {
            return;
        }

        var anim = _selectedIdleAnimation;
        var frame = anim.GetFrame("forward", _characterAnimTime);
        var hasNeighbors = _characterRegistry.Characters.Count > 1;

        if (hasNeighbors)
        {
            DrawNeighborPreview(spriteBatch, new Rectangle(LeftPreviewX, LeftPreviewY, PreviewCharacterAreaSize, PreviewCharacterAreaSize), -1);
            DrawNeighborPreview(spriteBatch, new Rectangle(RightPreviewX, RightPreviewY, PreviewCharacterAreaSize, PreviewCharacterAreaSize), 1);
        }

        spriteBatch.Draw(anim.Texture, new Rectangle(MainCharacterX, MainCharacterY, MainCharacterAreaSize, MainCharacterAreaSize), frame, Color.White);
        _leftArrowButton.Draw(spriteBatch);
        _rightArrowButton.Draw(spriteBatch);
    }

    private void DrawNeighborPreview(SpriteBatch spriteBatch, Rectangle area, int direction)
    {
        var index = GetWrappedIndex(_selectedIndex + direction);
        var character = _characterRegistry.Characters[index];
        var anim = GetIdleAnimation(character);
        if (anim == null)
        {
            return;
        }

        var previewFrame = anim.GetFrame("forward", _characterAnimTime);
        var previewTint = new Color(140, 140, 140, 255);

        spriteBatch.Draw(anim.Texture, area, previewFrame, previewTint);
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
        _idleAnimations.Clear();
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

        _selectedIndex = index;
        _selectedCharacter = _characterRegistry.Characters[index];
        _characterAnimTime = 0;
        _selectedIdleAnimation = GetIdleAnimation(_selectedCharacter);
    }

    private int GetWrappedIndex(int index)
    {
        var count = _characterRegistry.Characters.Count;
        if (count == 0)
        {
            return 0;
        }

        var wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }

    private SpriteAnimation? GetIdleAnimation(CharacterDefinition character)
    {
        if (_idleAnimations.TryGetValue(character.Id, out var cached))
        {
            return cached;
        }

        var animation = LoadIdleAnimation(character);
        _idleAnimations[character.Id] = animation;
        return animation;
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

        var texturePath = NormalizeTextureRelativePath(definition.Texture);
        var texture = LoadTextureFromContent(texturePath);
        if (texture == null)
        {
            return null;
        }

        return new SpriteAnimation(definition, texture);
    }

    private Texture2D? LoadTextureFromContent(string relativePath)
    {
        try
        {
            return _content.Load<Texture2D>(GetContentName(relativePath));
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }

    private string GetContentPath(string relativePath)
    {
        var safePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(AppContext.BaseDirectory, _contentRootDirectory, safePath);
    }

    private static string GetContentName(string relativePath)
    {
        var safePath = relativePath.Replace('\\', '/');
        var withoutExtension = Path.ChangeExtension(safePath, null);
        return withoutExtension ?? safePath;
    }

    private string NormalizeTextureRelativePath(string relativePath)
    {
        if (relativePath.StartsWith("Textures/", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        return Path.Combine("Textures", relativePath);
    }

    private bool WasPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }
}
