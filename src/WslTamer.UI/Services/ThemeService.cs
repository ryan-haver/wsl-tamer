using System;
using System.Windows;
using Wpf.Ui.Appearance;

namespace WslTamer.UI.Services;

public class ThemeService
{
    public ThemeService()
    {
        // Initialize theme manager
        ApplicationThemeManager.ApplySystemTheme();
    }

    public void ApplyThemeToWindow(Window window, ApplicationTheme theme)
    {
        // WPF-UI handles this automatically for FluentWindow
        // But if we need to force it:
        ApplicationThemeManager.Apply(theme);
    }
    
    public ApplicationTheme CurrentTheme => ApplicationThemeManager.GetAppTheme();
}
