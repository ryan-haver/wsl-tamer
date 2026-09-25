using WslTamer.Core.Config;
using WslTamer.Core.Tests.Support;

namespace WslTamer.Core.Tests;

public class IniDocumentTests
{
    [Fact]
    public void Round_trips_a_real_file_byte_for_byte()
    {
        var text = Fixture.Read("wslconfig-docs-sample.txt");
        Assert.Equal(text, IniDocument.Parse(text).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("[wsl2]")]
    [InlineData("[wsl2]\nmemory=4GB")]
    [InlineData("﻿[wsl2]\r\nmemory=4GB\r\n")]
    [InlineData("   \n# only a comment\n\n")]
    public void Round_trips_edge_cases(string text)
    {
        var expected = text.TrimStart('﻿');
        Assert.Equal(expected, IniDocument.Parse(text).ToString());
    }

    [Fact]
    public void Reads_values_case_insensitively_and_splits_on_first_equals()
    {
        var doc = IniDocument.Parse(Fixture.Read("wslconfig-docs-sample.txt"));

        Assert.Equal("4GB", doc.Get("WSL2", "Memory"));
        Assert.Equal("vsyscall=emulate", doc.Get("wsl2", "kernelCommandLine"));
        Assert.Equal("true", doc.Get("wsl2", "localhostForwarding"));
        Assert.Equal("true", doc.Get("experimental", "sparseVhd"));
        Assert.Null(doc.Get("wsl2", "swap"));
        Assert.Equal(@"C:\temp\myCustomKernel", WslConfigCatalog.Find("wsl2", "kernel")!.FromFileValue(doc.Get("wsl2", "kernel")!));
    }

    [Fact]
    public void Changing_a_value_touches_only_that_line()
    {
        var text = Fixture.Read("wslconfig-docs-sample.txt");
        var doc = IniDocument.Parse(text);

        doc.Set("wsl2", "memory", "8GB");

        Assert.Equal(text.Replace("memory=4GB", "memory=8GB", StringComparison.Ordinal), doc.ToString());
    }

    [Fact]
    public void Keeps_existing_spelling_and_spacing_when_updating()
    {
        var doc = IniDocument.Parse("[wsl2]\nkernelCommandLine = vsyscall=emulate\nlocalhostforwarding=true\n");

        doc.Set("wsl2", "kernelCommandLine", "quiet");
        doc.Set("wsl2", "localhostForwarding", "false");

        Assert.Equal("[wsl2]\nkernelCommandLine = quiet\nlocalhostforwarding=false\n", doc.ToString());
    }

    [Fact]
    public void Adds_new_key_after_the_last_key_of_its_section()
    {
        var doc = IniDocument.Parse("[wsl2]\nmemory=4GB\n\n# experimental below\n[experimental]\nsparseVhd=true\n");

        doc.Set("wsl2", "swap", "0");

        Assert.Equal("[wsl2]\nmemory=4GB\nswap=0\n\n# experimental below\n[experimental]\nsparseVhd=true\n", doc.ToString());
    }

    [Fact]
    public void Creates_missing_section_at_the_end_with_file_newlines()
    {
        var doc = IniDocument.Parse("[wsl2]\r\nmemory=4GB\r\n");

        doc.Set("experimental", "autoMemoryReclaim", "gradual");

        Assert.Equal("[wsl2]\r\nmemory=4GB\r\n\r\n[experimental]\r\nautoMemoryReclaim=gradual\r\n", doc.ToString());
    }

    [Fact]
    public void Empty_document_gets_a_section()
    {
        var doc = IniDocument.Empty("\r\n");
        doc.Set("wsl2", "memory", "8GB");
        Assert.Equal("[wsl2]\r\nmemory=8GB\r\n", doc.ToString());
    }

    [Fact]
    public void Updates_every_duplicate_and_removes_all_on_remove()
    {
        var doc = IniDocument.Parse("[wsl2]\nmemory=2GB\nmemory=4GB\n[wsl2]\nmemory=6GB\n");

        doc.Set("wsl2", "memory", "8GB");
        Assert.Equal("[wsl2]\nmemory=8GB\nmemory=8GB\n[wsl2]\nmemory=8GB\n", doc.ToString());

        Assert.True(doc.Remove("wsl2", "MEMORY"));
        Assert.Equal("[wsl2]\n[wsl2]\n", doc.ToString());
        Assert.False(doc.Remove("wsl2", "memory"));
    }

    [Fact]
    public void Preserves_unknown_sections_and_comments()
    {
        const string text = "; vendor comment\n[custom]\nfoo = bar # not a comment to us\n[wsl2]\nmemory=4GB\n";
        var doc = IniDocument.Parse(text);

        doc.Set("wsl2", "processors", "4");

        Assert.Equal("; vendor comment\n[custom]\nfoo = bar # not a comment to us\n[wsl2]\nmemory=4GB\nprocessors=4\n", doc.ToString());
        Assert.Equal("bar # not a comment to us", doc.Get("custom", "foo"));
    }

    [Fact]
    public void Rejects_values_with_line_breaks()
    {
        var doc = IniDocument.Empty();
        Assert.Throws<ArgumentException>(() => doc.Set("boot", "command", "a\nb"));
    }
}
