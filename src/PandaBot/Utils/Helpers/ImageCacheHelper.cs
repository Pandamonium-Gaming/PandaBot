using System.Security.Cryptography;
using System.Text;

namespace PandaBot.Utils.Helpers;

public static class ImageCacheHelper
{
    private const string BaseUrl = "https://cdn.ashesforge.com";
    private const string CacheDirectory = "cache/images";

    public static string GetFullImageUrl(string relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return string.Empty;

        if (relativeUrl.StartsWith("http"))
            return relativeUrl;

        return $"{BaseUrl}{relativeUrl}";
    }

    public static async Task<string> DownloadAndCacheImageAsync(string relativeUrl, HttpClient httpClient)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return string.Empty;

        var fullUrl = GetFullImageUrl(relativeUrl);
        var fileName = GetCachedFileName(relativeUrl);
        var localPath = Path.Combine(CacheDirectory, fileName);

        // Create cache directory if it doesn't exist
        Directory.CreateDirectory(CacheDirectory);

        // If file already exists, return the path
        if (File.Exists(localPath))
            return localPath;

        try
        {
            var imageBytes = await httpClient.GetByteArrayAsync(fullUrl);
            await File.WriteAllBytesAsync(localPath, imageBytes);
            return localPath;
        }
        catch (Exception ex)
        {
            // Log error and return empty string
            Console.WriteLine($"Failed to download image {fullUrl}: {ex.Message}");
            return string.Empty;
        }
    }

    private static string GetCachedFileName(string relativeUrl)
    {
        var extension = Path.GetExtension(relativeUrl);
        var hash = ComputeHash(relativeUrl);
        return $"{hash}{extension}";
    }

    private static string ComputeHash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static void ClearCache()
    {
        if (Directory.Exists(CacheDirectory))
        {
            Directory.Delete(CacheDirectory, true);
        }
    }

    public static long GetCacheSize()
    {
        if (!Directory.Exists(CacheDirectory))
            return 0;

        var files = Directory.GetFiles(CacheDirectory, "*", SearchOption.AllDirectories);
        return files.Sum(file => new FileInfo(file).Length);
    }
}
