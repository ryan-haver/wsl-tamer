using WslTamer.Core.Storage;

namespace WslTamer.Core.Config;

public interface IWslConfigFile
{
    string FilePath { get; }

    /// <summary>Loads .wslconfig, or an empty document if it doesn't exist.</summary>
    IniDocument Load();

    /// <summary>Saves atomically, keeping the previous version as .wslconfig.wsltamer.bak.</summary>
    void Save(IniDocument document);
}

/// <summary>%UserProfile%\.wslconfig.</summary>
public sealed class WslConfigFile : IWslConfigFile
{
    public WslConfigFile()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wslconfig"))
    {
    }

    public WslConfigFile(string filePath) => FilePath = filePath;

    public string FilePath { get; }

    public string BackupPath => FilePath + ".wsltamer.bak";

    public IniDocument Load() =>
        File.Exists(FilePath)
            ? IniDocument.Parse(File.ReadAllText(FilePath), defaultNewLine: "\r\n")
            : IniDocument.Empty("\r\n");

    public void Save(IniDocument document) =>
        AtomicFile.WriteAllText(FilePath, document.ToString(), BackupPath);
}
