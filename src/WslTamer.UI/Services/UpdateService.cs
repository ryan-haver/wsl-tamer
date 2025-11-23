using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Windows;

namespace WslTamer.UI.Services;

public class UpdateService
{
    private const string RepoOwner = "ryan-haver";
    private const string RepoName = "wsl-tamer";
    private const string CurrentVersion = "v0.1.0"; // Should match tag

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
                    "Do you want to download it now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = release.HtmlUrl,
                        UseShellExecute = true
                    });
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

    private bool IsNewerVersion(string tagName)
    {
        // Simple string comparison for now, assuming vX.Y.Z format
        // Remove 'v' prefix
        var current = CurrentVersion.TrimStart('v');
        var latest = tagName.TrimStart('v');
        
        return string.Compare(latest, current, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";
    }
}
