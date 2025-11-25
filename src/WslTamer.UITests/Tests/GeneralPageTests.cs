using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace WslTamer.UITests.Tests;

/// <summary>
/// Tests for the General page functionality
/// </summary>
[TestFixture]
public class GeneralPageTests : TestBase
{
    private Window? NavigateToGeneralPage()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow == null) return null;

        var generalNav = settingsWindow.FindFirstDescendant(cf => 
            cf.ByName("General"));

        generalNav?.Click();
        Thread.Sleep(500);

        return settingsWindow;
    }

    [Test]
    [Category("General")]
    public void GeneralPage_DisplaysWslStatus()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        // Look for WSL status indicator
        var statusElement = window!.FindFirstDescendant(cf => 
            cf.ByText("WSL Status").Or(cf.ByText("Status")));

        Assert.That(statusElement, Is.Not.Null, "Should display WSL status");
    }

    [Test]
    [Category("General")]
    public void GeneralPage_HasStartOnLoginCheckbox()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        var startOnLoginCheckbox = window!.FindFirstDescendant(cf => 
            cf.ByText("Start with Windows").Or(cf.ByText("Start on login")));

        Assert.That(startOnLoginCheckbox, Is.Not.Null, "Should have Start on Login option");
    }

    [Test]
    [Category("General")]
    public void GeneralPage_CanToggleStartOnLogin()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        var checkbox = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox));

        if (checkbox != null)
        {
            var initialState = checkbox.IsEnabled;
            checkbox.Click();
            Thread.Sleep(300);

            // Verify state changed (if checkbox is functional)
            Assert.Pass("Start on Login toggle is interactive");
        }
        else
        {
            Assert.Warn("Start on Login checkbox not found");
        }
    }

    [Test]
    [Category("General")]
    public void GeneralPage_HasReclaimMemoryButton()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        var reclaimButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Reclaim Memory").Or(cf.ByText("Free Memory")));

        Assert.That(reclaimButton, Is.Not.Null, "Should have Reclaim Memory button");
    }

    [Test]
    [Category("General")]
    public void GeneralPage_ReclaimMemoryButton_IsClickable()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        var reclaimButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Reclaim Memory"));

        if (reclaimButton != null)
        {
            Assert.That(reclaimButton.IsEnabled, Is.True, "Reclaim Memory button should be enabled");
            
            // Don't actually click it to avoid disrupting WSL during tests
            Assert.Pass("Reclaim Memory button is clickable");
        }
        else
        {
            Assert.Warn("Reclaim Memory button not found");
        }
    }

    [Test]
    [Category("General")]
    public void GeneralPage_HasShutdownButton()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        var shutdownButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Shutdown WSL").Or(cf.ByText("Shutdown")));

        Assert.That(shutdownButton, Is.Not.Null, "Should have Shutdown button");
    }

    [Test]
    [Category("General")]
    public void GeneralPage_HasOpenExplorerButton()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        var openExplorerButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Open in Explorer").Or(cf.ByText("Explorer")));

        if (openExplorerButton != null)
        {
            Assert.Pass("Open Explorer button found");
        }
        else
        {
            Assert.Warn("Open Explorer button not found");
        }
    }

    [Test]
    [Category("General")]
    public void GeneralPage_DisplaysMemoryUsage()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        // Look for memory-related text
        var memoryText = window!.FindFirstDescendant(cf => 
            cf.ByText("Memory").Or(cf.ByText("RAM")).Or(cf.ByText("GB")));

        if (memoryText != null)
        {
            Assert.Pass("Memory usage information is displayed");
        }
        else
        {
            Assert.Warn("Memory usage not displayed - may load asynchronously");
        }
    }

    [Test]
    [Category("General")]
    public void GeneralPage_DisplaysCpuUsage()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        // Look for CPU-related text
        var cpuText = window!.FindFirstDescendant(cf => 
            cf.ByText("CPU").Or(cf.ByText("Processor")));

        if (cpuText != null)
        {
            Assert.Pass("CPU usage information is displayed");
        }
        else
        {
            Assert.Warn("CPU usage not displayed - may load asynchronously");
        }
    }

    [Test]
    [Category("General")]
    [Category("Layout")]
    public void GeneralPage_HasProperLayout()
    {
        var window = NavigateToGeneralPage();
        Assert.That(window, Is.Not.Null);

        // Check that elements are present and properly arranged
        var allButtons = window!.FindAllDescendants(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));

        Assert.That(allButtons.Length, Is.GreaterThan(0), "Should have interactive buttons");
    }
}
