using Ga.Common;

namespace Ga.Center;

public sealed class ServerInfo
{
    public int ServerType { get; set; }
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public NetSession? Session { get; set; }
}