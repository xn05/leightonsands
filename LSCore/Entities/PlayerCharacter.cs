using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LeightonSands.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LeightonSands.Entities;

public sealed class PlayerCharacter
{
    private const float DefaultSpeedTilesPerSecond = 5.5f;
    private const float DefaultCornerSlideSpeedTilesPerSecond = 2.5f;
    private const float DefaultHeightTiles = 1.8f;
    private const float HitboxSizeTiles = 0.5f;
    private const float MaxCornerSlideTiles = 1f;
    private const string DefaultFacing = "forward";

    private ContentManager _content = null!;
    private CharacterDefinition? _definition;
    private SpriteAnimation? _idleAnimation;
    private SpriteAnimation? _walkAnimation;
    private readonly Dictionary<Rectangle, Rectangle> _visibleFrameCache = new();
    private string _loadedPlayerId = string.Empty;
    private string _playerId = string.Empty;
    private string _facing = DefaultFacing;
    private bool _isWalking;
    private double _animationTime;

    public Vector2 FeetPosition { get; private set; }
    public float HeightTiles { get; set; } = DefaultHeightTiles;
    public float SpeedTilesPerSecond { get; set; } = DefaultSpeedTilesPerSecond;
    public float CornerSlideSpeedTilesPerSecond { get; set; } = DefaultCornerSlideSpeedTilesPerSecond;

    public void LoadContent(ContentManager content, string playerId)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        CornerSlideSpeedTilesPerSecond = LoadCornerSlideSpeed();
        SetCharacter(playerId);
    }

    public void SetCharacter(string playerId)
    {
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            _playerId = playerId;
        }

        if (_content == null || _loadedPlayerId.Equals(_playerId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _definition = null;
        _idleAnimation = null;
        _walkAnimation = null;
        LoadAnimations();
    }

    public void SpawnAtTile(TiledMap map, int tileX, int tileY)
    {
        FeetPosition = new Vector2(
            (tileX * map.TileWidth) + map.TileWidth * 0.5f,
            (tileY * map.TileHeight) + map.TileHeight);
        _animationTime = 0;
        _facing = DefaultFacing;
    }

    public void Update(GameTime gameTime, TiledMap map, TiledCollisionMap collisionMap)
    {
        _animationTime += gameTime.ElapsedGameTime.TotalSeconds;
        var input = ReadMovementInput();
        _isWalking = input != Vector2.Zero;
        if (!_isWalking)
        {
            return;
        }

        input.Normalize();
        var speedPixelsPerSecond = MathHelper.Max(0f, SpeedTilesPerSecond) * map.TileWidth;
        var cornerSlidePixels = MathHelper.Max(0f, CornerSlideSpeedTilesPerSecond) * map.TileWidth * (float)gameTime.ElapsedGameTime.TotalSeconds;
        Move(input * speedPixelsPerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds, cornerSlidePixels, map, collisionMap);
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, TiledMap map)
    {
        var animation = _isWalking ? _walkAnimation : _idleAnimation;
        if (animation == null)
        {
            return;
        }

        var frame = animation.GetFrame(_facing, _animationTime);
        var visibleFrame = GetVisibleFrame(animation.Texture, frame);
        var desiredHeight = map.TileHeight * MathHelper.Max(0.1f, HeightTiles);
        var scale = desiredHeight / visibleFrame.Height;
        var width = visibleFrame.Width * scale;
        var height = visibleFrame.Height * scale;
        var screenPosition = (FeetPosition - camera.Position) * camera.Zoom;
        var destination = new Rectangle(
            (int)MathF.Round(screenPosition.X - width * camera.Zoom * 0.5f),
            (int)MathF.Round(screenPosition.Y - height * camera.Zoom),
            (int)MathF.Round(width * camera.Zoom),
            (int)MathF.Round(height * camera.Zoom));

        spriteBatch.Draw(animation.Texture, destination, visibleFrame, Color.White);
    }

    public Rectangle GetHitbox(TiledMap map)
    {
        var size = MathF.Max(1f, map.TileWidth * HitboxSizeTiles);
        return new Rectangle(
            (int)MathF.Round(FeetPosition.X - size * 0.5f),
            (int)MathF.Round(FeetPosition.Y - size),
            (int)MathF.Round(size),
            (int)MathF.Round(size));
    }

    private Vector2 ReadMovementInput()
    {
        var keyboard = Keyboard.GetState();
        var input = Vector2.Zero;

        var movingLeft = keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left);
        var movingRight = keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right);
        var movingUp = keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up);
        var movingDown = keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down);

        if (movingLeft)
        {
            input.X -= 1f;
        }

        if (movingRight)
        {
            input.X += 1f;
        }

        if (movingUp)
        {
            input.Y -= 1f;
        }

        if (movingDown)
        {
            input.Y += 1f;
        }

        if (movingLeft && !movingRight)
        {
            _facing = "left";
        }
        else if (movingRight && !movingLeft)
        {
            _facing = "right";
        }
        else if (movingUp && !movingDown)
        {
            _facing = "backward";
        }
        else if (movingDown && !movingUp)
        {
            _facing = "forward";
        }

        return input;
    }

    private void Move(Vector2 movement, float cornerSlidePixels, TiledMap map, TiledCollisionMap collisionMap)
    {
        var nextX = FeetPosition + new Vector2(movement.X, 0f);
        if (!collisionMap.IntersectsBlockedArea(GetHitbox(nextX, map)))
        {
            FeetPosition = nextX;
        }
        else if (Math.Abs(movement.X) > 0f && Math.Abs(movement.Y) < 0.001f)
        {
            TrySlideAroundCorner(new Vector2(movement.X, 0f), cornerSlidePixels, verticalSlide: true, map, collisionMap);
        }

        var nextY = FeetPosition + new Vector2(0f, movement.Y);
        if (!collisionMap.IntersectsBlockedArea(GetHitbox(nextY, map)))
        {
            FeetPosition = nextY;
        }
        else if (Math.Abs(movement.Y) > 0f && Math.Abs(movement.X) < 0.001f)
        {
            TrySlideAroundCorner(new Vector2(0f, movement.Y), cornerSlidePixels, verticalSlide: false, map, collisionMap);
        }
    }

    private void TrySlideAroundCorner(Vector2 blockedMovement, float cornerSlidePixels, bool verticalSlide, TiledMap map, TiledCollisionMap collisionMap)
    {
        var maxSlide = Math.Max(1f, map.TileWidth * MaxCornerSlideTiles);
        var searchStep = 1f;
        var slideStep = Math.Max(0.25f, cornerSlidePixels);
        var slide = FindCornerSlide(blockedMovement, verticalSlide, maxSlide, searchStep, map, collisionMap);
        if (slide == 0f)
        {
            return;
        }

        var signedSlideStep = MathF.CopySign(Math.Min(Math.Abs(slide), slideStep), slide);
        var slideMovement = verticalSlide ? new Vector2(0f, signedSlideStep) : new Vector2(signedSlideStep, 0f);
        var nextPosition = FeetPosition + slideMovement;
        if (!collisionMap.IntersectsBlockedArea(GetHitbox(nextPosition, map)))
        {
            FeetPosition = nextPosition;
        }
    }

    private float FindCornerSlide(
        Vector2 blockedMovement,
        bool verticalSlide,
        float maxSlide,
        float step,
        TiledMap map,
        TiledCollisionMap collisionMap)
    {
        for (var distance = step; distance <= maxSlide; distance += step)
        {
            var negativeOffset = verticalSlide ? new Vector2(0f, -distance) : new Vector2(-distance, 0f);
            if (CanMoveAfterSlide(blockedMovement, negativeOffset, map, collisionMap))
            {
                return -distance;
            }

            var positiveOffset = verticalSlide ? new Vector2(0f, distance) : new Vector2(distance, 0f);
            if (CanMoveAfterSlide(blockedMovement, positiveOffset, map, collisionMap))
            {
                return distance;
            }
        }

        return 0f;
    }

    private bool CanMoveAfterSlide(Vector2 blockedMovement, Vector2 slideOffset, TiledMap map, TiledCollisionMap collisionMap)
    {
        var slidePosition = FeetPosition + slideOffset;
        if (collisionMap.IntersectsBlockedArea(GetHitbox(slidePosition, map)))
        {
            return false;
        }

        return !collisionMap.IntersectsBlockedArea(GetHitbox(slidePosition + blockedMovement, map));
    }

    private static Rectangle GetHitbox(Vector2 feetPosition, TiledMap map)
    {
        var size = MathF.Max(1f, map.TileWidth * HitboxSizeTiles);
        return new Rectangle(
            (int)MathF.Round(feetPosition.X - size * 0.5f),
            (int)MathF.Round(feetPosition.Y - size),
            (int)MathF.Round(size),
            (int)MathF.Round(size));
    }

    private void LoadAnimations()
    {
        var registry = _content.Load<CharacterRegistry>("Characters/characters");
        _definition = registry.Characters.FirstOrDefault(character =>
            character.Id.Equals(_playerId, StringComparison.OrdinalIgnoreCase)) ?? registry.Characters.FirstOrDefault();
        if (_definition == null)
        {
            return;
        }

        _idleAnimation = LoadAnimation(_definition, "idle");
        _walkAnimation = LoadAnimation(_definition, "walk") ?? _idleAnimation;
        _loadedPlayerId = _definition.Id;
    }

    private SpriteAnimation? LoadAnimation(CharacterDefinition character, string animationName)
    {
        if (!character.Animations.TryGetValue(animationName, out var animationPath))
        {
            return null;
        }

        var definition = _content.Load<AnimationDefinition>(GetCharacterContentName(animationPath));
        if (definition == null || string.IsNullOrWhiteSpace(definition.Texture))
        {
            return null;
        }

        var texture = _content.Load<Texture2D>(GetContentName(NormalizeTextureRelativePath(definition.Texture)));
        return new SpriteAnimation(definition, texture);
    }

    private Rectangle GetVisibleFrame(Texture2D texture, Rectangle frame)
    {
        if (_visibleFrameCache.TryGetValue(frame, out var cached))
        {
            return cached;
        }

        var pixels = new Color[frame.Width * frame.Height];
        texture.GetData(0, frame, pixels, 0, pixels.Length);

        var minX = frame.Width;
        var minY = frame.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                if (pixels[y * frame.Width + x].A == 0)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            _visibleFrameCache[frame] = frame;
            return frame;
        }

        var visibleFrame = new Rectangle(
            frame.X + minX,
            frame.Y + minY,
            maxX - minX + 1,
            maxY - minY + 1);
        _visibleFrameCache[frame] = visibleFrame;
        return visibleFrame;
    }

    private float LoadCornerSlideSpeed()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "Config", "settings.xml");
        if (!File.Exists(settingsPath))
        {
            return DefaultCornerSlideSpeedTilesPerSecond;
        }

        var document = XDocument.Load(settingsPath);
        var value = document.Root?.Element("corner_slide_speed")?.Value;
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed)
            ? speed
            : DefaultCornerSlideSpeedTilesPerSecond;
    }

    private static string GetContentName(string relativePath)
    {
        var safePath = relativePath.Replace('\\', '/');
        return Path.ChangeExtension(safePath, null) ?? safePath;
    }

    private static string GetCharacterContentName(string relativePath)
    {
        return GetContentName(relativePath.StartsWith("Characters/", StringComparison.OrdinalIgnoreCase)
            ? relativePath
            : $"Characters/{relativePath}");
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
