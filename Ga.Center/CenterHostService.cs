using Ga.Common;
using Serilog;

namespace Ga.Center;

public class CenterHostService : IHostedLifecycleService
{
    // 1
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StartingAsync...");
        CenterEngine.Instance.Start();
        return Task.CompletedTask;
    }

    // 2
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StartAsync...");
        Log.Information($"CenterHostService IP={GameUtil.HostIp()}");
        return Task.CompletedTask;
    }

    // 3
    public Task StartedAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StartedAsync...");
        return Task.CompletedTask;
    }

    // 1
    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StoppingAsync...");
        CenterEngine.Instance.Stop();
        ServerService.Instance.Stop();
        return Task.CompletedTask;
    }

    // 2
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StopAsync...");
        return Task.CompletedTask;
    }

    // 3
    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        Log.Information("CenterHostService StoppedAsync...");
        return Task.CompletedTask;
    }
}