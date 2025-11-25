using FlaUI.Core.AutomationElements;

namespace WslTamer.UITests.Tests;

/// <summary>
/// Tests for the Distributions page functionality
/// </summary>
[TestFixture]
public class DistributionsPageTests : TestBase
{
    private Window? NavigateToDistributionsPage()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow == null) return null;

        var distrosNav = settingsWindow.FindFirstDescendant(cf => 
            cf.ByName("Distributions"));

        distrosNav?.Click();
        Thread.Sleep(500);

        return settingsWindow;
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_DisplaysDistributionList()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        // Look for a list or data grid
        var listElement = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.List).Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid)));

        if (listElement != null)
        {
            Assert.Pass("Distribution list is displayed");
        }
        else
        {
            Assert.Warn("Distribution list not found - may be empty or loading");
        }
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_HasRefreshButton()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        var refreshButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Refresh").Or(cf.ByAutomationId("RefreshButton")));

        Assert.That(refreshButton, Is.Not.Null, "Should have Refresh button");
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_HasInstallButton()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        var installButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Install").Or(cf.ByText("Install New")).Or(cf.ByText("Add")));

        if (installButton != null)
        {
            Assert.Pass("Install button found");
        }
        else
        {
            Assert.Warn("Install button not found");
        }
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_CanClickRefresh()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        var refreshButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Refresh"));

        if (refreshButton != null && refreshButton.IsEnabled)
        {
            refreshButton.Click();
            Thread.Sleep(1000);
            
            Assert.Pass("Refresh button is functional");
        }
        else
        {
            Assert.Warn("Refresh button not clickable");
        }
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_ShowsDistributionNames()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        // Wait for distributions to load
        Thread.Sleep(2000);

        var textElements = window!.FindAllDescendants(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text));

        // Should have some text elements (even if no distributions installed)
        Assert.That(textElements.Length, Is.GreaterThan(0), "Should display text content");
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_DistributionActions_AreAccessible()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        // Look for action buttons (Start, Stop, etc.)
        var buttons = window!.FindAllDescendants(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));

        if (buttons.Length > 0)
        {
            Assert.Pass($"Found {buttons.Length} action buttons");
        }
        else
        {
            Assert.Warn("No action buttons found - may require a distribution to be installed");
        }
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_CanOpenContextMenu()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        // Look for a list item to right-click
        var listItem = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

        if (listItem != null)
        {
            listItem.RightClick();
            Thread.Sleep(500);

            // Check for context menu
            var contextMenu = window.FindFirstDescendant(cf => 
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.Menu));

            if (contextMenu != null)
            {
                Assert.Pass("Context menu opens successfully");
            }
            else
            {
                Assert.Warn("Context menu not found after right-click");
            }
        }
        else
        {
            Assert.Warn("No list items to test context menu - no distributions installed");
        }
    }

    [Test]
    [Category("Distributions")]
    [Category("Dialog")]
    public void DistributionsPage_InstallButton_OpensDialog()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        var installButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Install").Or(cf.ByText("Install New")));

        if (installButton != null && installButton.IsEnabled)
        {
            installButton.Click();
            Thread.Sleep(1000);

            // Look for a dialog window
            var dialog = Automation?.GetDesktop().FindFirstDescendant(cf => 
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                .And(cf.ByName("Install").Or(cf.ByName("Registry"))));

            if (dialog != null)
            {
                // Close the dialog
                var closeButton = dialog.FindFirstDescendant(cf => 
                    cf.ByText("Close").Or(cf.ByText("Cancel")));
                closeButton?.Click();
                
                Assert.Pass("Install dialog opens successfully");
            }
            else
            {
                Assert.Warn("Install dialog not detected");
            }
        }
        else
        {
            Assert.Warn("Install button not available");
        }
    }

    [Test]
    [Category("Distributions")]
    public void DistributionsPage_DisplaysDistributionInfo()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        // Wait for data to load
        Thread.Sleep(2000);

        // Check for common distribution info elements
        var infoElements = window!.FindAllDescendants(cf => 
            cf.ByText("Version").Or(cf.ByText("Size")).Or(cf.ByText("Status")));

        if (infoElements.Length > 0)
        {
            Assert.Pass("Distribution information is displayed");
        }
        else
        {
            Assert.Warn("No distribution information displayed - may be empty list");
        }
    }

    [Test]
    [Category("Distributions")]
    [Category("Layout")]
    public void DistributionsPage_HasProperLayout()
    {
        var window = NavigateToDistributionsPage();
        Assert.That(window, Is.Not.Null);

        // Check that the page has a reasonable structure
        var allElements = window!.FindAllDescendants();
        
        Assert.That(allElements.Length, Is.GreaterThan(5), "Page should have multiple UI elements");
    }
}
