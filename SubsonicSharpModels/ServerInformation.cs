namespace SubsonicSharp;

public class ServerConfiguration
{
    public string Host { get; set; } 
    public int Port { get; set; } 
    public string Basepath { get; set; }
    public string ApiVersion { get; set; }
    public const string AppName = "SubsonicSharp";
    public Protocol ConnectionProtocol { get; set; } 

    public enum Protocol
    {
        http,
        https
    }

    public ServerConfiguration(string host, int port = 4533, string apiVersion = "1.16.1", string basepath = "/rest", Protocol protocol = Protocol.http)
    {
        Host = host;
        Port = port;
        Basepath = basepath;
        ConnectionProtocol = protocol;
        ApiVersion = apiVersion;
    }

    public string BaseUrl() => $"{Host}:{Port}{Basepath}/";
}