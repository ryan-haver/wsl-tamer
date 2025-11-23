using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Windows;
using System.IO;

namespace WslTamer.UI.Services;

public class UpdateService
{
    private const string RepoOwner = "ryan-haver";
    private const string RepoName = "wsl-tamer";
    private const string CurrentVersion = "v1.0.4"; // Should match tag

    public async Task CheckForUpdatesAsync(bool silent = false)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WslTamer-Updater");

            var release = await client.GetFromJsonAsync<GitHubRelease>(
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");

            if (release != null && IsNewerVersion(release.TagName))
            {
                var result = System.Windows.MessageBox.Show(
                    $"A new version ({release.TagName}) is available!\n\n" +
                    $"Release Notes:\n{release.Body}\n\n" +
                    "Do you want to install it now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadAndInstallUpdate(release, client);
                }
            }
            else if (!silent)
            {
                System.Windows.MessageBox.Show("You are running the latest version.", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                System.Windows.MessageBox.Show($"Failed to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task DownloadAndInstallUpdate(GitHubRelease release, HttpClient client)
    {
        try
        {
            // Find MSI asset
            var msiAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));
            
            if (msiAsset == null)
            {
                // Fallback to browser download if no MSI found
                Process.Start(new ProcessStartInfo
                {
                    FileName = release.HtmlUrl,
                    UseShellExecute = true
                });
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), msiAsset.Name);
            
            // Download
            // Note: In a real app, we'd show a progress bar here
            var data = await client.GetByteArrayAsync(msiAsset.BrowserDownloadUrl);
            await File.WriteAllBytesAsync(tempPath, data);

            // Install
            Process.Start(new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{tempPath}\" /passive",
                UseShellExecute = true
            });

            // Shutdown current app to allow update
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool IsNewerVersion(string tagName)
    {
        try
        {
            var current = Version.Parse(CurrentVersion.TrimStart('v'));
            var latest = Version.Parse(tagName.TrimStart('v'));
            return latest > current;
        }
        catch
        {
            // Fallback to string comparison if parsing fails
            var current = CurrentVersion.TrimStart('v');
            var latest = tagName.TrimStart('v');
            return string.Compare(latest, current, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";
    }
}
