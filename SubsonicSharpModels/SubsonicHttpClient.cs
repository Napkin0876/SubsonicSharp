using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SubsonicSharp.Entities;
using SubsonicSharp.Extensions;
using Index = SubsonicSharp.Entities.Index;

namespace SubsonicSharp;

public class SubsonicHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ServerConfiguration _serverConfiguration;
    private readonly SubsonicAuth _subsonicAuth;
    private readonly ILogger _logger;

    private const string JsonMediaType = "application/json";
    private const string ApiVersionParameter = "v";
    private const string ClientNameParameter = "c";
    private const string FormatParameter = "f";
    private const string UserNameParameter = "u";
    private const string TokenParameter = "t";
    private const string SaltParameter = "s";
    private const string Format = "json";

    public SubsonicHttpClient(HttpClient httpClient, ServerConfiguration serverInformation, SubsonicAuth userToken,
        ILogger logger)
    {
        _serverConfiguration = serverInformation;
        _subsonicAuth = userToken;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _logger = logger;
    }

    private async Task<T> ExecuteAsync<T>(HttpMethod method, string relativePath, object? data = null,
        List<KeyValuePair<string, string>>? parameters = null) where T : BaseResponse
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(relativePath));

        relativePath = AppendQueryString(relativePath, parameters);

        _logger.LogInformation("Executing {Method} on {RelativePath}", method, relativePath);

        var requestMessage = CreateHttpRequestMessage(method, relativePath, data);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("response: {Substring}", responseContent[..Math.Min(150, responseContent.Length)]);
            
            var result = JsonSerializer.Deserialize<SubsonicApiResponse<T>>(responseContent) ??
                         throw new JsonException($"Failed to deserialize response content: {responseContent}");

            if (!result.SubsonicResponse.IsSuccess())
            {
                throw HandleSubsonicException(result.SubsonicResponse.Error ?? throw new InvalidOperationException());
            }

            return result.SubsonicResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    private static HttpRequestException HandleSubsonicException(SubsonicError error)
    {
        return error.Code switch
        {
            0 => new HttpRequestException("A generic error occurred: " + error.Message, null,
                HttpStatusCode.InternalServerError),
            10 => new HttpRequestException("Required parameter is missing: " + error.Message, null,
                HttpStatusCode.BadRequest),
            20 => new HttpRequestException(
                "Incompatible Subsonic REST protocol version. Client must upgrade: " + error.Message, null,
                HttpStatusCode.HttpVersionNotSupported),
            30 => new HttpRequestException(
                "Incompatible Subsonic REST protocol version. Server must upgrade: " + error.Message, null,
                HttpStatusCode.HttpVersionNotSupported),
            40 => new HttpRequestException("Wrong username or password: " + error.Message, null,
                HttpStatusCode.Unauthorized),
            41 => new HttpRequestException("Token authentication not supported for LDAP users: " + error.Message, null,
                HttpStatusCode.Forbidden),
            50 => new HttpRequestException("User is not authorized for the given operation: " + error.Message, null,
                HttpStatusCode.Forbidden),
            60 => new HttpRequestException(
                "The trial period for the Subsonic server is over. Please upgrade to Subsonic Premium: " +
                error.Message, null, HttpStatusCode.PaymentRequired),
            70 => new HttpRequestException("The requested data was not found: " + error.Message, null,
                HttpStatusCode.NotFound),
            _ => new HttpRequestException("An unknown error occurred: " + error.Message, null,
                HttpStatusCode.InternalServerError)
        };
    }

    private HttpRequestMessage CreateHttpRequestMessage(HttpMethod method, string relativePath, object? data)
    {
        var requestMessage = new HttpRequestMessage(method, $"{_serverConfiguration.BaseUrl()}{relativePath}");
        SetRequiredParameters(requestMessage);

        if (data != null)
        {
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, JsonMediaType);
        }

        return requestMessage;
    }

    private static string AppendQueryString(string relativePath, List<KeyValuePair<string, string>>? parameters)
    {
        if (parameters is {Count: > 0})
        {
            var queryString = GenerateQueryString(parameters);
            relativePath = $"{relativePath}?{queryString}";
        }

        return relativePath;
    }

    private static string GenerateQueryString(List<KeyValuePair<string, string>> parameters)
    {
        return string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private void SetRequiredParameters(HttpRequestMessage requestMessage)
    {
        var query = System.Web.HttpUtility.ParseQueryString(requestMessage.RequestUri.Query);
        query[ApiVersionParameter] = _serverConfiguration.ApiVersion;
        query[ClientNameParameter] = ServerConfiguration.AppName;
        query[FormatParameter] = Format;
        query[UserNameParameter] = _subsonicAuth.Username;
        query[TokenParameter] = _subsonicAuth.Token;
        query[SaltParameter] = _subsonicAuth.Salt;

        var uriBuilder = new UriBuilder(requestMessage.RequestUri!)
        {
            Query = query.ToString()
        };
        requestMessage.RequestUri = uriBuilder.Uri;
    }

    #region System

    public async Task<BaseResponse> PingAsync()
    {
        var result = await ExecuteAsync<BaseResponse>(HttpMethod.Get, "ping");

        return result;
    }

    #endregion

    #region Browsing

    /// <summary>
    ///  Returns all configured top-level music folders. Takes no extra parameters. 
    /// </summary>
    /// <returns>A task that contains a collection of <see cref="MusicFolder"/>.</returns>
    public async Task<IEnumerable<MusicFolder>> GetMusicFoldersAsync()
    {
        var folders = await ExecuteAsync<GetMusicFoldersResponse>(HttpMethod.Get, "getMusicFolders");
        return folders.MusicFolders.MusicFolder;
    }

    /// <summary>
    ///  Returns an indexed structure of all artists.
    /// </summary>
    /// <param name="musicFolderId">The ID of the music folder. If null or empty, all folders are considered.</param>
    /// <param name="modifiedSince">The date and time to filter indexes modified since that date. If null, no time-based filtering is applied.</param>
    /// <returns>A task that contains a collection of <see cref="Index"/>.</returns>
    public async Task<IEnumerable<Index>> GetIndexesAsync(string? musicFolderId = null, DateTime? modifiedSince = null)
    {
        var queryParameters = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(musicFolderId))
        {
            queryParameters.Add(new KeyValuePair<string, string>("musicFolderId",musicFolderId));
        }

        if (modifiedSince.HasValue)
        {
            var millisecondsSinceEpoch = new DateTimeOffset(modifiedSince.Value).ToUnixTimeMilliseconds();
            queryParameters.Add(new KeyValuePair<string, string>("ifModifiedSince",millisecondsSinceEpoch.ToString()));
        }

        var response = await ExecuteAsync<GetIndexesResponse>(HttpMethod.Get, "getIndexes", null, queryParameters);
        return response.Indexes.Index;
    }

    /// <summary>
    /// Returns a listing of all files in a music directory.
    /// Typically used to get list of albums for an artist, or list of songs for an album. 
    /// </summary>
    /// <param name="id">The ID of the music directory. Required.</param>
    /// <returns>A task containing a <see cref="MusicDirectory"/>.</returns>
    public async Task<MusicDirectory> GetMusicDirectoryAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        var response = await ExecuteAsync<GetMusicDirectoryResponse>(HttpMethod.Get, "getMusicDirectory",
            parameters: [new KeyValuePair<string, string>("id", id)]);

        return response.Directory;
    }

    /// <summary>
    /// Returns all genres.
    /// </summary>
    /// <returns>
    /// A task containing a collection of <see cref="Genre"/>
    /// </returns>
    public async Task<IEnumerable<Genre>> GetGenresAsync()
    {
        var response = await ExecuteAsync<GetGenresResponse>(HttpMethod.Get, "getGenres");
        return response.Genres.Genre;
    }

    /// <summary>
    ///  Similar to getIndexes, but organizes music according to ID3 tags. 
    /// </summary>
    /// <returns>
    /// A task containing a collection of <see cref="Index"/>
    /// </returns>
    public async Task<IEnumerable<Index>> GetArtistsAsync()
    {
        var response = await ExecuteAsync<GetArtistsResponse>(HttpMethod.Get, "getArtists");
        return response.Artists.Index;
    }

    /// <summary>
    ///  Returns details for an artist, including a list of albums. This method organizes music according to ID3 tags. 
    /// </summary>
    /// <param name="id">The ID of the artist. Required.</param>
    /// <returns>A task containing a <see cref="Artist"/>.</returns>
    public async Task<Artist> GetArtistAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        var response = await ExecuteAsync<GetArtistResponse>(HttpMethod.Get, $"getArtist",
            parameters: new List<KeyValuePair<string, string>>
            {
                new("id", id)
            });

        return response.Artist;
    }

    /// <summary>
    ///  Returns details for an album, including a list of songs. This method organizes music according to ID3 tags. 
    /// </summary>
    /// <param name="id">The ID of the album. Required.</param>
    /// <returns>A task containing a <see cref="Album"/>.</returns>
    public async Task<Album> GetAlbumAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id), "Album ID cannot be null or empty.");
        }

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id),
        };
    
        var result = await ExecuteAsync<GetAlbumResponse>(HttpMethod.Get, "getAlbum", parameters: parameters);
        return result.Album;
    }

    /// <summary>
    ///  Returns details for a song. 
    /// </summary>
    /// <param name="id">The ID of the song. Required.</param>
    /// <returns>A task containing a <see cref="Song"/>.</returns>
    public async Task<Song> GetSongAsync(string id)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id)
        };

        var response = await ExecuteAsync<GetSongResponse>(HttpMethod.Get, "getSong", null, parameters);
        return response.Song;
    }

    /// <summary>
    ///  Returns all video files. 
    /// </summary>
    /// <returns>A task containing a collection of <see cref="Video"/>.</returns>
    public async Task<Video> GetVideosAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///   Returns details for a video, including information about available audio tracks, subtitles (captions) and conversions. 
    /// </summary>
    /// <param name="id">The ID of the video. Required.</param>
    /// <returns>A task containing a <see cref="Video"/>.</returns>
    public async Task<Video> GetVideoInfoAsync(string id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns artist info with biography, image URLs and similar artists, using data from last.fm.  
    /// </summary>
    /// <param name="id">The artist, album or song ID. Required.</param>
    /// <param name="count">Max number of similar artists to return.</param>
    /// <param name="includeNotPresent">Whether to return artists that are not present in the media library.</param>
    /// <returns>A task containing a <see cref="ArtistInfo"/>.</returns>
    public async Task<ArtistInfo> GetArtistInfoAsync(string id, int? count = 20, bool? includeNotPresent = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id),
            new("count", count.ToString()!),
            new("includeNotPresent", includeNotPresent.ToString()!.ToLower())
        };
        
        var response = await ExecuteAsync<GetArtistInfoResponse>(HttpMethod.Get, "getArtistInfo", null, parameters);
        return response.ArtistInfo;
    }

    /// <summary>
    /// Similar to <see cref="GetArtistInfoAsync"/>, but organizes music according to ID3 tags. AKA GetArtistInfo2
    /// </summary>
    /// <param name="id">The artist, album or song ID. Required.</param>
    /// <param name="count">Max number of similar artists to return.</param>
    /// <param name="includeNotPresent">Whether to return artists that are not present in the media library.</param>
    /// <returns>A task containing a <see cref="ArtistInfo"/>.</returns>
    public async Task<ArtistInfo> GetArtistInfo2Async(string id, int? count = 20, bool? includeNotPresent = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id),
            new("count", count.ToString()!),
            new("includeNotPresent", includeNotPresent.ToString()!.ToLower())
        };
        
        var response = await ExecuteAsync<GetArtistInfo2Response>(HttpMethod.Get, "getArtistInfo2", null, parameters);
        return response.ArtistInfo;
    }

    /// <summary>
    ///  Returns a random collection of songs from the given artist and similar artists, using data from last.fm. Typically used for artist radio features. 
    /// </summary>
    /// <param name="id">Required. The artist, album or song ID. Required.</param>
    /// <param name="count">Optional. Max number of similar artists to return. Defaults to 50.</param>
    /// <returns>A task containing a collection of <see cref="Song"/>.</returns>
    public async Task<IEnumerable<Song>> GetSimilarSongsAsync(string id, int? count = 50)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id)
        };

        if (count.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string>("count", count.Value.ToString()));
        }

        var response = await ExecuteAsync<GetSimilarSongsResponse>(HttpMethod.Get, "getSimilarSongs", null, parameters);

        return response.Songs.Song;
    }
    
    /// <summary>
    ///   Similar to <see cref="GetSimilarSongsAsync"/>, but organizes music according to ID3 tags.  
    /// </summary>
    /// <param name="id">Required. The artist, album or song ID. Required.</param>
    /// <param name="count">Optional. Max number of similar artists to return. Defaults to 50.</param>
    /// <returns>A task containing a collection of <see cref="Song"/>.</returns>
    public async Task<IEnumerable<Song>> GetSimilarSongs2Async(string id, int? count = 50)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id)
        };

        if (count.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string>("count", count.Value.ToString()));
        }

        var response = await ExecuteAsync<GetSimilarSongs2Response>(HttpMethod.Get, "getSimilarSongs2", null, parameters);

        return response.Songs.Song;
    }

    #endregion

    #region Search

    /// <summary>
    ///  Returns albums, artists and songs matching the given search criteria. Supports paging through the result. 
    /// </summary>
    /// <param name="request">A <see cref="SubsonicSearchRequest"/> containing parameters for the search.</param>
    /// <returns>A task containing <see cref="SearchResult"/>.</returns>
    public async Task<SearchResult> Search2Async(SubsonicSearchRequest request)
    {
        var queryParameters = new List<KeyValuePair<string, string>>
        {
            new("query", request.Query),
            new("artistCount", request.ArtistCount.ToString()!),
            new("artistOffset", request.ArtistOffset.ToString()!),
            new("albumCount", request.AlbumCount.ToString()!),
            new("albumOffset", request.AlbumOffset.ToString()!),
            new("songCount", request.SongCount.ToString()!),
            new("songOffset", request.SongOffset.ToString()!)
        };

        if (request.MusicFolderId.HasValue)
        {
            queryParameters.Add(
                new KeyValuePair<string, string>("musicFolderId", request.MusicFolderId.Value.ToString()));
        }

        var relativePath = "search2";
        var response = await ExecuteAsync<Search2Response>(HttpMethod.Get, relativePath, null, queryParameters);
        return response.SearchResult;
    }

    /// <summary>
    ///   Similar to <see cref="Search2Async"/>, but organizes music according to ID3 tags.  
    /// </summary>
    /// <param name="request">A <see cref="SubsonicSearchRequest"/> containing parameters for the search.</param>
    /// <returns>A task containing <see cref="SearchResult"/>.</returns>
    public async Task<SearchResult> Search3Async(SubsonicSearchRequest request)
    {
        var queryParameters = new List<KeyValuePair<string, string>>
        {
            new("query", request.Query),
            new("artistCount", request.ArtistCount.ToString()!),
            new("artistOffset", request.ArtistOffset.ToString()!),
            new("albumCount", request.AlbumCount.ToString()!),
            new("albumOffset", request.AlbumOffset.ToString()!),
            new("songCount", request.SongCount.ToString()!),
            new("songOffset", request.SongOffset.ToString()!)
        };

        if (request.MusicFolderId.HasValue)
        {
            queryParameters.Add(
                new KeyValuePair<string, string>("musicFolderId", request.MusicFolderId.Value.ToString()));
        }

        const string relativePath = "search3";
        var response = await ExecuteAsync<Search3Response>(HttpMethod.Get, relativePath, null, queryParameters);
        return response.SearchResult;
    }

    #endregion

    #region AlbumSonglists

    /// <summary>
    ///  Returns starred songs, albums and artists.   
    /// </summary>
    /// <param name="musicFolderId">Optional</param>
    /// <returns>A task containing <see cref="SearchResult"/>.</returns>
    public async Task<SearchResult> GetStarredAsync(string? musicFolderId = null)
    {
        var parameters = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(musicFolderId))
        {
            parameters.Add(new KeyValuePair<string, string>("musicFolderId", musicFolderId));
        }

        var response = await ExecuteAsync<GetStarredResponse>(HttpMethod.Get, "getStarred", null, parameters);
        return response.SearchResult;
    }

    /// <summary>
    ///   Similar to  <see cref="GetStarredAsync"/>, but organizes music according to ID3 tags.   
    /// </summary>
    /// <param name="musicFolderId">Optional</param>
    /// <returns>A task containing <see cref="SearchResult"/>.</returns>
    public async Task<SearchResult> GetStarred2Async(string? musicFolderId = null)
    {
        var parameters = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(musicFolderId))
        {
            parameters.Add(new KeyValuePair<string, string>("musicFolderId", musicFolderId));
        }

        var response = await ExecuteAsync<GetStarred2Response>(HttpMethod.Get, "getStarred2", null, parameters);
        return response.SearchResult;
    }

    public async Task<IEnumerable<Song>> GetRandomSongsAsync(int? size = 10, string? genre = null, int? fromYear = null, int? toYear = null,
        string? musicFolderId = null)
    {
        var parameters = new List<KeyValuePair<string, string>>();

        if (size.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string>("size", size.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(genre))
        {
            parameters.Add(new KeyValuePair<string, string>("genre", genre));
        }

        if (fromYear.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string>("fromYear", fromYear.Value.ToString()));
        }

        if (toYear.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string>("toYear", toYear.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(musicFolderId))
        {
            parameters.Add(new KeyValuePair<string, string>("musicFolderId", musicFolderId));
        }

        // Fetch the random songs using ExecuteAsync method
        var response = await ExecuteAsync<GetRandomSongsResponse>(HttpMethod.Get, "getRandomSongs", parameters: parameters);
            
        // Return the list of Song entities
        return response?.Songs?.Song;
    }

    #endregion

    #region Playlists

    /// <summary>
    /// Returns all playlists a user is allowed to play. 
    /// </summary>
    /// <param name="userName">Optional</param>
    /// <returns>A task containing a collection of <see cref="Playlist"/>.</returns>
    public async Task<IEnumerable<Playlist>> GetPlaylistsAsync(string? userName = null)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            parameters.Add(new KeyValuePair<string, string>(UserNameParameter, userName));
        }

        var response =
            await ExecuteAsync<GetPlaylistsResponse>(HttpMethod.Get, "getPlaylists", parameters: parameters);

        return response.Playlists.Playlist;
    }

    /// <summary>
    /// Returns a listing of files in a saved playlist. 
    /// </summary>
    /// <param name="playlistId">Required</param>
    /// <returns>A task containing a collection of <see cref="Playlist"/>.</returns>
    public async Task<IEnumerable<Playlist>> GetPlaylistAsync(string playlistId)
    {
        var parameters = new List<KeyValuePair<string, string>>();

        parameters.Add(new KeyValuePair<string, string>(UserNameParameter, playlistId));

        var response =
            await ExecuteAsync<GetPlaylistsResponse>(HttpMethod.Get, "getPlaylist", parameters: parameters);

        return response.Playlists.Playlist;
    }

    /// <summary>
    /// Creates a playlist.  
    /// </summary>
    /// <param name="name">Required</param>
    /// <param name="songIds">Optional. The song ids to include in the new playlist</param>
    /// <returns>A task containing the newly created <see cref="Playlist"/>.</returns>
    public async Task<Playlist> CreatePlaylistAsync(string name, IEnumerable<string>? songIds = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("name", name)
        };

        if (songIds != null)
        {
            parameters.AddRange(songIds.Select(id => new KeyValuePair<string, string>("songId", id)));
        }

        var response =
            await ExecuteAsync<GetPlaylistResponse>(HttpMethod.Post, "createPlaylist", null, parameters);
        return response.Playlist;
    }

    /// <summary>
    /// Replaces an existing playlist.  
    /// </summary>
    /// <param name="playlistId">Required</param>
    /// <param name="songIds">Optional. The song ids to include the new version of the playlist</param>
    /// <returns>A task containing the newly created <see cref="Playlist"/>.</returns>
    public async Task<Playlist> ReplacePlaylistAsync(string playlistId, IEnumerable<string>? songIds = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("playlistId", playlistId)
        };

        if (songIds != null)
        {
            parameters.AddRange(songIds.Select(id => new KeyValuePair<string, string>("songId", id)));
        }

        var response =
            await ExecuteAsync<GetPlaylistResponse>(HttpMethod.Get, "createPlaylist", null, parameters);
        return response.Playlist;
    }

    /// <summary>
    /// Updates a playlist. Only the owner of a playlist is allowed to update it.   
    /// </summary>
    /// <param name="playlistId">Required</param>
    /// <param name="name">Optional. The human-readable name of the playlist.</param>
    /// <param name="comment">Optional. The playlist comment.</param>
    /// <param name="isPublic">Optional. Defaults to true</param>
    /// <param name="songIds">Optional. The song ids to add to the playlist</param>
    /// <param name="songIndexesToRemove">Optional. List of songs to remove from the playlist. Identified by the zero-based index of the entry within the playlist.</param>
    /// <returns>A bool indicating success</returns>
    public async Task<bool> UpdatePlaylistAsync(string playlistId, string? name = null, string? comment = null,
        bool? isPublic = true, IEnumerable<string>? songIds = null, IEnumerable<string>? songIndexesToRemove = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("playlistId", playlistId)
        };

        if (!string.IsNullOrEmpty(name))
            parameters.Add(new KeyValuePair<string, string>("name", name));
        if (!string.IsNullOrEmpty(comment))
            parameters.Add(new KeyValuePair<string, string>("comment", comment));
        if (isPublic.HasValue)
            parameters.Add(new KeyValuePair<string, string>("public", isPublic.Value.ToString().ToLower()));

        if (songIds != null)
        {
            parameters.AddRange(songIds.Select(songId => new KeyValuePair<string, string>("songId", songId)));
        }

        if (songIndexesToRemove != null)
        {
            parameters.AddRange(songIndexesToRemove.Select(index =>
                new KeyValuePair<string, string>("songIndexToRemove", index)));
        }

        try
        {
            var response = await ExecuteAsync<BaseResponse>(HttpMethod.Post, "updatePlaylist", null, parameters);
            return response.IsSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating playlist with ID {PlaylistId}", playlistId);
            return false;
        }
    }

    /// <summary>
    /// Deletes an existing playlist.  
    /// </summary>
    /// <param name="playlistId">Required</param>
    /// <returns>A bool indicating success</returns>
    public async Task<bool> DeletePlaylistAsync(string playlistId)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", playlistId)
        };

        var response = await ExecuteAsync<BaseResponse>(HttpMethod.Delete, "deletePlaylist", null, parameters);
        return response.IsSuccess();
    }

    #endregion

    #region MediaAnnotation

    /// <summary>
    ///  Attaches a star to a song, album or artist.
    ///  At least one of songIds, albumIds, or artistIds must be provided
    /// </summary>
    /// <param name="songIds">Optional</param>
    /// <param name="albumIds">Optional</param>
    /// <param name="artistIds">Optional</param>
    /// <returns>A bool indicating success</returns>
    public async Task<bool> StarAsync(IEnumerable<string>? songIds, IEnumerable<string>? albumIds, IEnumerable<string>? artistIds = null)
    {
        if (songIds == null && albumIds == null && artistIds == null)
        {
            throw new ArgumentException("At least one of songIds, albumIds, or artistIds must be provided.");
        }

        var parameters = new List<KeyValuePair<string, string>>();
        
        if (songIds != null)
        {
            parameters.AddRange(songIds.Select(id => new KeyValuePair<string, string>("id", id)));
        }

        if (albumIds != null)
        {
            parameters.AddRange(albumIds.Select(id => new KeyValuePair<string, string>("albumId", id)));
        }

        if (artistIds != null)
        {
            parameters.AddRange(artistIds.Select(id => new KeyValuePair<string, string>("artistId", id)));
        }

        var response = await ExecuteAsync<BaseResponse>(HttpMethod.Get, "star", parameters: parameters);
        
        return response.IsSuccess();
    }
    
    /// <summary>
    ///  Removes the star from a song, album or artist. 
    ///  At least one of songIds, albumIds, or artistIds must be provided
    /// </summary>
    /// <param name="songIds">Optional</param>
    /// <param name="albumIds">Optional</param>
    /// <param name="artistIds">Optional</param>
    /// <returns>A bool indicating success</returns>
    public async Task<bool> UnStarAsync(IEnumerable<string>? songIds, IEnumerable<string>? albumIds, IEnumerable<string>? artistIds = null)
    {
        if (songIds == null && albumIds == null && artistIds == null)
        {
            throw new ArgumentException("At least one of songIds, albumIds, or artistIds must be provided.");
        }

        var parameters = new List<KeyValuePair<string, string>>();
        
        if (songIds != null)
        {
            parameters.AddRange(songIds.Select(id => new KeyValuePair<string, string>("id", id)));
        }

        if (albumIds != null)
        {
            parameters.AddRange(albumIds.Select(id => new KeyValuePair<string, string>("albumId", id)));
        }

        if (artistIds != null)
        {
            parameters.AddRange(artistIds.Select(id => new KeyValuePair<string, string>("artistId", id)));
        }

        var response = await ExecuteAsync<BaseResponse>(HttpMethod.Get, "unstar", parameters: parameters);
        
        return response.IsSuccess();
    }

    /// <summary>
    ///   Sets the rating for a music file. 
    /// </summary>
    /// <param name="id">Required. A string which uniquely identifies the file (song) or folder (album/artist) to rate.</param>
    /// <param name="rating">Required. The rating between 1 and 5 (inclusive), or 0 to remove the rating.</param>
    /// <returns>A bool indicating success</returns>
    public async Task<bool> SetRatingAsync(string id, double rating = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Song ID cannot be null or whitespace.", nameof(id));
        
        if (rating is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 0 and 5.");
        
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("id", id),
            new("rating", rating.ToString(CultureInfo.InvariantCulture))
        };
        
        var response = await ExecuteAsync<BaseResponse>(HttpMethod.Post, "setRating", null, parameters);
        return response.IsSuccess();
    }

    
    
    #endregion

    #region MediaRetrieval

    /// <summary>
    ///   Searches for and returns lyrics for a given song. 
    /// </summary>
    /// <param name="artist">Required</param>
    /// <param name="song">Required</param>
    /// <param name="decodeHtml">Optional</param>
    /// <returns>A task containing the lyrics in string form. string.Empty if no lyrics are found</returns>
    public async Task<string> GetLyricsAsync(string artist, string song, bool decodeHtml = true)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("artist", artist),
            new("title", song)
        };
        
        var response = await ExecuteAsync<GetLyricsResponse>(HttpMethod.Get, "getLyrics", parameters: parameters);
        
        if(decodeHtml)
            return response?.Lyrics?.value.Decode() ?? string.Empty;
        
        return response?.Lyrics?.value ?? string.Empty;
    }

    #endregion
}