using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record Album
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("parent")] public string Parent { get; set; }
    [JsonPropertyName("isDir")] public bool IsDir { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("album")] public string AlbumName { get; set; }
    [JsonPropertyName("artist")] public string Artist { get; set; }
    [JsonPropertyName("year")] public int Year { get; set; }
    [JsonPropertyName("genre")] public string Genre { get; set; }
    [JsonPropertyName("coverArt")] public string CoverArt { get; set; }
    [JsonPropertyName("duration")] public int Duration { get; set; }
    [JsonPropertyName("created")] public string Created { get; set; }
    [JsonPropertyName("artistId")] public string ArtistId { get; set; }
    [JsonPropertyName("songCount")] public int SongCount { get; set; }
    [JsonPropertyName("isVideo")] public bool IsVideo { get; set; }
    [JsonPropertyName("bpm")] public int Bpm { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; }
    [JsonPropertyName("sortName")] public string SortName { get; set; }
    [JsonPropertyName("mediaType")] public string MediaType { get; set; }
    [JsonPropertyName("musicBrainzId")] public string MusicBrainzId { get; set; }
    [JsonPropertyName("genres")] public IEnumerable<Genre> Genres { get; set; }
    [JsonPropertyName("song")] public IEnumerable<Song> Song { get; set; }
    [JsonPropertyName("replayGain")] public ReplayGain ReplayGain { get; set; }
    [JsonPropertyName("channelCount")] public int ChannelCount { get; set; }
    [JsonPropertyName("samplingRate")] public int SamplingRate { get; set; }
    [JsonPropertyName("playCount")] public int PlayCount { get; set; }
    [JsonPropertyName("played")] public string Played { get; set; }
}