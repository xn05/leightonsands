using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeightonSands;

public sealed class CharacterRegistry
{
    [JsonPropertyName("characters")]
    public List<CharacterDefinition> Characters { get; set; } = new();
}

public sealed class CharacterDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("selectable")]
    public bool Selectable { get; set; }

    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1f;

    [JsonPropertyName("animations")]
    public Dictionary<string, string> Animations { get; set; } = new();
}


