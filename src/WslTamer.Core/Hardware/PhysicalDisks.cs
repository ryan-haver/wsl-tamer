using System.Globalization;
using System.Management;

namespace WslTamer.Core.Hardware;

public sealed record PhysicalDisk(
    int Number,
    string FriendlyName,
    long SizeBytes,
    bool IsBoot,
    bool IsSystem,
    bool IsOffline,
    string BusType)
{
    public string DevicePath => $@"\\.\PHYSICALDRIVE{Number.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Disks Windows boots or runs from must never be handed to WSL.</summary>
    public bool CanMount => !IsBoot && !IsSystem;
}

public interface IPhysicalDiskProvider
{
    IReadOnlyList<PhysicalDisk> GetDisks();
}

/// <summary>Reads disks from the Windows Storage WMI provider (MSFT_Disk).</summary>
public sealed class PhysicalDiskProvider : IPhysicalDiskProvider
{
    public IReadOnlyList<PhysicalDisk> GetDisks()
    {
        var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
        var query = new ObjectQuery("SELECT Number, FriendlyName, Size, IsBoot, IsSystem, IsOffline, BusType FROM MSFT_Disk");
        using var searcher = new ManagementObjectSearcher(scope, query);
        using var results = searcher.Get();

        var disks = new List<PhysicalDisk>();
        foreach (var item in results.OfType<ManagementObject>())
        {
            using (item)
            {
                disks.Add(new PhysicalDisk(
                    Number: Convert.ToInt32(item["Number"], CultureInfo.InvariantCulture),
                    FriendlyName: item["FriendlyName"] as string ?? "Disk",
                    SizeBytes: Convert.ToInt64(item["Size"] ?? 0UL, CultureInfo.InvariantCulture),
                    IsBoot: item["IsBoot"] as bool? ?? true,
                    IsSystem: item["IsSystem"] as bool? ?? true,
                    IsOffline: item["IsOffline"] as bool? ?? false,
                    BusType: BusTypeName(item["BusType"])));
            }
        }

        return disks.OrderBy(d => d.Number).ToList();
    }

    private static string BusTypeName(object? value) => Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture) switch
    {
        1 => "SCSI",
        3 => "ATA",
        7 => "USB",
        8 => "RAID",
        10 => "SAS",
        11 => "SATA",
        12 => "SD",
        13 => "MMC",
        15 => "File-backed",
        17 => "NVMe",
        _ => "Other",
    };
}
