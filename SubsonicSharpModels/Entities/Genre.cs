using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record Genre
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("value")] public string Value { get; set; }
    [JsonPropertyName("songCount")] public int SongCount { get; set; }
    [JsonPropertyName("albumCount")] public int AlbumCount { get; set; }
}