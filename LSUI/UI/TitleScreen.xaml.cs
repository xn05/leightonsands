using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeightonSands;
using LeightonSands.Scenes;
using LeightonSands.UI;
using LSUI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LSUI.UI;

public sealed class TitleScreen : IGameScene
{
    private const string ResourceName = "TitleScreen.xaml";
    private const int MainCharacterAreaSize = 240;
    private const int PreviewCharacterAreaSize = 200;
    private const int MainCharacterX = 520;
    private const int MainCharacterY = 14;
    private const int LeftPreviewX = 289;
    private const int LeftPreviewY = 34;
    private const int RightPreviewX = 789;
    private const int RightPreviewY = 34;

    private SpriteFont _uiFont = null!;
    private UIScreen _ui = null!;
    private ContentManager _content = null!;
    private CharacterRegistry _characterRegistry = new();
    private CharacterDefinition? _selectedCharacter;
    private SpriteAnimation? _selectedIdleAnimation;
    private readonly Dictionary<string, SpriteAnimation?> _idleAnimations = new();
    private double _characterAnimTime;
    private KeyboardState _previousKeyboard;
    private int _selectedIndex;
    private UIButton _leftArrowButton = null!;
    private UIButton _rightArrowButton = null!;
    private UIButton _newGameButton = null!;
    private UIButton _openGameButton = null!;
    private UIButton _settingsButton = null!;
    private UIButton _closeButton = null!;

    public bool CloseRequested { get; private set; }
    public bool NewGameRequested { get; private set; }
    public string SelectedCharacterId => _selectedCharacter?.Id ?? string.Empty;

    public void ClearNewGameRequest()
    {
        NewGameRequested = false;
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content;

        _uiFont = content.Load<SpriteFont>("Font/Main");
        UIElementFactory.Register();
        _ui = XamlUIScreenLoader.LoadFromResource<TitleScreen>(content, ResourceName, _uiFont);
        LoadControls();
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

        _ui.Update(Mouse.GetState());
        if (_leftArrowButton.WasClicked)
        {
            SelectPreviousCharacter();
        }

        if (_rightArrowButton.WasClicked)
        {
            SelectNextCharacter();
        }

        if (_newGameButton.WasClicked)
        {
            NewGameRequested = true;
        }

        if (_closeButton.WasClicked)
        {
            CloseRequested = true;
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawCharacterSelect(spriteBatch);
        _ui.Draw(spriteBatch);
        spriteBatch.End();
    }

    private void LoadControls()
    {
        _leftArrowButton = _ui.Get<UIButton>("LeftArrowButton");
        _rightArrowButton = _ui.Get<UIButton>("RightArrowButton");
        _newGameButton = _ui.Get<UIButton>("NewGameButton");
        _openGameButton = _ui.Get<UIButton>("OpenGameButton");
        _settingsButton = _ui.Get<UIButton>("SettingsButton");
        _closeButton = _ui.Get<UIButton>("CloseButton");
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
        _characterRegistry = _content.Load<CharacterRegistry>("Characters/characters");
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

        var definition = _content.Load<AnimationDefinition>(GetCharacterContentName(animationPath));
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

    private static string GetContentName(string relativePath)
    {
        var safePath = relativePath.Replace('\\', '/');
        var withoutExtension = Path.ChangeExtension(safePath, null);
        return withoutExtension ?? safePath;
    }

    private static string GetCharacterContentName(string relativePath)
    {
        return GetContentName(relativePath.StartsWith("Characters/", StringComparison.OrdinalIgnoreCase)
            ? relativePath
            : $"Characters/{relativePath}");
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
