namespace WslTamer.Core.Config;

/// <summary>
/// Documented global settings for %UserProfile%\.wslconfig.
/// Source: https://learn.microsoft.com/windows/wsl/wsl-config
/// </summary>
public static class WslConfigCatalog
{
    public const string Wsl2 = "wsl2";
    public const string General = "general";
    public const string Experimental = "experimental";

    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        // Resources
        new() { Group = "Resources", Section = Wsl2, Key = "memory", Kind = SettingKind.Size, Label = "Memory limit", Description = "Maximum memory for the WSL 2 VM, e.g. 8GB.", DefaultText = "50% of Windows memory", ProfileCandidate = true },
        new() { Group = "Resources", Section = Wsl2, Key = "processors", Kind = SettingKind.Integer, Minimum = 1, Label = "Processors", Description = "Number of logical processors for the VM.", DefaultText = "All logical processors", ProfileCandidate = true },
        new() { Group = "Resources", Section = Wsl2, Key = "swap", Kind = SettingKind.Size, Label = "Swap size", Description = "Swap space for the VM. 0 disables swap.", DefaultText = "25% of Windows memory", ProfileCandidate = true },
        new() { Group = "Resources", Section = Wsl2, Key = "swapFile", Kind = SettingKind.WindowsPath, Label = "Swap file", Description = "Full Windows path of the swap VHD.", DefaultText = @"%Temp%\swap.vhdx" },
        new() { Group = "Resources", Section = Experimental, Key = "autoMemoryReclaim", Kind = SettingKind.Choice, Choices = ["disabled", "gradual", "dropCache"], Label = "Automatic memory reclaim", Description = "How WSL returns cached memory to Windows.", DefaultText = "dropCache", ProfileCandidate = true },
        new() { Group = "Resources", Section = Wsl2, Key = "vmIdleTimeout", Kind = SettingKind.Integer, Minimum = -1, Label = "VM idle timeout (ms)", Description = "Milliseconds the VM stays up with nothing running before it shuts down.", DefaultText = "60000", RequiresWindows11 = true, ProfileCandidate = true },
        new() { Group = "Resources", Section = General, Key = "instanceIdleTimeout", Kind = SettingKind.Integer, Minimum = -1, Label = "Distribution idle timeout (ms)", Description = "Milliseconds an idle distribution keeps running. -1 never stops it.", DefaultText = "15000", ProfileCandidate = true },

        // Networking
        new() { Group = "Networking", Section = Wsl2, Key = "networkingMode", Kind = SettingKind.Choice, Choices = ["nat", "mirrored", "none", "consomme"], Label = "Networking mode", Description = "NAT is the default. Mirrored shares Windows' network interfaces.", DefaultText = "nat", RequiresWindows11 = true, ProfileCandidate = true },
        new() { Group = "Networking", Section = Wsl2, Key = "localhostForwarding", Kind = SettingKind.Boolean, Label = "Localhost forwarding", Description = "Reach Linux services from Windows on localhost (NAT mode).", DefaultText = "true", ProfileCandidate = true },
        new() { Group = "Networking", Section = Wsl2, Key = "dnsTunneling", Kind = SettingKind.Boolean, Label = "DNS tunneling", Description = "Proxy DNS requests through Windows. Helps with many VPNs.", DefaultText = "true", RequiresWindows11 = true, ProfileCandidate = true },
        new() { Group = "Networking", Section = Wsl2, Key = "firewall", Kind = SettingKind.Boolean, Label = "Windows Firewall", Description = "Apply Windows Firewall and Hyper-V rules to WSL traffic.", DefaultText = "true", RequiresWindows11 = true, ProfileCandidate = true },
        new() { Group = "Networking", Section = Wsl2, Key = "autoProxy", Kind = SettingKind.Boolean, Label = "Use Windows proxy", Description = "Use Windows' HTTP proxy settings inside WSL.", DefaultText = "true", RequiresWindows11 = true, ProfileCandidate = true },
        new() { Group = "Networking", Section = Wsl2, Key = "dnsProxy", Kind = SettingKind.Boolean, Label = "DNS proxy (NAT)", Description = "In NAT mode, use the host NAT as the Linux DNS server.", DefaultText = "true" },
        new() { Group = "Networking", Section = Experimental, Key = "hostAddressLoopback", Kind = SettingKind.Boolean, Label = "Host address loopback", Description = "Mirrored mode: connect between Windows and Linux using the host's IP addresses.", DefaultText = "false", RequiresWindows11 = true },
        new() { Group = "Networking", Section = Experimental, Key = "ignoredPorts", Kind = SettingKind.Text, Label = "Ignored ports", Description = "Mirrored mode: comma-separated ports Linux may bind even if Windows uses them.", RequiresWindows11 = true },

        // Features
        new() { Group = "Features", Section = Wsl2, Key = "guiApplications", Kind = SettingKind.Boolean, Label = "GUI apps (WSLg)", Description = "Run graphical Linux apps.", DefaultText = "true", ProfileCandidate = true },
        new() { Group = "Features", Section = Wsl2, Key = "gpuSupport", Kind = SettingKind.Boolean, Label = "GPU support", Description = "Give the VM access to the Windows GPU.", DefaultText = "true", ProfileCandidate = true },
        new() { Group = "Features", Section = Wsl2, Key = "nestedVirtualization", Kind = SettingKind.Boolean, Label = "Nested virtualization", Description = "Allow VMs to run inside WSL 2.", DefaultText = "true", RequiresWindows11 = true, ProfileCandidate = true },
        new() { Group = "Features", Section = Wsl2, Key = "debugConsole", Kind = SettingKind.Boolean, Label = "Debug console", Description = "Open a console showing kernel messages when WSL starts.", DefaultText = "false", RequiresWindows11 = true },
        new() { Group = "Features", Section = Wsl2, Key = "safeMode", Kind = SettingKind.Boolean, Label = "Safe mode", Description = "Disable many features to recover a broken distribution.", DefaultText = "false" },

        // Disks and kernel
        new() { Group = "Disks and kernel", Section = Experimental, Key = "sparseVhd", Kind = SettingKind.Boolean, Label = "Sparse disks for new distributions", Description = "New distribution disks shrink automatically as files are deleted.", DefaultText = "false" },
        new() { Group = "Disks and kernel", Section = Wsl2, Key = "defaultVhdSize", Kind = SettingKind.Size, Label = "Default disk size", Description = "Maximum size for new distribution disks.", DefaultText = "1TB" },
        new() { Group = "Disks and kernel", Section = General, Key = "distributionInstallPath", Kind = SettingKind.WindowsPath, Label = "Install location", Description = "Default folder for newly installed distributions.", DefaultText = @"%LocalAppData%\wsl" },
        new() { Group = "Disks and kernel", Section = Wsl2, Key = "kernel", Kind = SettingKind.WindowsPath, Label = "Custom kernel", Description = "Full Windows path to a custom Linux kernel.", DefaultText = "Microsoft kernel" },
        new() { Group = "Disks and kernel", Section = Wsl2, Key = "kernelModules", Kind = SettingKind.WindowsPath, Label = "Custom kernel modules", Description = "Full Windows path to a kernel modules VHD." },
        new() { Group = "Disks and kernel", Section = Wsl2, Key = "kernelCommandLine", Kind = SettingKind.Text, Label = "Kernel command line", Description = "Extra kernel arguments, e.g. vsyscall=emulate." },
        new() { Group = "Disks and kernel", Section = Wsl2, Key = "maxCrashDumpCount", Kind = SettingKind.Integer, Minimum = 0, Label = "Crash dumps to keep", Description = "Older crash dumps beyond this number are deleted.", DefaultText = "10" },
    ];

    public static IReadOnlyList<SettingDefinition> ProfileSettings { get; } = All.Where(s => s.ProfileCandidate).ToList();

    public static SettingDefinition? Find(string section, string key) =>
        All.FirstOrDefault(s =>
            string.Equals(s.Section, section, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

    public static SettingDefinition? FindById(string id)
    {
        int dot = id.IndexOf('.', StringComparison.Ordinal);
        return dot <= 0 ? null : Find(id[..dot], id[(dot + 1)..]);
    }
}
