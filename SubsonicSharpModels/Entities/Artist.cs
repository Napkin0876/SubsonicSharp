namespace SubsonicSharp.Entities;

public record Artist
{
    public string id { get; set; }
    public string name { get; set; }
    public string coverArt { get; set; }
    public int albumCount { get; set; }
    public string artistImageUrl { get; set; }
    public string musicBrainzId { get; set; }
    public IEnumerable<Album> album { get; set; }
}