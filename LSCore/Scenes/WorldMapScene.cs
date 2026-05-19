using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LeightonSands.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LeightonSands.Scenes;

public sealed class WorldMapScene : TiledMapScene
{
    private const float PlayerSpeed = 90f;
    private const string DefaultFacing = "forward";

    private readonly MapRegionDefinition _region;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private ContentManager _content = null!;
    private string _contentRootDirectory = "Content";
    private CharacterDefinition? _playerDefinition;
    private SpriteAnimation? _idleAnimation;
    private SpriteAnimation? _walkAnimation;
    private TiledCollisionMap? _collisionMap;
    private GraphicsDevice? _graphicsDevice;
    private Vector2 _playerPosition;
    private string _facing = DefaultFacing;
    private bool _isWalking;
    private double _animationTime;
    private string _loadedPlayerId = string.Empty;

    public WorldMapScene(MapRegionDefinition region) : base(region.Map)
    {
        _region = region;
    }

    public void StartNewGame(string playerId)
    {
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            _region.Player = playerId;
        }

        if (Map != null)
        {
            ResetPlayerToSpawn();
        }

        ReloadPlayerAnimations();
    }

    public override void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        base.LoadContent(content, graphicsDevice);

        if (_idleAnimation != null)
        {
            return;
        }

        _content = content;
        _contentRootDirectory = content.RootDirectory;
        _graphicsDevice = graphicsDevice;
        if (Map == null)
        {
            return;
        }

        _collisionMap = new TiledCollisionMap(Map);
        Camera.Zoom = 3f;
        ResetPlayerToSpawn();
        ReloadPlayerAnimations();
        CenterCamera(graphicsDevice.Viewport);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Map == null)
        {
            return;
        }

        _animationTime += gameTime.ElapsedGameTime.TotalSeconds;
        var input = ReadMovementInput();
        _isWalking = input != Vector2.Zero;
        if (_isWalking)
        {
            input.Normalize();
            MovePlayer(input * PlayerSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        if (_graphicsDevice != null)
        {
            CenterCamera(_graphicsDevice.Viewport);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        DrawPlayer(spriteBatch);
    }

    private Vector2 ReadMovementInput()
    {
        var keyboard = Keyboard.GetState();
        var input = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
        {
            input.X -= 1f;
            _facing = "left";
        }

        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
        {
            input.X += 1f;
            _facing = "right";
        }

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
        {
            input.Y -= 1f;
            _facing = "backward";
        }

        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
        {
            input.Y += 1f;
            _facing = "forward";
        }

        return input;
    }

    private void MovePlayer(Vector2 movement)
    {
        var nextX = _playerPosition + new Vector2(movement.X, 0f);
        if (!IsBlocked(nextX))
        {
            _playerPosition = nextX;
        }

        var nextY = _playerPosition + new Vector2(0f, movement.Y);
        if (!IsBlocked(nextY))
        {
            _playerPosition = nextY;
        }
    }

    private bool IsBlocked(Vector2 position)
    {
        return _collisionMap?.IntersectsBlockedArea(GetCollisionBounds(position)) == true;
    }

    private Rectangle GetCollisionBounds(Vector2 position)
    {
        var width = Map?.TileWidth ?? 16;
        var height = Map?.TileHeight ?? 16;
        return new Rectangle(
            (int)MathF.Round(position.X - width * 0.5f),
            (int)MathF.Round(position.Y - height),
            width,
            height);
    }

    private void DrawPlayer(SpriteBatch spriteBatch)
    {
        var animation = _isWalking ? _walkAnimation : _idleAnimation;
        if (animation == null || _playerDefinition == null)
        {
            return;
        }

        var frame = animation.GetFrame(_facing, _animationTime);
        var scale = _region.PlayerScale > 0f ? _region.PlayerScale : 1f;
        var width = (int)MathF.Round(frame.Width * scale);
        var height = (int)MathF.Round(frame.Height * scale);
        var screenPosition = (_playerPosition - Camera.Position) * Camera.Zoom;
        var destination = new Rectangle(
            (int)MathF.Round(screenPosition.X - width * Camera.Zoom * 0.5f),
            (int)MathF.Round(screenPosition.Y - height * Camera.Zoom),
            (int)MathF.Round(width * Camera.Zoom),
            (int)MathF.Round(height * Camera.Zoom));

        spriteBatch.Draw(animation.Texture, destination, frame, Color.White);
    }

    private void CenterCamera(Viewport viewport)
    {
        if (Map == null)
        {
            return;
        }

        var center = _playerPosition - new Vector2(0f, Map.TileHeight * 0.5f);
        var visibleWidth = viewport.Width / MathHelper.Max(0.01f, Camera.Zoom);
        var visibleHeight = viewport.Height / MathHelper.Max(0.01f, Camera.Zoom);
        Camera.Position = new Vector2(
            MathHelper.Clamp(center.X - visibleWidth * 0.5f, 0f, Math.Max(0, Map.PixelWidth - visibleWidth)),
            MathHelper.Clamp(center.Y - visibleHeight * 0.5f, 0f, Math.Max(0, Map.PixelHeight - visibleHeight)));
    }

    private void ResetPlayerToSpawn()
    {
        if (Map == null)
        {
            return;
        }

        _playerPosition = new Vector2(
            (_region.SpawnTileX * Map.TileWidth) + Map.TileWidth * 0.5f,
            (_region.SpawnTileY * Map.TileHeight) + Map.TileHeight);
        _animationTime = 0;
        _facing = DefaultFacing;
    }

    private void ReloadPlayerAnimations()
    {
        if (_content == null || _loadedPlayerId.Equals(_region.Player, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _playerDefinition = null;
        _idleAnimation = null;
        _walkAnimation = null;
        LoadPlayerAnimations();
    }

    private void LoadPlayerAnimations()
    {
        var registryPath = GetContentPath("Characters/characters.json");
        if (!File.Exists(registryPath))
        {
            return;
        }

        var json = File.ReadAllText(registryPath);
        var registry = JsonSerializer.Deserialize<CharacterRegistry>(json, _jsonOptions) ?? new CharacterRegistry();
        _playerDefinition = registry.Characters.FirstOrDefault(character =>
            character.Id.Equals(_region.Player, StringComparison.OrdinalIgnoreCase)) ?? registry.Characters.FirstOrDefault();
        if (_playerDefinition == null)
        {
            return;
        }

        _idleAnimation = LoadAnimation(_playerDefinition, "idle");
        _walkAnimation = LoadAnimation(_playerDefinition, "walk") ?? _idleAnimation;
        _loadedPlayerId = _playerDefinition.Id;
    }

    private SpriteAnimation? LoadAnimation(CharacterDefinition character, string animationName)
    {
        if (!character.Animations.TryGetValue(animationName, out var animationPath))
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

        var texture = _content.Load<Texture2D>(GetContentName(NormalizeTextureRelativePath(definition.Texture)));
        return new SpriteAnimation(definition, texture);
    }

    private string GetContentPath(string relativePath)
    {
        var safePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(AppContext.BaseDirectory, _contentRootDirectory, safePath);
    }

    private static string GetContentName(string relativePath)
    {
        var safePath = relativePath.Replace('\\', '/');
        return Path.ChangeExtension(safePath, null) ?? safePath;
    }

    private static string NormalizeTextureRelativePath(string relativePath)
    {
        if (relativePath.StartsWith("Textures/", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        return Path.Combine("Textures", relativePath);
    }
}
