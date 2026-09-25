using System.Text;
using WslTamer.Core.Hardware;
using WslTamer.Core.Processes;
using WslTamer.Core.Tests.Support;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests;

public class ParserTests
{
    [Fact]
    public void Online_list_skips_preamble_and_header()
    {
        var list = WslOutputParser.ParseOnlineList(Fixture.Read("wsl-list-online.txt"));

        Assert.DoesNotContain(list, d => d.Name is "NAME" or "Install" or "The");
        Assert.Contains(list, d => d.Name == "Ubuntu-24.04" && d.FriendlyName == "Ubuntu 24.04 LTS");
        Assert.Contains(list, d => d.Name == "SUSE-Linux-Enterprise-15-SP7" && d.FriendlyName == "SUSE Linux Enterprise 15 SP7");
        Assert.Equal("Ubuntu", list[0].Name);
        Assert.All(list, d => Assert.True(DistroNames.IsValid(d.Name)));
    }

    [Fact]
    public void Online_list_does_not_depend_on_english_text()
    {
        const string german = """
            Nachfolgend finden Sie eine Liste der gültigen Distributionen, die installiert werden können.
            Installieren Sie mithilfe von "wsl.exe --install <Distro>".

            NAME                            ANZEIGENAME
            Ubuntu                          Ubuntu
            Debian                          Debian GNU/Linux
            """;

        var list = WslOutputParser.ParseOnlineList(german);

        Assert.Equal(["Ubuntu", "Debian"], list.Select(d => d.Name));
    }

    [Fact]
    public void Version_is_read_by_position()
    {
        var info = WslOutputParser.ParseVersion(Fixture.Read("wsl-version.txt"));

        Assert.NotNull(info);
        Assert.Matches(@"^\d+\.\d+\.\d+", info.WslVersion);
        Assert.Matches(@"^\d+\.\d+", info.KernelVersion);
        Assert.StartsWith("10.0.", info.WindowsVersion, StringComparison.Ordinal);
    }

    [Fact]
    public void Version_returns_null_for_old_inbox_wsl_help_text()
    {
        Assert.Null(WslOutputParser.ParseVersion("Invalid command line option: --version\nUsage: wsl.exe [Argument]"));
    }

    [Fact]
    public void Name_list_handles_crlf_and_blank_lines()
    {
        Assert.Equal(["Ubuntu", "docker-desktop"], WslOutputParser.ParseNameList("Ubuntu\r\n\r\ndocker-desktop\r\n"));
        Assert.Empty(WslOutputParser.ParseNameList(string.Empty));
    }

    [Fact]
    public void Extracts_error_code_and_message()
    {
        const string output = "Invalid command line argument: --compact\r\nPlease use 'wsl.exe --help' to get a list of supported arguments.\r\nError code: Wsl/E_INVALIDARG\r\n";

        Assert.Equal("Wsl/E_INVALIDARG", WslOutputParser.ExtractErrorCode(output));
        Assert.Equal("Invalid command line argument: --compact Please use 'wsl.exe --help' to get a list of supported arguments.", WslOutputParser.ExtractErrorMessage(output));
    }

    [Theory]
    [InlineData("Ubuntu-24.04", true)]
    [InlineData("my_distro.2", true)]
    [InlineData("", false)]
    [InlineData("has space", false)]
    [InlineData("semi;colon", false)]
    [InlineData("$(id)", false)]
    public void Validates_distro_names(string name, bool valid) => Assert.Equal(valid, DistroNames.IsValid(name));

    [Fact]
    public void Decodes_utf16_and_utf8_output()
    {
        const string text = "Ubuntu\r\nDébian\r\n";

        Assert.Equal(text, OutputDecoder.Decode(Encoding.Unicode.GetBytes(text)));
        Assert.Equal(text, OutputDecoder.Decode(Encoding.UTF8.GetBytes(text)));
        Assert.Equal(text, OutputDecoder.Decode([0xFF, 0xFE, .. Encoding.Unicode.GetBytes(text)]));
        Assert.Equal(text, OutputDecoder.Decode([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(text)]));
        Assert.Equal(string.Empty, OutputDecoder.Decode([]));
    }

    [Fact]
    public void Parses_usbipd_state()
    {
        var devices = UsbIpdParser.ParseState(Fixture.Read("usbipd-state.json"));

        Assert.Equal(3, devices.Count);

        var keyboard = devices[0];
        Assert.Equal("1-4", keyboard.BusId);
        Assert.False(keyboard.IsShared);
        Assert.False(keyboard.IsAttached);
        Assert.Equal("046d:c52b", keyboard.HardwareId);

        var serial = devices[1];
        Assert.True(serial.IsShared);
        Assert.True(serial.IsAttached);
        Assert.Equal("172.28.64.1", serial.AttachedTo);

        var unplugged = devices[2];
        Assert.False(unplugged.IsConnected);
        Assert.True(unplugged.IsForced);
    }

    [Fact]
    public void Lxss_entry_builds_vhd_path_and_strips_long_path_prefix()
    {
        var entry = new LxssEntry(Guid.NewGuid(), "docker-desktop", 2, @"\\?\C:\Users\me\AppData\Local\Docker\wsl\main", "ext4.vhdx", 0, null, null);
        Assert.Equal(@"C:\Users\me\AppData\Local\Docker\wsl\main\ext4.vhdx", entry.VhdPath);

        var wsl1 = entry with { Version = 1 };
        Assert.Null(wsl1.VhdPath);

        var noFileName = entry with { BasePath = @"D:\wsl\ubuntu", VhdFileName = null };
        Assert.Equal(@"D:\wsl\ubuntu\ext4.vhdx", noFileName.VhdPath);
    }
}
