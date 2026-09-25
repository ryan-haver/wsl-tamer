using System.Text;
using WslTamer.Core.Config;
using WslTamer.Core.Disks;

namespace WslTamer.Core.Tests;

public class SettingsTests
{
    [Fact]
    public void Catalog_ids_are_unique()
    {
        Assert.Equal(WslConfigCatalog.All.Count, WslConfigCatalog.All.Select(s => s.Id.ToLowerInvariant()).Distinct().Count());
        Assert.Equal(WslConfCatalog.All.Count, WslConfCatalog.All.Select(s => s.Id.ToLowerInvariant()).Distinct().Count());
    }

    [Fact]
    public void Choice_values_are_normalized_to_documented_spelling()
    {
        var mode = WslConfigCatalog.Find("wsl2", "networkingMode")!;
        Assert.Equal("mirrored", mode.ToFileValue("MIRRORED"));

        var reclaim = WslConfigCatalog.Find("experimental", "autoMemoryReclaim")!;
        Assert.Equal("dropCache", reclaim.ToFileValue("dropcache"));
    }

    [Fact]
    public void Booleans_are_written_lowercase()
    {
        var setting = WslConfigCatalog.Find("wsl2", "guiApplications")!;
        Assert.Equal("false", setting.ToFileValue("False"));
        Assert.Equal("Use true or false.", setting.Validate("maybe"));
    }

    [Fact]
    public void Path_escaping_is_idempotent()
    {
        Assert.Equal(@"C:\\a\\b", WslValues.EscapeWindowsPath(@"C:\a\b"));
        Assert.Equal(@"C:\\a\\b", WslValues.EscapeWindowsPath(@"C:\\a\\b"));
        Assert.Equal(@"C:\a\b", WslConfigCatalog.Find("wsl2", "swapFile")!.FromFileValue(@"C:\\a\\b"));
    }

    [Fact]
    public void Quoted_wsl_conf_values_are_unquoted_for_display()
    {
        var options = WslConfCatalog.Find("automount", "options")!;
        Assert.Equal("metadata,uid=1000", options.FromFileValue("\"metadata,uid=1000\""));
    }

    [Fact]
    public void Compact_script_embeds_path_as_single_quoted_literal()
    {
        var encoded = VhdService.BuildCompactCommand(@"C:\Users\O'Brien\wsl\ext4.vhdx");
        var script = Encoding.Unicode.GetString(Convert.FromBase64String(encoded));

        Assert.Contains(@"$vhd = 'C:\Users\O''Brien\wsl\ext4.vhdx'", script, StringComparison.Ordinal);
        Assert.Contains("compact vdisk", script, StringComparison.Ordinal);
        Assert.Contains("attach vdisk readonly", script, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => VhdService.BuildCompactCommand("C:\\bad\"path.vhdx"));
    }
}
