using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SubsonicSharp.Entities;
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

        if (parameters is {Count: > 0})
        {
            var queryString = string.Join("&",
                parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            relativePath = $"{relativePath}?{queryString}";
        }
        
        _logger.LogDebug($"Executing method {method} with path {relativePath}");

        var requestMessage = CreateHttpRequestMessage(method, relativePath, data);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
           
            var result = JsonSerializer.Deserialize<SubsonicApiResponse<T>>(responseContent, options) ??
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
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
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

    public async Task<BaseResponse> Ping()
    {
        var result =  await ExecuteAsync<BaseResponse>(HttpMethod.Get, "ping");
        
        return result;
    }

    #endregion

    #region Browsing

    /// <summary>
    ///  Returns all configured top-level music folders. Takes no extra parameters. 
    /// </summary>
    /// <returns>A task that contains a collection of <see cref="MusicFolder"/>.</returns>
    public async Task<IEnumerable<MusicFolder>> GetMusicFolders()
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
    public async Task<IEnumerable<Index>> GetIndexes(string? musicFolderId, DateTime? modifiedSince)
    {
        var queryParameters = new List<string>();

        if (!string.IsNullOrEmpty(musicFolderId))
        {
            queryParameters.Add($"musicFolderId={musicFolderId}");
        }

        if (modifiedSince.HasValue)
        {
            var millisecondsSinceEpoch = new DateTimeOffset(modifiedSince.Value).ToUnixTimeMilliseconds();
            queryParameters.Add($"modifiedSince={millisecondsSinceEpoch}");
        }

        var queryString = string.Join("&", queryParameters);
        var requestUri = string.IsNullOrEmpty(queryString) ? "getIndexes" : $"getIndexes?{queryString}";

        var response = await ExecuteAsync<GetIndexesResponse>(HttpMethod.Get, requestUri);
        return response.Indexes.Index;
    }

    /// <summary>
    /// Returns a listing of all files in a music directory.
    /// Typically used to get list of albums for an artist, or list of songs for an album. 
    /// </summary>
    /// <param name="id">The ID of the music directory. Required.</param>
    /// <returns>A task containing a <see cref="MusicDirectory"/>.</returns>
    public async Task<MusicDirectory> GetMusicDirectory(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        var response = await ExecuteAsync<GetMusicDirectoryResponse>(HttpMethod.Get, $"getMusicDirectory",
            parameters: new List<KeyValuePair<string, string>>
            {
                new("id", id)
            });

        return response.Directory;
    }

    /// <summary>
    /// Returns all genres.
    /// </summary>
    /// <returns>
    /// A task containing a collection of <see cref="Genre"/>
    /// </returns>
    public async Task<IEnumerable<Genre>> GetGenres()
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
    public async Task<IEnumerable<Index>> GetArtists()
    {
        var response = await ExecuteAsync<GetArtistsResponse>(HttpMethod.Get, "getArtists");
        return response.Artists.Index;
    }

    /// <summary>
    ///  Returns details for an artist, including a list of albums. This method organizes music according to ID3 tags. 
    /// </summary>
    /// <param name="id">The ID of the artist. Required.</param>
    /// <returns>A task containing a <see cref="Artist"/>.</returns>
    public async Task<Artist> GetArtist(string id)
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
    public async Task<Album> GetAlbum(string id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///  Returns details for a song. 
    /// </summary>
    /// <param name="id">The ID of the song. Required.</param>
    /// <returns>A task containing a <see cref="Song"/>.</returns>
    public async Task<Song> GetSong(string id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///  Returns all video files. 
    /// </summary>
    /// <returns>A task containing a collection of <see cref="Video"/>.</returns>
    public async Task<Video> GetVideos()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///   Returns details for a video, including information about available audio tracks, subtitles (captions) and conversions. 
    /// </summary>
    /// <param name="id">The ID of the video. Required.</param>
    /// <returns>A task containing a <see cref="Video"/>.</returns>
    public async Task<Video> GetVideoInfo(string id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///   Returns artist info with biography, image URLs and similar artists, using data from last.fm.  
    /// </summary>
    /// <param name="id">The artist, album or song ID. Required.</param>
    /// <param name="count">Max number of similar artists to return.</param>
    /// <param name="includeNotPresent">Whether to return artists that are not present in the media library.</param>
    /// <returns>A task containing a <see cref="Artist"/>.</returns>
    public async Task<Artist> GetArtistInfo(string id, int? count = 20, bool? includeNotPresent = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        throw new NotImplementedException();
    }

    /// <summary>
    ///    Similar to <see cref="GetArtistInfo"/>, but organizes music according to ID3 tags. AKA GetArtistInfo2
    /// </summary>
    /// <param name="id">The artist, album or song ID. Required.</param>
    /// <param name="count">Max number of similar artists to return.</param>
    /// <param name="includeNotPresent">Whether to return artists that are not present in the media library.</param>
    /// <returns>A task containing a <see cref="Artist"/>.</returns>
    public async Task<Artist> GetArtistInfoAlternate(string id, int? count = 20, bool? includeNotPresent = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Valid ID must be provided.", nameof(id));

        throw new NotImplementedException();
    }

    #endregion

    #region Search

    /// <summary>
    ///  Returns albums, artists and songs matching the given search criteria. Supports paging through the result. 
    /// </summary>
    /// <param name="request">A <see cref="SubsonicSearchRequest"/> containing parameters for the search.</param>
    /// <returns>A task containing <see cref="SearchResult"/>.</returns>
    public async Task<SearchResult> Search2(SubsonicSearchRequest request)
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
    ///   Similar to <see cref="Search2"/>, but organizes music according to ID3 tags.  
    /// </summary>
    /// <param name="request">A <see cref="SubsonicSearchRequest"/> containing parameters for the search.</param>
    /// <returns>A task containing <see cref="SearchResult"/>.</returns>
    public async Task<SearchResult> Search3(SubsonicSearchRequest request)
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
        var response = await ExecuteAsync<Search3Response>(HttpMethod.Get, relativePath, null, queryParameters);
        return response.SearchResult;
    }

    #endregion
}