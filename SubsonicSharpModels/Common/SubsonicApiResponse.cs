using System.Text.Json.Serialization;
using SubsonicSharp.Entities;
using Index = SubsonicSharp.Entities.Index;

namespace SubsonicSharp;

public class SubsonicApiResponse<T> 
{
    [JsonPropertyName("subsonic-response")]
    public T SubsonicResponse { get; init; }
}

public class BaseResponse
{
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("serverVersion")] public string ServerVersion { get; set; }
    [JsonPropertyName("openSubsonic")] public bool OpenSubsonic { get; set; }
    [JsonPropertyName("error")] public SubsonicError? Error { get; set; }

    public bool IsSuccess()
    {
        return Status == "ok";
    }
}

public record SubsonicError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")]public string Message { get; set; }
}

public class GetMusicFoldersResponse : BaseResponse
{
    [JsonPropertyName("musicFolders")] public MusicFolderList MusicFolders { get; set; }
}

public record MusicFolderList
{
    [JsonPropertyName("musicFolder")] public IEnumerable<MusicFolder> MusicFolder { get; set; }
}

public class GetIndexesResponse : BaseResponse
{
    [JsonPropertyName("indexes")] public IndexList Indexes { get; set; }
}

public record IndexList
{
    [JsonPropertyName("index")] public IEnumerable<Index> Index { get; set; }
}

public class GetMusicDirectoryResponse : BaseResponse
{
    [JsonPropertyName("directory")] public MusicDirectory Directory { get; set; }
}

public class GetGenresResponse : BaseResponse
{
    [JsonPropertyName("genres")] public GenreList Genres { get; set; }
}

public record GenreList
{
    [JsonPropertyName("genre")] public IEnumerable<Genre> Genre { get; set; }
}

public class GetArtistsResponse : BaseResponse
{
    [JsonPropertyName("artists")] public ArtistIndexList Artists { get; set; }
}

public class ArtistIndexList
{
    [JsonPropertyName("index")] public IEnumerable<Index> Index { get; set; }
}

public class GetArtistResponse : BaseResponse
{
    [JsonPropertyName("artist")] public Artist Artist { get; set; }
}

public class GetArtistInfoResponse : BaseResponse
{
    [JsonPropertyName("artistInfo")] public ArtistInfo ArtistInfo { get; set; }
}

public class GetArtistInfo2Response : BaseResponse
{
    [JsonPropertyName("artistInfo2")] public ArtistInfo ArtistInfo { get; set; }
}

public class GetAlbumResponse : BaseResponse
{
    [JsonPropertyName("album")] public Album Album { get; set; }
}

public class GetSongResponse : BaseResponse
{
    [JsonPropertyName("song")] public Song Song { get; set; }
}

public class Search2Response : BaseResponse
{
    [JsonPropertyName("searchResult2")] public SearchResult SearchResult { get; set; }
}

public class Search3Response : BaseResponse
{
    [JsonPropertyName("searchResult3")] public SearchResult SearchResult { get; set; }
}

public class GetStarredResponse : BaseResponse
{
    [JsonPropertyName("starred")] public SearchResult SearchResult { get; set; }
}

public class GetStarred2Response : BaseResponse
{
    [JsonPropertyName("starred2")] public SearchResult SearchResult { get; set; }
}

public class GetRandomSongsResponse : BaseResponse
{
    [JsonPropertyName("randomSongs")] public SongList Songs { get; set; }
}

public record SongList
{
    [JsonPropertyName("song")] public IEnumerable<Song> Song { get; set; }
}

public record SearchResult
{
    [JsonPropertyName("artist")] public IEnumerable<Artist> Artist { get; set; }
    [JsonPropertyName("album")] public IEnumerable<Album> Album { get; set; }
    [JsonPropertyName("song")] public IEnumerable<Song> Song { get; set; }
}

public class GetPlaylistsResponse : BaseResponse
{
    [JsonPropertyName("playlists")] public PlaylistList Playlists { get; set; }
}

public record PlaylistList
{
    [JsonPropertyName("playlist")] public IEnumerable<Playlist> Playlist { get; set; }
}

public class GetPlaylistResponse : BaseResponse
{
    [JsonPropertyName("playlist")] public Playlist Playlist { get; set; }
}