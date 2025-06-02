namespace Ga.Common;

public sealed class GaOptions
{
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string ServerCluster { get; set; } = string.Empty;
    public int ServerPort { get; set; }
}