using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record Song
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("parent")]
    public string Parent { get; set; }
    [JsonPropertyName("isDir")]
    public bool IsDir { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("album")]
    public string Album { get; set; }
    [JsonPropertyName("artist")]
    public string Artist { get; set; }
    [JsonPropertyName("track")]
    public int Track { get; set; }
    [JsonPropertyName("year")]
    public int Year { get; set; }
    [JsonPropertyName("genre")]
    public string Genre { get; set; }
    [JsonPropertyName("coverArt")]
    public string CoverArt { get; set; }
    [JsonPropertyName("size")]
    public int Size { get; set; }
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
    [JsonPropertyName("suffix")]
    public string Suffix { get; set; }
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    [JsonPropertyName("bitRate")]
    public int BitRate { get; set; }
    [JsonPropertyName("path")]
    public string Path { get; set; }
    [JsonPropertyName("discNumber")]
    public int DiscNumber { get; set; }
    [JsonPropertyName("created")]
    public string Created { get; set; }
    [JsonPropertyName("albumId")]
    public string AlbumId { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("isVideo")]
    public bool IsVideo { get; set; }
    [JsonPropertyName("bpm")]
    public int Bpm { get; set; }
    [JsonPropertyName("comment")]
    public string Comment { get; set; }
    [JsonPropertyName("sortName")]
    public string SortName { get; set; }
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; }
    [JsonPropertyName("musicBrainzId")]
    public string MusicBrainzId { get; set; }
    [JsonPropertyName("]")]
    public IEnumerable<Genre> Genres { get; set; }
    [JsonPropertyName("replayGain")]
    public ReplayGain ReplayGain { get; set; }
    [JsonPropertyName("channelCount")]
    public int ChannelCount { get; set; }
    [JsonPropertyName("samplingRate")]
    public int SamplingRate { get; set; }
    [JsonPropertyName("artistId")]
    public string ArtistId { get; set; }
    [JsonPropertyName("starred")]
    public DateTime Starred { get; set; }
    [JsonPropertyName("userRating")]
    public int UserRating { get; set; }
}