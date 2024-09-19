using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record MusicDirectory
{
    public IEnumerable<MusicDirectoryChild> Child { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("parent")] public string Parent { get; set; }
    [JsonPropertyName("coverArt")] public string CoverArt { get; set; }
    [JsonPropertyName("songCount")] public int SongCount { get; set; }
    [JsonPropertyName("albumCount")] public int AlbumCount { get; set; }
    [JsonPropertyName("playCount")] public int PlayCount { get; set; }
    [JsonPropertyName("played")] public DateTime Played { get; set; }
}

