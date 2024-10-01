using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record Index
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("artist")] public IEnumerable<Artist> Artists { get; set; }
}

