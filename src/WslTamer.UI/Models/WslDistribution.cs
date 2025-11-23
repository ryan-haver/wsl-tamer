namespace WslTamer.UI.Models;

public class WslDistribution
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsDefault { get; set; }
    public bool IsRunning => State.Equals("Running", StringComparison.OrdinalIgnoreCase);
}
