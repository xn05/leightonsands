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

    [JsonPropertyName("properties")]
    public string Properties { get; set; } = string.Empty;
}

public sealed class MapRegionProperties
{
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

    [JsonPropertyName("player_height_tiles")]
    public float PlayerHeightTiles { get; set; } = 1.8f;

    [JsonPropertyName("speed")]
    public float Speed { get; set; } = 5.5f;

    [JsonPropertyName("camera_zoom")]
    public float CameraZoom { get; set; } = 3f;

    [JsonPropertyName("brightness")]
    public float Brightness { get; set; } = 1f;

    [JsonPropertyName("fog_color")]
    public string FogColor { get; set; } = "#000000";

    [JsonPropertyName("fog_opacity")]
    public float FogOpacity { get; set; }
}
