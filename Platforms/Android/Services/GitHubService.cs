using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MAUISilentUpdateTestApplication.Platforms.Android.Services;
public class GitHubService
{
    //private readonly string _gitHubToken;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubService(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
    }

    public async Task<string> GetLatestVersionFromGitHub()
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync($"https://api.github.com/repos/{_owner}/{_repo}/releases/latest");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            var latestRelease = JsonSerializer.Deserialize<GitHubRelease>(responseBody);
            if (latestRelease == null)
            {
                throw new Exception("No releases found.");
            }

            return latestRelease.TagName;
        }
    }

    public async Task<GitHubAsset> DownloadLatestAssetFromGitHub(string assetName, string assetUrl)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(assetUrl);
            response.EnsureSuccessStatusCode();

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            return new GitHubAsset { Name = assetName, Content = fileBytes };
        }
    }
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;
}

public class GitHubAsset
{
    public string Name { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}