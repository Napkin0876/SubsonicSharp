using System.Security.Cryptography;

namespace SubsonicSharp;

public class SubsonicAuth
{
    public string Username { get; set; }
    public bool Plaintext { get; set; }
    public string Token { get; set; }
    public string? Salt { get; set; }

    public SubsonicAuth(string username, string password, bool plaintext = false, int saltLength = 10)
    {
        Plaintext = plaintext;
        Username = username;
        if (Plaintext)
        {
            Token = password;
        }
        else
        {
            Salt = GenerateSalt(saltLength);
            Token = GenerateMd5(password + Salt);
        }
    }

    private static string GenerateMd5(string text)
    {
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(text);
        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }

    private static string GenerateSalt(int length)
    {
        const string valid = "0123456789abcdefghijklmnopqrstuvwxyz";
        
        Random rand = new Random();
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = valid[rand.Next(valid.Length)];
        }

        return new string(chars);
    }

    internal void UpdatePassword(string password)
    {
        Token = GenerateMd5(password + Salt);
    }

    public override string ToString()
    {
        if (Plaintext)
            return $"u={Username}&p={Token}";
        else
        {
            return $"u={Username}&t={Token}&s={Salt}";
        }
    }
}