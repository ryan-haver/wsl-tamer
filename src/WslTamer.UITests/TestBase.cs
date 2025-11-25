using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System.Diagnostics;
using System.IO;

namespace WslTamer.UITests;

/// <summary>
/// Base class for all UI tests providing common functionality for app lifecycle and interaction
/// </summary>
public abstract class TestBase
{
    protected Application? App;
    protected UIA3Automation? Automation;
    protected Window? MainWindow;
    protected string AppPath = string.Empty;
    protected string TestRunId = string.Empty;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestRunId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        // Find the built application
        var solutionDir = FindSolutionDirectory();
        AppPath = Path.Combine(solutionDir, "src", "WslTamer.UI", "bin", "Debug", "net8.0-windows", "WslTamer.UI.exe");
        
        if (!File.Exists(AppPath))
        {
            Assert.Fail($"Application not found at: {AppPath}. Please build the solution first.");
        }
    }

    [SetUp]
    public void SetUp()
    {
        // Kill any existing instances
        KillExistingInstances();
        
        // Initialize automation
        Automation = new UIA3Automation();
        
        // Start the application
        App = Application.Launch(AppPath);
        
        // Wait for application to be ready
        Thread.Sleep(2000);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            // Take screenshot on failure
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                TakeScreenshot(TestContext.CurrentContext.Test.Name);
            }
        }
        finally
        {
            // Close application
            App?.Close();
            App?.Dispose();
            
            // Dispose automation
            Automation?.Dispose();
            
            // Kill any remaining processes
            KillExistingInstances();
        }
    }

    protected Window? GetSettingsWindow()
    {
        if (Automation == null) return null;
        
        // Settings window should be the main window when opened
        var windows = Automation.GetDesktop().FindAllChildren(cf => 
            cf.ByClassName("Window").And(cf.ByName("Settings")));
        
        return windows.FirstOrDefault()?.AsWindow();
    }

    protected void TakeScreenshot(string testName)
    {
        try
        {
            var screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            var screenshotPath = Path.Combine(screenshotDir, $"{testName}_{TestRunId}.png");
            
            using var screenshot = Capture.Screen();
            screenshot.ToFile(screenshotPath);
            
            TestContext.WriteLine($"Screenshot saved: {screenshotPath}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to take screenshot: {ex.Message}");
        }
    }

    protected void KillExistingInstances()
    {
        try
        {
            var processes = Process.GetProcessesByName("WslTamer.UI");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
            }
        }
        catch { }
    }

    protected string FindSolutionDirectory()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(currentDir);
        
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Any())
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        
        throw new InvalidOperationException("Solution directory not found");
    }

    protected AutomationElement? FindElementByText(AutomationElement parent, params string[] texts)
    {
        foreach (var text in texts)
        {
            var element = parent.FindFirstDescendant(cf => cf.ByText(text));
            if (element != null) return element;
        }
        return null;
    }

    protected AutomationElement? FindElementByName(AutomationElement parent, params string[] names)
    {
        foreach (var name in names)
        {
            var element = parent.FindFirstDescendant(cf => cf.ByName(name));
            if (element != null) return element;
        }
        return null;
    }

    protected void WaitForElement(Func<AutomationElement?> findElement, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var element = findElement();
            if (element != null)
            {
                return;
            }
            Thread.Sleep(100);
        }
        
        Assert.Fail($"Element not found within {timeoutMs}ms");
    }

    protected void ClickTrayIcon()
    {
        if (Automation == null) return;
        
        // Find the notification area
        var desktop = Automation.GetDesktop();
        var notificationArea = desktop.FindFirstDescendant(cf => 
            cf.ByClassName("Shell_TrayWnd"));
        
        if (notificationArea != null)
        {
            // Click on notification area to open tray icons
            notificationArea.Click();
            Thread.Sleep(500);
        }
    }

    protected void OpenSettingsViaRegistry()
    {
        // For testing, we can directly instantiate the settings window
        // This avoids the complexity of interacting with the system tray
    }
}
