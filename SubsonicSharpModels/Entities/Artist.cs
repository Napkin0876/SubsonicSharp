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

public record ArtistInfo
{
    public string biography { get; set; }
    public string musicBrainzId { get; set; }
    public string lastFmUrl { get; set; }
    public string smallImageUrl { get; set; }
    public string mediumImageUrl { get; set; }
    public string largeImageUrl { get; set; }
    public IEnumerable<SimilarArtist> similarArtist { get; set; }
}

public record SimilarArtist
{
    public string id { get; set; }
    public string name { get; set; }
    public int albumCount { get; set; }
    public string coverArt { get; set; }
    public string artistImageUrl { get; set; }
}

