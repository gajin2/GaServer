namespace Ga.Common;

public sealed class CenterClient : NetClientBase
{
    private CenterClient()
    {
    }

    public static readonly CenterClient Instance = new();

    public override void Init()
    {
        Host = ServerService.Instance.CenterOptions.Host;
        Port = ServerService.Instance.CenterOptions.Port;

        ClientName = $"{nameof(CenterClient)}:{Host}:{Port}";
    }
}