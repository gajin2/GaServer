using Ga.Common;
using Serilog;

namespace Ga.Center;

public class CenterHostService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StartAsync...");
        Log.Information($"CenterHostService IP={GameUtil.HostIp()}");
        CenterEngine.Instance.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StopAsync...");
        CenterEngine.Instance.Stop();
        ServerService.Instance.Stop();
        return Task.CompletedTask;
    }
}