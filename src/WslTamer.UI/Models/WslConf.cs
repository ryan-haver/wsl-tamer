namespace WslTamer.UI.Models;

public class WslConf
{
    public WslConfBoot Boot { get; set; } = new();
    public WslConfAutomount Automount { get; set; } = new();
    public WslConfNetwork Network { get; set; } = new();
    public WslConfInterop Interop { get; set; } = new();
    public WslConfUser User { get; set; } = new();
}

public class WslConfBoot
{
    public bool? Systemd { get; set; }
    public string? Command { get; set; }
}

public class WslConfAutomount
{
    public bool? Enabled { get; set; }
    public bool? MountFsTab { get; set; }
    public string? Root { get; set; }
    public string? Options { get; set; }
}

public class WslConfNetwork
{
    public bool? GenerateHosts { get; set; }
    public bool? GenerateResolvConf { get; set; }
    public string? Hostname { get; set; }
}

public class WslConfInterop
{
    public bool? Enabled { get; set; }
    public bool? AppendWindowsPath { get; set; }
}

public class WslConfUser
{
    public string? Default { get; set; }
}
