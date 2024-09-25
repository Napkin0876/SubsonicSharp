using System.ComponentModel.DataAnnotations;

namespace SubsonicSharp;

/// <summary>
/// Represents a request for a Subsonic search operation.
/// </summary>
/// <param name="Query">The search query string. This parameter is required.</param>
/// <param name="MusicFolderId">The optional ID of the music folder to search within.</param>
/// <param name="ArtistCount">The number of artist results to return. Default is 20.</param>
/// <param name="ArtistOffset">The offset for artist results. Default is 0.</param>
/// <param name="AlbumCount">The number of album results to return. Default is 20.</param>
/// <param name="AlbumOffset">The offset for album results. Default is 0.</param>
/// <param name="SongCount">The number of song results to return. Default is 20.</param>
/// <param name="SongOffset">The offset for song results. Default is 0.</param>
public record SubsonicSearchRequest(
    [Required]
    string Query,
    int? MusicFolderId = null,
    int? ArtistCount = 20,
    int? ArtistOffset = 0,
    int? AlbumCount = 20,
    int? AlbumOffset = 0,
    int? SongCount = 20,
    int? SongOffset = 0
);
