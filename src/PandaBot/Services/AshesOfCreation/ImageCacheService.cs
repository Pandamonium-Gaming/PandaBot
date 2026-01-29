using PandaBot.Utils.Helpers;

namespace PandaBot.Services.AshesOfCreation;

public class ImageCacheService
{
    private readonly HttpClient _httpClient;

    public ImageCacheService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AshesForge");
    }

    public async Task<string> CacheImageAsync(string relativeUrl)
    {
        return await ImageCacheHelper.DownloadAndCacheImageAsync(relativeUrl, _httpClient);
    }

    public string GetImageUrl(string relativeUrl)
    {
        return ImageCacheHelper.GetFullImageUrl(relativeUrl);
    }

    public void ClearCache()
    {
        ImageCacheHelper.ClearCache();
    }

    public long GetCacheSizeInBytes()
    {
        return ImageCacheHelper.GetCacheSize();
    }
}
