using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WslTamer.UI.Services;

public class ThemeService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";

    public enum ThemeType
    {
        Light,
        Dark
    }

    public ThemeType CurrentTheme { get; private set; }

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        ApplySystemTheme();
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            ApplySystemTheme();
        }
    }

    public void ApplySystemTheme()
    {
        CurrentTheme = GetSystemTheme();
        ChangeTheme(CurrentTheme);
    }

    private ThemeType GetSystemTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        object? registryValueObject = key?.GetValue(RegistryValueName);
        if (registryValueObject == null)
        {
            return ThemeType.Light;
        }

        int registryValue = (int)registryValueObject;
        return registryValue > 0 ? ThemeType.Light : ThemeType.Dark;
    }

    private void ChangeTheme(ThemeType theme)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{theme}.xaml")
        };

        // Clear old theme dictionaries (assuming we only have one theme dict added)
        // A safer way is to find the old one by Source, but for now we'll just clear and re-add
        // or replace the specific one.
        // To be safe, let's just clear all and re-add (if we had other resources, this would be bad).
        // Better approach: Remove the old theme dictionary if it exists.
        
        var oldDict = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.ToString().Contains("Themes/"));
        if (oldDict != null)
        {
            app.Resources.MergedDictionaries.Remove(oldDict);
        }
        
        app.Resources.MergedDictionaries.Add(dict);

        // Update all open windows
        foreach (Window window in app.Windows)
        {
            ApplyThemeToWindow(window, theme);
        }
    }

    public void ApplyThemeToWindow(Window window, ThemeType theme)
    {
        if (theme == ThemeType.Dark)
        {
            UseImmersiveDarkMode(window, true);
        }
        else
        {
            UseImmersiveDarkMode(window, false);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private static bool UseImmersiveDarkMode(Window window, bool enabled)
    {
        if (IsWindows10OrGreater(17763))
        {
            var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
            if (IsWindows10OrGreater(18985))
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }

            int useImmersiveDarkMode = enabled ? 1 : 0;
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            return DwmSetWindowAttribute(hwnd, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
        }

        return false;
    }

    private static bool IsWindows10OrGreater(int build = -1)
    {
        return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
    }
}
