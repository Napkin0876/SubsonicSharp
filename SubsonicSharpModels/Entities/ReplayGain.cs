using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record ReplayGain
{
    [JsonPropertyName("trackGain")]
    public double TrackGain { get; set; }
    [JsonPropertyName("albumGain")]
    public double AlbumGain { get; set; }
    [JsonPropertyName("trackPeak")]
    public double TrackPeak { get; set; }
    [JsonPropertyName("albumPeak")]
    public double AlbumPeak { get; set; }
}