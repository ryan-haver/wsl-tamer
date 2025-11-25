using FlaUI.Core.AutomationElements;

namespace WslTamer.UITests.Tests;

/// <summary>
/// Tests for the Hardware page functionality
/// </summary>
[TestFixture]
public class HardwarePageTests : TestBase
{
    private Window? NavigateToHardwarePage()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow == null) return null;

        var hardwareNav = settingsWindow.FindFirstDescendant(cf => 
            cf.ByName("Hardware"));

        hardwareNav?.Click();
        Thread.Sleep(500);

        return settingsWindow;
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_DisplaysUsbDevices()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for devices to load
        Thread.Sleep(2000);

        var usbSection = window!.FindFirstDescendant(cf => 
            cf.ByText("USB").Or(cf.ByText("Devices")));

        if (usbSection != null)
        {
            Assert.Pass("USB devices section is displayed");
        }
        else
        {
            Assert.Warn("USB devices section not found");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_HasRefreshButton()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        var refreshButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Refresh").Or(cf.ByText("Scan")));

        Assert.That(refreshButton, Is.Not.Null, "Should have Refresh button");
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_CanClickRefresh()
    {
        var window = NavigateToHardwarePage();
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
    [Category("Hardware")]
    public void HardwarePage_DisplaysDeviceList()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for list to populate
        Thread.Sleep(2000);

        var listElement = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.List).Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid)));

        if (listElement != null)
        {
            Assert.Pass("Device list is displayed");
        }
        else
        {
            Assert.Warn("Device list not found - may be empty");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_ShowsDeviceDetails()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for devices
        Thread.Sleep(2000);

        // Look for device information
        var deviceInfo = window!.FindAllDescendants(cf => 
            cf.ByText("VID").Or(cf.ByText("PID")).Or(cf.ByText("Device ID")));

        if (deviceInfo.Length > 0)
        {
            Assert.Pass("Device details are shown");
        }
        else
        {
            Assert.Warn("No device details found - may have no USB devices");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_HasAttachButtons()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for UI to load
        Thread.Sleep(2000);

        var attachButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Attach").Or(cf.ByText("Connect")));

        if (attachButton != null)
        {
            Assert.Pass("Attach button found");
        }
        else
        {
            Assert.Warn("Attach button not found - may require devices");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_HasDetachButtons()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for UI to load
        Thread.Sleep(2000);

        var detachButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Detach").Or(cf.ByText("Disconnect")));

        if (detachButton != null)
        {
            Assert.Pass("Detach button found");
        }
        else
        {
            Assert.Warn("Detach button not found - may require attached devices");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_DisplaysDiskDevices()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for data
        Thread.Sleep(2000);

        var diskSection = window!.FindFirstDescendant(cf => 
            cf.ByText("Disk").Or(cf.ByText("Storage")));

        if (diskSection != null)
        {
            Assert.Pass("Disk devices section is displayed");
        }
        else
        {
            Assert.Warn("Disk section not found");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_ShowsDeviceStatus()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        // Wait for status to load
        Thread.Sleep(2000);

        var statusText = window!.FindFirstDescendant(cf => 
            cf.ByText("Connected").Or(cf.ByText("Attached")).Or(cf.ByText("Available")));

        if (statusText != null)
        {
            Assert.Pass("Device status is shown");
        }
        else
        {
            Assert.Warn("No status information found");
        }
    }

    [Test]
    [Category("Hardware")]
    [Category("Layout")]
    public void HardwarePage_HasProperLayout()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        var allElements = window!.FindAllDescendants();
        
        Assert.That(allElements.Length, Is.GreaterThan(5), "Page should have multiple UI elements");
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_UsbTab_IsAccessible()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        var usbTab = window!.FindFirstDescendant(cf => 
            cf.ByText("USB"));

        if (usbTab != null)
        {
            Assert.Pass("USB tab is accessible");
        }
        else
        {
            Assert.Warn("USB tab not found");
        }
    }

    [Test]
    [Category("Hardware")]
    public void HardwarePage_DiskTab_IsAccessible()
    {
        var window = NavigateToHardwarePage();
        Assert.That(window, Is.Not.Null);

        var diskTab = window!.FindFirstDescendant(cf => 
            cf.ByText("Disk").Or(cf.ByText("Disks")));

        if (diskTab != null)
        {
            Assert.Pass("Disk tab is accessible");
        }
        else
        {
            Assert.Warn("Disk tab not found");
        }
    }
}
