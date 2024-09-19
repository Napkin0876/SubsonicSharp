namespace SubsonicSharp.Entities;

public record Index
{
    public string name { get; set; }
    public IEnumerable<Artist> artist { get; set; }
}

