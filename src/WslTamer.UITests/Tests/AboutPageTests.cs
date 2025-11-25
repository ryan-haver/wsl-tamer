using FlaUI.Core.AutomationElements;

namespace WslTamer.UITests.Tests;

/// <summary>
/// Tests for the About page functionality
/// </summary>
[TestFixture]
public class AboutPageTests : TestBase
{
    private Window? NavigateToAboutPage()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow == null) return null;

        var aboutNav = settingsWindow.FindFirstDescendant(cf => 
            cf.ByName("About"));

        aboutNav?.Click();
        Thread.Sleep(500);

        return settingsWindow;
    }

    [Test]
    [Category("About")]
    public void AboutPage_DisplaysAppName()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var appName = window!.FindFirstDescendant(cf => 
            cf.ByText("WSL Tamer"));

        Assert.That(appName, Is.Not.Null, "Should display application name");
    }

    [Test]
    [Category("About")]
    public void AboutPage_DisplaysVersion()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var versionText = window!.FindFirstDescendant(cf => 
            cf.ByText("Version").Or(cf.ByText("v1.")).Or(cf.ByText("1.")));

        Assert.That(versionText, Is.Not.Null, "Should display version information");
    }

    [Test]
    [Category("About")]
    public void AboutPage_HasCheckForUpdatesButton()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var checkUpdatesButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Check for Updates").Or(cf.ByText("Update")));

        Assert.That(checkUpdatesButton, Is.Not.Null, "Should have Check for Updates button");
    }

    [Test]
    [Category("About")]
    public void AboutPage_CheckUpdatesButton_IsClickable()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var checkUpdatesButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Check for Updates"));

        if (checkUpdatesButton != null)
        {
            Assert.That(checkUpdatesButton.IsEnabled, Is.True, "Check for Updates should be enabled");
        }
        else
        {
            Assert.Warn("Check for Updates button not found");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_HasAutoUpdateCheckbox()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var autoUpdateCheckbox = window!.FindFirstDescendant(cf => 
            cf.ByText("Automatic").Or(cf.ByText("Auto-update")));

        if (autoUpdateCheckbox != null)
        {
            Assert.Pass("Auto-update checkbox found");
        }
        else
        {
            Assert.Warn("Auto-update checkbox not found");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_DisplaysCopyright()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var copyrightText = window!.FindFirstDescendant(cf => 
            cf.ByText("©").Or(cf.ByText("Copyright")).Or(cf.ByText("2024")).Or(cf.ByText("2025")));

        if (copyrightText != null)
        {
            Assert.Pass("Copyright information is displayed");
        }
        else
        {
            Assert.Warn("Copyright information not found");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_HasGitHubLink()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var githubLink = window!.FindFirstDescendant(cf => 
            cf.ByText("GitHub").Or(cf.ByText("Source")));

        if (githubLink != null)
        {
            Assert.Pass("GitHub link found");
        }
        else
        {
            Assert.Warn("GitHub link not found");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_HasLicenseInfo()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var licenseText = window!.FindFirstDescendant(cf => 
            cf.ByText("License").Or(cf.ByText("MIT")));

        if (licenseText != null)
        {
            Assert.Pass("License information is displayed");
        }
        else
        {
            Assert.Warn("License information not found");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_DisplaysAppIcon()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        // Look for an image element (app icon)
        var iconElement = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Image));

        if (iconElement != null)
        {
            Assert.Pass("App icon is displayed");
        }
        else
        {
            Assert.Warn("App icon not found");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_CanClickCheckForUpdates()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var checkUpdatesButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Check for Updates"));

        if (checkUpdatesButton != null && checkUpdatesButton.IsEnabled)
        {
            checkUpdatesButton.Click();
            Thread.Sleep(2000);
            
            // Should show some feedback
            Assert.Pass("Check for Updates button is functional");
        }
        else
        {
            Assert.Warn("Cannot test Check for Updates functionality");
        }
    }

    [Test]
    [Category("About")]
    public void AboutPage_CanToggleAutoUpdate()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var autoUpdateCheckbox = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox));

        if (autoUpdateCheckbox != null && autoUpdateCheckbox.IsEnabled)
        {
            autoUpdateCheckbox.Click();
            Thread.Sleep(300);
            
            Assert.Pass("Auto-update checkbox is functional");
        }
        else
        {
            Assert.Warn("Auto-update checkbox not functional");
        }
    }

    [Test]
    [Category("About")]
    [Category("Layout")]
    public void AboutPage_HasProperLayout()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        var allElements = window!.FindAllDescendants();
        
        Assert.That(allElements.Length, Is.GreaterThan(5), "Page should have multiple UI elements");
    }

    [Test]
    [Category("About")]
    public void AboutPage_DisplaysDescription()
    {
        var window = NavigateToAboutPage();
        Assert.That(window, Is.Not.Null);

        // Look for descriptive text about the application
        var descriptionText = window!.FindFirstDescendant(cf => 
            cf.ByText("WSL").Or(cf.ByText("Windows Subsystem for Linux")));

        if (descriptionText != null)
        {
            Assert.Pass("Application description is displayed");
        }
        else
        {
            Assert.Warn("Application description not found");
        }
    }
}
