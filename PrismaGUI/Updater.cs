using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Prisma;

namespace PrismaGUI;

public sealed class Updater : IDisposable
{
    private readonly HttpClient _httpClient;

    private const string RepositoryName = "Mainframe98/Prisma";
    private const string ApiPath = "https://api.github.com/repos/" + RepositoryName;
    public const string RepositoryRoot = "https://github.com/" + RepositoryName;

    /// <summary>
    /// Cached latest available version. <code>null</code> if <see cref="HasUpdates"/> has not yet been called or couldn't determine if there was a new version.
    /// Returns current version if the application is up to date.
    /// </summary>
    public Version? CachedNewVersion { get; private set; }

    public Updater()
    {
        this._httpClient = new HttpClient();
        this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    /// <summary>
    /// Indicates if the value in <see cref="CachedNewVersion"/> is a newer version than the current version.
    /// </summary>
    private bool NewVersionIsNewer => this.CachedNewVersion != null && PrismaInfo.Version.CompareTo(this.CachedNewVersion) <= 0;

    /// <summary>
    /// Check for updates.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> HasUpdates()
    {
        HttpResponseMessage response = await this._httpClient.GetAsync(ApiPath + "/releases");

        if (!response.IsSuccessStatusCode)
        {
            Utilities.ApplicationLogger.Error(
                "Failed to get a coherent response from the update API. It provided code {Code}, with content {Content}",
                response.StatusCode,
                response.ToString()
            );

            return false;
        }

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            Utilities.ApplicationLogger.Error(
                "No releases found. Received JSON: {Json}",
                document.ToJsonString()
            );

            return false;
        }

        foreach (JsonElement release in document.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out JsonElement isDraft) && isDraft.ValueKind != JsonValueKind.False)
            {
                continue;
            }

            if (release.TryGetProperty("prerelease", out JsonElement isPreRelease) && isPreRelease.ValueKind != JsonValueKind.False)
            {
                continue;
            }

            if (!Version.TryParse(release.GetProperty("tag_name").GetString(), out Version? latestReleaseVersion))
            {
                continue;
            }

            this.CachedNewVersion = latestReleaseVersion;

            return this.NewVersionIsNewer;
        }

        return false;
    }

    /// <summary>
    /// Update Prisma to a newer version.
    ///
    /// This will exit the application!
    /// </summary>
    /// <exception cref="ArgumentException">Throw when <see cref="CachedNewVersion"/> is not a newer version</exception>
    public async Task Update()
    {
        if (!this.NewVersionIsNewer)
        {
            throw new ArgumentException("No newer version was found", nameof(this.CachedNewVersion));
        }

        string installerPath = Path.Combine(Path.GetTempPath(), "PrismaSetup.exe");

        await using (Stream installerStream = await this._httpClient.GetStreamAsync(RepositoryRoot + "/releases/latest/download/PrismaSetup.exe")) {
            await using (FileStream fileStream = File.OpenWrite(installerPath)) {
                await installerStream.CopyToAsync(fileStream);
            }
        }

        Process.Start(new ProcessStartInfo(installerPath)
        {
            UseShellExecute = false
        });

        Application.Current.Shutdown();
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        this._httpClient.Dispose();
    }
}
