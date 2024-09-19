namespace SubsonicSharp.Entities;

public record Album
{
    public string id { get; set; }
    public string parent { get; set; }
    public bool isDir { get; set; }
    public string title { get; set; }
    public string name { get; set; }
    public string album { get; set; }
    public string artist { get; set; }
    public int year { get; set; }
    public string genre { get; set; }
    public string coverArt { get; set; }
    public int duration { get; set; }
    public string created { get; set; }
    public string artistId { get; set; }
    public int songCount { get; set; }
    public bool isVideo { get; set; }
    public int bpm { get; set; }
    public string comment { get; set; }
    public string sortName { get; set; }
    public string mediaType { get; set; }
    public string musicBrainzId { get; set; }
    public IEnumerable<Genre> genres { get; set; }
    public ReplayGain replayGain { get; set; }
    public int channelCount { get; set; }
    public int samplingRate { get; set; }
    public int playCount { get; set; }
    public string played { get; set; }
}