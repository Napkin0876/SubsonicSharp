using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record Playlist
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; }
    [JsonPropertyName("songCount")] public int SongCount { get; set; }
    [JsonPropertyName("duration")] public int Duration { get; set; }
    [JsonPropertyName("public")] public bool Public { get; set; }
    [JsonPropertyName("owner")] public string Owner { get; set; }
    [JsonPropertyName("created")] public string Created { get; set; }
    [JsonPropertyName("changed")] public string Changed { get; set; }
    [JsonPropertyName("coverArt")] public string CoverArt { get; set; }
    [JsonPropertyName("entry")] public IEnumerable<Song> Entry { get; set; }
}