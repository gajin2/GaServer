using System.Runtime.Serialization;
using Ga.Common;
using Orleans.Configuration;
using Orleans.Serialization;
using Serilog;

namespace Ga.Game;

class Program
{
    static void Main(string[] args)
    {
        GameUtil.InitLog("Game.txt");
        ServerService.Instance.ServerType = (int)Pb.ServerType.Game;

        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("config.json");

        builder.Configuration.GetSection(nameof(GaOptions)).Bind(ServerService.Instance.Options);
        builder.Configuration.GetSection(nameof(CenterOptions)).Bind(ServerService.Instance.CenterOptions);

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

        builder.Host.UseOrleansClient((ctx, siloBuilder) =>
        {
            OrleansOptions opt = new();
            ctx.Configuration.GetSection(nameof(OrleansOptions)).Bind(opt);

            if (!opt.ServiceId.Equals(ServerService.Instance.ServerName))
            {
                throw new InvalidDataContractException("BadServiceIdServerName");
            }

            if (!opt.ClusterId.Equals(ServerService.Instance.ServerCluster))
            {
                throw new InvalidDataContractException("BadClusterId");
            }

            string orleansConnStr = ctx.Configuration.GetConnectionString("Orleans") ?? "";
            if (string.IsNullOrEmpty(orleansConnStr))
            {
                throw new InvalidDataException("BadOrleansConnectionString");
            }

            siloBuilder.Services.AddSerializer(serBuilder =>
            {
                serBuilder.AddJsonSerializer(isSupported: type => type.Namespace?.StartsWith("Grains") ?? false);
            });

            if (GameUtil.IsWin())
            {
                siloBuilder.UseLocalhostClustering(serviceId: opt.ServiceId, clusterId: opt.ClusterId,
                    gatewayPort: opt.GatewayPort);
            }
            else
            {
                siloBuilder.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = opt.ClusterId;
                        options.ServiceId = opt.ServiceId;
                    })
                    .UseAdoNetClustering(options =>
                    {
                        options.Invariant = Def.AdoInvariant;
                        options.ConnectionString = orleansConnStr;
                    });
            }
        });

        // init

        CenterClient.Instance.Init();

        // Add services to the container.

        builder.Services.AddSerilog();
        builder.Services.AddHostedService<GameHostService>();
        builder.Services.AddSingleton(ServerService.Instance);
        builder.Services.AddSingleton(ClientService.Instance);
        builder.Services.AddSingleton(CenterClient.Instance);
        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseWebSockets();
        app.MapControllers();

        try
        {
            app.Run();
            Log.Information("exit...");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "app.Run ex");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}