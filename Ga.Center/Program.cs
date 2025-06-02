using Ga.Common;
using Microsoft.AspNetCore.Connections;
using Serilog;

namespace Ga.Center;

static class Program
{
    static void Main(string[] args)
    {
        GameUtil.InitLog("Center.txt");
        ServerService.Instance.ServerType = (int)Pb.ServerType.Center;

        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("config.json");

        builder.Configuration.GetSection(nameof(GaOptions)).Bind(ServerService.Instance.Options);

        if (string.IsNullOrEmpty(ServerService.Instance.Options.ServerCluster))
        {
            throw new InvalidDataException("ServerCluster is null");
        }

        if (ServerService.Instance.Options.ServerId <= 0)
        {
            throw new InvalidDataException($"ServerId is {ServerService.Instance.Options.ServerId}");
        }

        if (string.IsNullOrEmpty(ServerService.Instance.Options.ServerName))
        {
            throw new InvalidDataException("ServerName is null");
        }

        builder.WebHost.UseKestrel(opt =>
        {
            int serverPort = ServerService.Instance.ServerPort;
            opt.ListenAnyIP(serverPort, b => b.UseConnectionHandler<ServerHandler>());
        });

        // init

        if (!CenterEngine.Instance.Init())
        {
            Log.Error("CenterEngine.Init() Failed");
            return;
        }

        // Add services to the container.

        builder.Services.AddSerilog();
        builder.Services.AddHostedService<CenterHostService>();
        builder.Services.AddSingleton(ServerService.Instance);
        builder.Services.AddSingleton(CenterEngine.Instance);

        try
        {
            using IHost host = builder.Build();
            host.Run();
            Log.Information("exit...");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}