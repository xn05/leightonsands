using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeightonSands.Maps;

public sealed class MapRegionRegistry
{
    [JsonPropertyName("regions")]
    public List<MapRegionDefinition> Regions { get; set; } = new();
}

public sealed class MapRegionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("map")]
    public string Map { get; set; } = string.Empty;

    [JsonPropertyName("spawn_tile_x")]
    public int SpawnTileX { get; set; }

    [JsonPropertyName("spawn_tile_y")]
    public int SpawnTileY { get; set; }

    [JsonPropertyName("player")]
    public string Player { get; set; } = "nightingale";

    [JsonPropertyName("player_scale")]
    public float PlayerScale { get; set; } = 1f;
}
