namespace WslTamer.UI.Models;

public class UsbDevice
{
    public string BusId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // e.g., "Not shared", "Shared", "Attached"
    public bool IsAttached { get; set; }
}

public class PhysicalDisk
{
    public string DeviceId { get; set; } = string.Empty; // e.g., \\.\PHYSICALDRIVE1
    public string Model { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsMounted { get; set; }
}
