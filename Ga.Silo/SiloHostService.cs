using Ga.Common;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Ga.Silo;

public sealed class SiloHostService: IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("SiloHostService StartAsync...");
        Log.Information($"SiloHostService IP={GameUtil.HostIp()}");
        CenterClient.Instance.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("SiloHostService StopAsync...");
        CenterClient.Instance.Stop();
        return Task.CompletedTask;
    }
}