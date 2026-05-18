using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LeightonSands;

public sealed class AnimationDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("texture")]
    public string Texture { get; set; } = string.Empty;

    [JsonPropertyName("grid")]
    public AnimationGrid Grid { get; set; } = new();

    [JsonPropertyName("fps")]
    public int Fps { get; set; } = 1;

    [JsonPropertyName("index_base")]
    public int IndexBase { get; set; } = 1;

    [JsonPropertyName("sequences")]
    public Dictionary<string, int[]> Sequences { get; set; } = new();
}

public sealed class AnimationGrid
{
    [JsonPropertyName("columns")]
    public int Columns { get; set; } = 1;

    [JsonPropertyName("rows")]
    public int Rows { get; set; } = 1;
}

public sealed class SpriteAnimation
{
    private readonly AnimationDefinition _definition;
    private readonly Texture2D _texture;
    private readonly int _frameWidth;
    private readonly int _frameHeight;

    public SpriteAnimation(AnimationDefinition definition, Texture2D texture)
    {
        _definition = definition;
        _texture = texture;
        _frameWidth = Math.Max(1, texture.Width / Math.Max(1, definition.Grid.Columns));
        _frameHeight = Math.Max(1, texture.Height / Math.Max(1, definition.Grid.Rows));
    }

    public Texture2D Texture => _texture;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;

    public Rectangle GetFrame(string sequence, double timeSeconds)
    {
        var frames = ResolveSequence(sequence);
        if (frames.Length == 0)
        {
            return new Rectangle(0, 0, _frameWidth, _frameHeight);
        }

        var fps = Math.Max(1, _definition.Fps);
        var frameIndex = (int)(timeSeconds * fps) % frames.Length;
        var tileIndex = Math.Max(0, frames[frameIndex] - _definition.IndexBase);
        var column = tileIndex % Math.Max(1, _definition.Grid.Columns);
        var row = tileIndex / Math.Max(1, _definition.Grid.Columns);
        return new Rectangle(column * _frameWidth, row * _frameHeight, _frameWidth, _frameHeight);
    }

    private int[] ResolveSequence(string sequence)
    {
        if (_definition.Sequences.TryGetValue(sequence, out var frames))
        {
            return frames;
        }

        foreach (var entry in _definition.Sequences)
        {
            return entry.Value;
        }

        return Array.Empty<int>();
    }
}

