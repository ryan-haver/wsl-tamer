using System;

namespace WslTamer.UI.Models;

public class WslProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Profile";
    public string Memory { get; set; } = "4GB"; // e.g., 4GB, 512MB
    public int Processors { get; set; } = 2;
    public string Swap { get; set; } = "0"; // 0 to disable, or size like 8GB
    public bool LocalhostForwarding { get; set; } = true;

    // Advanced Global Settings (.wslconfig)
    public string KernelPath { get; set; } = string.Empty; // Path to custom kernel
    public string NetworkingMode { get; set; } = "NAT"; // NAT, Mirrored, Bridged
    public bool GuiApplications { get; set; } = true; // Enable WSLg
    public bool DebugConsole { get; set; } = false; // Show debug console
}

public enum TriggerType
{
    Time,
    Process,
    PowerState,
    Network
}

public enum PowerStateTrigger
{
    OnBattery,
    PluggedIn
}

public class AutomationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Rule";
    public bool IsEnabled { get; set; } = true;
    public TriggerType TriggerType { get; set; }
    
    // For Time trigger: "HH:mm"
    // For Process trigger: "process_name.exe"
    // For PowerState trigger: "OnBattery" or "PluggedIn"
    public string TriggerValue { get; set; } = string.Empty;
    
    public Guid TargetProfileId { get; set; }
}

public class AppConfig
{
    public List<WslProfile> Profiles { get; set; } = new();
    public List<AutomationRule> Rules { get; set; } = new();
    public Guid? CurrentProfileId { get; set; }
    public Guid? DefaultProfileId { get; set; }
}
