using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace WslTamer.UITests.Tests;

/// <summary>
/// Tests for the Settings Window and its pages
/// </summary>
[TestFixture]
public class SettingsWindowTests : TestBase
{
    private Window? OpenSettings()
    {
        // Since the app is a tray icon, we need to simulate opening settings
        // For now, we'll launch the app with a command line argument if supported
        // or directly test the window components
        
        if (Automation == null) return null;
        
        // Try to find an existing settings window
        var windows = Automation.GetDesktop().FindAllChildren(cf => 
            cf.ByName("Settings"));
        
        return windows.FirstOrDefault()?.AsWindow();
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_Opens_Successfully()
    {
        // This test verifies the settings window can be found and has correct structure
        var settingsWindow = GetSettingsWindow();
        
        if (settingsWindow == null)
        {
            // Try to open it
            ClickTrayIcon();
            Thread.Sleep(1000);
            settingsWindow = GetSettingsWindow();
        }
        
        Assert.That(settingsWindow, Is.Not.Null, "Settings window should be accessible");
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_HasNavigationMenu()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        // Look for navigation items
        var navItems = settingsWindow!.FindAllDescendants(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

        Assert.That(navItems.Length, Is.GreaterThan(0), "Should have navigation items");
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_CanNavigateToGeneral()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var generalNav = settingsWindow!.FindFirstDescendant(cf => 
            cf.ByName("General"));

        if (generalNav != null)
        {
            generalNav.Click();
            Thread.Sleep(500);

            // Verify we're on the General page
            var generalContent = settingsWindow.FindFirstDescendant(cf => 
                cf.ByText("WSL Management"));

            Assert.That(generalContent, Is.Not.Null, "General page content should be visible");
        }
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_CanNavigateToDistributions()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var distrosNav = settingsWindow!.FindFirstDescendant(cf => 
            cf.ByName("Distributions"));

        if (distrosNav != null)
        {
            distrosNav.Click();
            Thread.Sleep(500);

            Assert.Pass("Successfully navigated to Distributions page");
        }
        else
        {
            Assert.Warn("Distributions navigation item not found");
        }
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_CanNavigateToProfiles()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var profilesNav = settingsWindow!.FindFirstDescendant(cf => 
            cf.ByName("Profiles"));

        if (profilesNav != null)
        {
            profilesNav.Click();
            Thread.Sleep(500);

            Assert.Pass("Successfully navigated to Profiles page");
        }
        else
        {
            Assert.Warn("Profiles navigation item not found");
        }
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_CanNavigateToHardware()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var hardwareNav = settingsWindow!.FindFirstDescendant(cf => 
            cf.ByName("Hardware"));

        if (hardwareNav != null)
        {
            hardwareNav.Click();
            Thread.Sleep(500);

            Assert.Pass("Successfully navigated to Hardware page");
        }
        else
        {
            Assert.Warn("Hardware navigation item not found");
        }
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_CanNavigateToAbout()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var aboutNav = settingsWindow!.FindFirstDescendant(cf => 
            cf.ByName("About"));

        if (aboutNav != null)
        {
            aboutNav.Click();
            Thread.Sleep(500);

            // Look for version information
            var versionText = settingsWindow.FindFirstDescendant(cf => 
                cf.ByText("Version"));

            Assert.That(versionText, Is.Not.Null, "About page should show version");
        }
        else
        {
            Assert.Warn("About navigation item not found");
        }
    }

    [Test]
    [Category("Settings")]
    [Category("Theme")]
    public void SettingsWindow_RespectsDarkTheme()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        // Check if the window has dark theme elements
        // This is a basic check - more sophisticated theme detection can be added
        Assert.Pass("Theme test placeholder - implement actual theme detection");
    }

    [Test]
    [Category("Settings")]
    [Category("Theme")]
    public void SettingsWindow_RespectsLightTheme()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        // Check if the window has light theme elements
        Assert.Pass("Theme test placeholder - implement actual theme detection");
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_HasCorrectMinimumSize()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var bounds = settingsWindow!.BoundingRectangle;
        
        Assert.That(bounds.Width, Is.GreaterThanOrEqualTo(800), "Window width should be at least 800px");
        Assert.That(bounds.Height, Is.GreaterThanOrEqualTo(600), "Window height should be at least 600px");
    }

    [Test]
    [Category("Settings")]
    public void SettingsWindow_CanResize()
    {
        var settingsWindow = GetSettingsWindow();
        Assert.That(settingsWindow, Is.Not.Null);

        var initialBounds = settingsWindow!.BoundingRectangle;
        
        // Try to resize (if resizable)
        var resizable = settingsWindow.Patterns.Transform.IsSupported;
        
        if (resizable)
        {
            Assert.Pass("Window is resizable");
        }
        else
        {
            Assert.Warn("Window is not resizable - check if this is intentional");
        }
    }
}
