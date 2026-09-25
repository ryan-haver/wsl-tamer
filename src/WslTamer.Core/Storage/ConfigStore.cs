using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WslTamer.Core.Automation;
using WslTamer.Core.Profiles;

namespace WslTamer.Core.Storage;

public interface IConfigStore
{
    string FilePath { get; }

    /// <summary>Set when the last load found an unreadable file and moved it aside.</summary>
    string? RecoveredFromPath { get; }

    AppConfig Load();
    void Save(AppConfig config);
}

/// <summary>
/// Stores app settings in %AppData%\WslTamer\config.json. Reads the v1.x format and
/// upgrades it. A corrupt file is renamed rather than overwritten.
/// </summary>
public sealed class ConfigStore(string filePath, ILogger<ConfigStore>? logger = null) : IConfigStore
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ILogger _logger = logger ?? NullLogger<ConfigStore>.Instance;

    public ConfigStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WslTamer", "config.json"))
    {
    }

    public string FilePath { get; } = filePath;

    public string? RecoveredFromPath { get; private set; }

    public AppConfig Load()
    {
        RecoveredFromPath = null;
        if (!File.Exists(FilePath))
        {
            return CreateDefault();
        }

        try
        {
            var node = JsonNode.Parse(File.ReadAllText(FilePath)) as JsonObject
                ?? throw new JsonException("The file is not a JSON object.");

            int version = node["SchemaVersion"]?.GetValue<int>() ?? 1;
            var config = version >= AppConfig.CurrentSchemaVersion
                ? node.Deserialize<AppConfig>(JsonOptions) ?? throw new JsonException("Empty configuration.")
                : MigrateFromV1(node);

            Normalize(config);
            return config;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            var aside = $"{FilePath}.corrupt-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";
            _logger.LogError(ex, "Could not read {Path}; moving it to {Aside}", FilePath, aside);
            File.Move(FilePath, aside, overwrite: true);
            RecoveredFromPath = aside;
            return CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        config.SchemaVersion = AppConfig.CurrentSchemaVersion;
        AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(config, JsonOptions), FilePath + ".bak");
    }

    public static AppConfig CreateDefault() => new()
    {
        Profiles = [.. WslProfile.CreateDefaults(Environment.ProcessorCount, SystemInfo.TotalPhysicalMemory)],
    };

    /// <summary>
    /// v1 stored every profile field whether or not the user cared about it. Each is
    /// mapped to a managed setting so applying a migrated profile writes the same keys
    /// as before (but no longer deletes the rest of .wslconfig).
    /// </summary>
    internal static AppConfig MigrateFromV1(JsonObject v1)
    {
        var config = new AppConfig();
        foreach (var p in v1["Profiles"]?.AsArray().OfType<JsonObject>() ?? [])
        {
            var profile = new WslProfile
            {
                Id = TryGuid(p["Id"]) ?? Guid.NewGuid(),
                Name = p["Name"]?.GetValue<string>() is { Length: > 0 } name ? name : "Profile",
            };

            void Put(string id, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    profile.Settings[id] = value.Trim();
                }
            }

            Put("wsl2.memory", p["Memory"]?.GetValue<string>());
            if (p["Processors"]?.GetValue<int>() is > 0 and var cores)
            {
                Put("wsl2.processors", cores.ToString(CultureInfo.InvariantCulture));
            }

            Put("wsl2.swap", p["Swap"]?.GetValue<string>());
            Put("wsl2.kernel", p["KernelPath"]?.GetValue<string>());
            Put("wsl2.localhostForwarding", Bool(p["LocalhostForwarding"]));
            Put("wsl2.guiApplications", Bool(p["GuiApplications"]));
            Put("wsl2.debugConsole", Bool(p["DebugConsole"]));

            // v1 offered "Bridged", which needs a vmSwitch it never wrote; treat it as NAT.
            var mode = p["NetworkingMode"]?.GetValue<string>()?.Trim().ToLowerInvariant();
            Put("wsl2.networkingMode", mode is "mirrored" ? "mirrored" : mode is null ? null : "nat");

            config.Profiles.Add(profile);
        }

        foreach (var r in v1["Rules"]?.AsArray().OfType<JsonObject>() ?? [])
        {
            // v1 trigger types: 0 Time (never worked), 1 Process, 2 PowerState, 3 Network.
            TriggerType? type = r["TriggerType"]?.GetValue<int>() switch
            {
                1 => TriggerType.ProcessRunning,
                2 => TriggerType.PowerSource,
                3 => TriggerType.NetworkConnected,
                _ => null,
            };

            if (type is null || TryGuid(r["TargetProfileId"]) is not { } target)
            {
                continue;
            }

            var value = r["TriggerValue"]?.GetValue<string>() ?? string.Empty;
            if (type == TriggerType.PowerSource)
            {
                value = value.Equals("OnBattery", StringComparison.OrdinalIgnoreCase) ? "Battery" : "AC";
            }

            config.Rules.Add(new AutomationRule
            {
                Id = TryGuid(r["Id"]) ?? Guid.NewGuid(),
                IsEnabled = r["IsEnabled"]?.GetValue<bool>() ?? true,
                TriggerType = type.Value,
                TriggerValue = value,
                TargetProfileId = target,
            });
        }

        config.FallbackProfileId = TryGuid(v1["DefaultProfileId"]);
        if (config.Profiles.Count == 0)
        {
            config.Profiles.AddRange(CreateDefault().Profiles);
        }

        return config;
    }

    private static void Normalize(AppConfig config)
    {
        config.Profiles ??= [];
        config.Rules ??= [];
        config.KeepAliveDistros ??= [];
        config.Preferences ??= new AppPreferences();

        foreach (var profile in config.Profiles)
        {
            profile.Settings = new Dictionary<string, string?>(profile.Settings ?? [], StringComparer.OrdinalIgnoreCase);
        }

        var ids = config.Profiles.Select(p => p.Id).ToHashSet();
        config.Rules.RemoveAll(r => !ids.Contains(r.TargetProfileId));
        if (config.FallbackProfileId is { } fallback && !ids.Contains(fallback))
        {
            config.FallbackProfileId = null;
        }
    }

    private static Guid? TryGuid(JsonNode? node) =>
        node?.GetValueKind() == JsonValueKind.String && Guid.TryParse(node.GetValue<string>(), out var id) ? id : null;

    private static string? Bool(JsonNode? node) =>
        node?.GetValueKind() is JsonValueKind.True or JsonValueKind.False
            ? (node.GetValue<bool>() ? "true" : "false")
            : null;
}
