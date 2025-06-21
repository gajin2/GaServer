using Ga.Common;
using Serilog;

namespace Ga.Game;

public sealed class GameHostService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("GameHostService StartAsync...");
        Log.Information($"GameHostService IP={GameUtil.HostIp()}");
        CenterClient.Instance.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        CenterClient.Instance.Stop();
        ClientService.Instance.Stop();
        return Task.CompletedTask;
    }
}