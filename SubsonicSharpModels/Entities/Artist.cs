using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record Artist
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("coverArt")] public string CoverArt { get; set; }
    [JsonPropertyName("albumCount")] public int AlbumCount { get; set; }
    [JsonPropertyName("artistImageUrl")] public string ArtistImageUrl { get; set; }
    [JsonPropertyName("musicBrainzId")] public string MusicBrainzId { get; set; }
    [JsonPropertyName("album")] public IEnumerable<Album> Albums { get; set; }
}

public record ArtistInfo
{
    [JsonPropertyName("biography")] public string Biography { get; set; }
    [JsonPropertyName("musicBrainzId")] public string MusicBrainzId { get; set; }
    [JsonPropertyName("lastFmUrl")] public string LastFmUrl { get; set; }
    [JsonPropertyName("smallImageUrl")] public string SmallImageUrl { get; set; }
    [JsonPropertyName("mediumImageUrl")] public string MediumImageUrl { get; set; }
    [JsonPropertyName("largeImageUrl")] public string LargeImageUrl { get; set; }
    [JsonPropertyName("similarArtist")] public IEnumerable<SimilarArtist> SimilarArtist { get; set; }
}

public record SimilarArtist
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("albumCount")] public int AlbumCount { get; set; }
    [JsonPropertyName("coverArt")] public string CoverArt { get; set; }
    [JsonPropertyName("artistImageUrl")] public string ArtistImageUrl { get; set; }
}

