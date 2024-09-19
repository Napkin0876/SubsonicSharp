using System.Text.Json.Serialization;

namespace SubsonicSharp.Entities;

public record ReplayGain
{
    [JsonPropertyName("trackGain")]
    public double TrackGain { get; set; }
    [JsonPropertyName("albumGain")]
    public double AlbumGain { get; set; }
    [JsonPropertyName("trackPeak")]
    public int TrackPeak { get; set; }
    [JsonPropertyName("albumPeak")]
    public int AlbumPeak { get; set; }
}