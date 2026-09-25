namespace WslTamer.Core.Config;

/// <summary>
/// Documented per-distribution settings for /etc/wsl.conf.
/// Source: https://learn.microsoft.com/windows/wsl/wsl-config
/// </summary>
public static class WslConfCatalog
{
    public const string FilePath = "/etc/wsl.conf";

    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        new() { Group = "Startup", Section = "boot", Key = "systemd", Kind = SettingKind.Boolean, Label = "systemd", Description = "Use systemd as the init system.", DefaultText = "false" },
        new() { Group = "Startup", Section = "boot", Key = "command", Kind = SettingKind.Text, Label = "Boot command", Description = "Command run as root when the distribution starts, e.g. service docker start." },
        new() { Group = "Startup", Section = "user", Key = "default", Kind = SettingKind.Text, Label = "Default user", Description = "User that sessions start as.", DefaultText = "The user created at install" },

        new() { Group = "Windows drives", Section = "automount", Key = "enabled", Kind = SettingKind.Boolean, Label = "Mount Windows drives", Description = "Mount fixed drives such as C: under the mount root.", DefaultText = "true" },
        new() { Group = "Windows drives", Section = "automount", Key = "root", Kind = SettingKind.Text, Label = "Mount root", Description = "Folder that Windows drives are mounted under.", DefaultText = "/mnt/" },
        new() { Group = "Windows drives", Section = "automount", Key = "options", Kind = SettingKind.Text, Label = "Mount options", Description = "DrvFs options, e.g. metadata,uid=1000,gid=1000,umask=022." },
        new() { Group = "Windows drives", Section = "automount", Key = "mountFsTab", Kind = SettingKind.Boolean, Label = "Process /etc/fstab", Description = "Mount file systems listed in /etc/fstab at startup.", DefaultText = "true" },

        new() { Group = "Network", Section = "network", Key = "hostname", Kind = SettingKind.Text, Label = "Hostname", Description = "Hostname for this distribution.", DefaultText = "Windows hostname" },
        new() { Group = "Network", Section = "network", Key = "generateHosts", Kind = SettingKind.Boolean, Label = "Generate /etc/hosts", Description = "Let WSL manage /etc/hosts.", DefaultText = "true" },
        new() { Group = "Network", Section = "network", Key = "generateResolvConf", Kind = SettingKind.Boolean, Label = "Generate /etc/resolv.conf", Description = "Let WSL manage DNS configuration.", DefaultText = "true" },

        new() { Group = "Windows interop", Section = "interop", Key = "enabled", Kind = SettingKind.Boolean, Label = "Run Windows programs", Description = "Allow launching Windows executables from Linux.", DefaultText = "true" },
        new() { Group = "Windows interop", Section = "interop", Key = "appendWindowsPath", Kind = SettingKind.Boolean, Label = "Add Windows PATH", Description = "Append Windows PATH entries to $PATH.", DefaultText = "true" },

        new() { Group = "Other", Section = "gpu", Key = "enabled", Kind = SettingKind.Boolean, Label = "GPU access", Description = "Allow Linux apps to use the Windows GPU.", DefaultText = "true" },
        new() { Group = "Other", Section = "time", Key = "useWindowsTimezone", Kind = SettingKind.Boolean, Label = "Use Windows time zone", Description = "Sync the time zone with Windows.", DefaultText = "true" },
    ];

    public static SettingDefinition? Find(string section, string key) =>
        All.FirstOrDefault(s =>
            string.Equals(s.Section, section, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
}
