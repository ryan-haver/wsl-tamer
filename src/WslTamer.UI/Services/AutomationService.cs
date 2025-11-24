using System.Diagnostics;
using System.Windows.Threading;
using WslTamer.UI.Models;
using System.Windows.Forms; // For SystemInformation

namespace WslTamer.UI.Services;

public class AutomationService
{
    private readonly ProfileManager _profileManager;
    private readonly WslService _wslService;
    private readonly DispatcherTimer _timer;
    private Guid? _lastAppliedProfileId;

    public AutomationService(ProfileManager profileManager, WslService wslService)
    {
        _profileManager = profileManager;
        _wslService = wslService;
        
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(10); // Check every 10 seconds
        _timer.Tick += CheckRules;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void CheckRules(object? sender, EventArgs e)
    {
        var rules = _profileManager.GetRules().Where(r => r.IsEnabled).ToList();
        
        foreach (var rule in rules)
        {
            if (EvaluateRule(rule))
            {
                // If rule matches and we haven't already applied this profile recently
                // (Simple logic: if multiple rules match, first one wins. 
                // Ideally we need a priority system or state machine)
                
                if (_lastAppliedProfileId != rule.TargetProfileId)
                {
                    var profile = _profileManager.GetProfile(rule.TargetProfileId);
                    if (profile != null)
                    {
                        // Notify user?
                        // Apply profile
                        _wslService.ApplyProfile(profile);
                        _lastAppliedProfileId = rule.TargetProfileId;
                        
                        // Stop checking other rules to avoid flapping
                        return;
                    }
                }
                // If we already applied it, we still return to "hold" this state
                return;
            }
        }
    }

    private bool EvaluateRule(AutomationRule rule)
    {
        switch (rule.TriggerType)
        {
            case TriggerType.Process:
                return IsProcessRunning(rule.TriggerValue);
            case TriggerType.Network:
                return CheckNetwork(rule.TriggerValue);
            case TriggerType.PowerState:
                // Deprecated/Hidden in UI but kept for compatibility
                return CheckPowerState(rule.TriggerValue);
            case TriggerType.Time:
                // Not implemented yet
                return false;
            default:
                return false;
        }
    }

    private bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        
        // Strip .exe if present for flexibility
        var name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
            ? processName.Substring(0, processName.Length - 4) 
            : processName;

        return Process.GetProcessesByName(name).Length > 0;
    }

    private bool CheckPowerState(string state)
    {
        var powerStatus = SystemInformation.PowerStatus;
        if (string.Equals(state, "OnBattery", StringComparison.OrdinalIgnoreCase))
        {
            return powerStatus.PowerLineStatus == PowerLineStatus.Offline;
        }
        else if (string.Equals(state, "PluggedIn", StringComparison.OrdinalIgnoreCase))
        {
            return powerStatus.PowerLineStatus == PowerLineStatus.Online;
        }
        return false;
    }

    private bool CheckNetwork(string targetSsid)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("SSID") && trimmed.Contains(":"))
                {
                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var currentSsid = parts[1].Trim();
                        if (string.Equals(currentSsid, targetSsid, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch { }
        return false;
    }
}
