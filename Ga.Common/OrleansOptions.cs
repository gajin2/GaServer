namespace Ga.Common;

public class OrleansOptions
{
    public string ClusterId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public int SiloPort { get; set; }
    public int GatewayPort { get; set; }
}