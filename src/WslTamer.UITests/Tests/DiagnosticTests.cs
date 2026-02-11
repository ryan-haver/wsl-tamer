using FlaUI.Core.AutomationElements;
using NUnit.Framework;

namespace WslTamer.UITests.Tests;

[TestFixture]
public class DiagnosticTests : TestBase
{
    [Test]
    [Category("Diagnostic")]
    public void ListAllWindows()
    {
        Assert.That(Automation, Is.Not.Null, "Automation should be initialized");
        
        var desktop = Automation!.GetDesktop();
        var allWindows = desktop.FindAllChildren(cf => cf.ByClassName("Window"));
        
        Console.WriteLine($"Found {allWindows.Length} windows:");
        foreach (var window in allWindows)
        {
            Console.WriteLine($"  - Name: '{window.Name}', ClassName: '{window.ClassName}', ProcessId: {window.Properties.ProcessId}");
        }
        
        var settingsWindow = GetSettingsWindow();
        Console.WriteLine($"\nGetSettingsWindow returned: {(settingsWindow != null ? $"Window '{settingsWindow.Name}'" : "null")}");
        
        Assert.That(settingsWindow, Is.Not.Null, "Settings window should be found");
    }
}
