using MAUISilentUpdateTestApplication.Platforms.Android.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAUISilentUpdateTestApplication.Platforms.Android.Services;
public class GitLabService
{
    private readonly string _gitLabToken;
    private readonly string _projectId;

    public GitLabService(string gitLabToken, string projectId)
    {
        _gitLabToken = gitLabToken;
        _projectId = projectId;
    }

    public async Task<string> GetLatestVersionFromGitLab()
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Private-Token", _gitLabToken);

            var response = await client.GetAsync($"http://10.160.41.200/tugcan.aygul/mauiversiontestapplication/{_projectId}/releases");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            var latestVersion = ParseLatestVersion(responseBody);
            return latestVersion;
        }
    }

    private string ParseLatestVersion(string responseBody)
    {
        var releases = JsonSerializer.Deserialize<List<GitLabRelease>>(responseBody);

        if(releases == null || releases.Count == 0)
        {
            throw new Exception("No releases found.");
        }

        var latestRelease = releases[0];

        return latestRelease.TagName;
    }

    public async Task AddAssetToRelease(string tagName, string filePath, string fileName)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _gitLabToken);

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var byteArrayContent = new ByteArrayContent(fileBytes);
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            var formData = new MultipartFormDataContent();
            formData.Add(byteArrayContent, "file", fileName);

            var response = await client.PostAsync($"https://gitlab.com/api/v4/projects/{_projectId}/uploads", formData);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var uploadResult = JsonSerializer.Deserialize<GitLabUploadResponse>(responseBody);

            var linkResponse = await client.PostAsync(
                $"https://gitlab.com/api/v4/projects/{_projectId}/releases/{tagName}/assets/links",
                new StringContent(JsonSerializer.Serialize(new
                {
                    name = fileName,
                    url = uploadResult.Url
                }), System.Text.Encoding.UTF8, "application/json"));

            linkResponse.EnsureSuccessStatusCode();
        }
    }
}

public class GitLabUploadResponse
{
    public string? Url { get; set; }
}
