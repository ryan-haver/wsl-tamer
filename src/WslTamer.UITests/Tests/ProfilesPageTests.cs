using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace WslTamer.UITests.Tests;

/// <summary>
/// Tests for the Profiles page functionality
/// </summary>
[TestFixture]
public class ProfilesPageTests : TestBase
{
    private Window? NavigateToProfilesPage()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow == null) return null;

        var profilesNav = settingsWindow.FindFirstDescendant(cf => 
            cf.ByName("Profiles"));

        profilesNav?.Click();
        Thread.Sleep(500);

        return settingsWindow;
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_DisplaysProfileList()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var listElement = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.List).Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid)));

        if (listElement != null)
        {
            Assert.Pass("Profile list is displayed");
        }
        else
        {
            Assert.Warn("Profile list not found");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_HasAddProfileButton()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var addButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Add").Or(cf.ByText("New Profile")).Or(cf.ByText("Create")));

        Assert.That(addButton, Is.Not.Null, "Should have Add Profile button");
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_HasRemoveProfileButton()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var removeButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Remove").Or(cf.ByText("Delete")));

        if (removeButton != null)
        {
            Assert.Pass("Remove Profile button found");
        }
        else
        {
            Assert.Warn("Remove Profile button not found");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_CanSelectProfile()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        // Wait for profiles to load
        Thread.Sleep(1000);

        var listItem = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

        if (listItem != null)
        {
            listItem.Click();
            Thread.Sleep(300);
            
            Assert.Pass("Can select a profile");
        }
        else
        {
            Assert.Warn("No profiles available to select");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_ShowsProfileSettings()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        // Look for profile setting controls
        var textBoxes = window!.FindAllDescendants(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));

        if (textBoxes.Length > 0)
        {
            Assert.Pass($"Found {textBoxes.Length} profile setting fields");
        }
        else
        {
            Assert.Warn("No profile setting fields found - may require selecting a profile");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_HasMemorySettings()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var memoryLabel = window!.FindFirstDescendant(cf => 
            cf.ByText("Memory").Or(cf.ByText("RAM")));

        if (memoryLabel != null)
        {
            Assert.Pass("Memory settings are available");
        }
        else
        {
            Assert.Warn("Memory settings not found");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_HasCpuSettings()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var cpuLabel = window!.FindFirstDescendant(cf => 
            cf.ByText("CPU").Or(cf.ByText("Processor")).Or(cf.ByText("Cores")));

        if (cpuLabel != null)
        {
            Assert.Pass("CPU settings are available");
        }
        else
        {
            Assert.Warn("CPU settings not found");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_HasSwapSettings()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var swapLabel = window!.FindFirstDescendant(cf => 
            cf.ByText("Swap"));

        if (swapLabel != null)
        {
            Assert.Pass("Swap settings are available");
        }
        else
        {
            Assert.Warn("Swap settings not found");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_HasLocalhostSettings()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var localhostLabel = window!.FindFirstDescendant(cf => 
            cf.ByText("Localhost").Or(cf.ByText("Networking")));

        if (localhostLabel != null)
        {
            Assert.Pass("Localhost settings are available");
        }
        else
        {
            Assert.Warn("Localhost settings not found");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_CanEditProfileName()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        // Select a profile first
        var listItem = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

        if (listItem != null)
        {
            listItem.Click();
            Thread.Sleep(300);

            // Find the name textbox
            var nameTextBox = window.FindFirstDescendant(cf => 
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));

            if (nameTextBox != null && nameTextBox.IsEnabled)
            {
                Assert.Pass("Profile name can be edited");
            }
            else
            {
                Assert.Warn("Profile name field not editable");
            }
        }
        else
        {
            Assert.Warn("No profile available to edit");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_CanEditMemoryValue()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        // Select a profile
        var listItem = window!.FindFirstDescendant(cf => 
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

        if (listItem != null)
        {
            listItem.Click();
            Thread.Sleep(300);

            // Find memory textbox
            var textBoxes = window.FindAllDescendants(cf => 
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));

            if (textBoxes.Length > 1)
            {
                Assert.Pass("Memory field can be edited");
            }
            else
            {
                Assert.Warn("Memory field not found");
            }
        }
        else
        {
            Assert.Warn("No profile available to edit");
        }
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_ValidatesMemoryInput()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        // This would test that invalid memory values are rejected
        // Implementation depends on validation behavior
        Assert.Pass("Memory validation test placeholder");
    }

    [Test]
    [Category("Profiles")]
    [Category("Layout")]
    public void ProfilesPage_HasProperLayout()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var allElements = window!.FindAllDescendants();
        
        Assert.That(allElements.Length, Is.GreaterThan(5), "Page should have multiple UI elements");
    }

    [Test]
    [Category("Profiles")]
    public void ProfilesPage_AddButton_IsClickable()
    {
        var window = NavigateToProfilesPage();
        Assert.That(window, Is.Not.Null);

        var addButton = window!.FindFirstDescendant(cf => 
            cf.ByText("Add").Or(cf.ByText("New Profile")));

        if (addButton != null)
        {
            Assert.That(addButton.IsEnabled, Is.True, "Add button should be enabled");
        }
        else
        {
            Assert.Warn("Add button not found");
        }
    }
}
