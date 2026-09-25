using System.Diagnostics;
using System.Reflection;

namespace WslTamer.App.Services;

public static class AppPaths
{
    public static string ExecutablePath => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "WslTamer.exe");

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslTamer", "logs");

    public static string Version { get; } =
        typeof(AppPaths).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "dev";

    public const string RepositoryUrl = "https://github.com/ryan-haver/wsl-tamer";

    public static void OpenInExplorer(string path)
    {
        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { "/select,", path } })?.Dispose();
        }
        else if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { path } })?.Dispose();
        }
    }

    public static void OpenUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
    }

    public static void OpenInEditor(string file)
    {
        if (!File.Exists(file))
        {
            File.WriteAllText(file, string.Empty);
        }

        Process.Start(new ProcessStartInfo("notepad.exe") { ArgumentList = { file } })?.Dispose();
    }
}
