using System.Runtime.Serialization;
using FreeSql;
using Ga.Common;
using Orleans.Configuration;
using Orleans.Serialization;
using Serilog;

namespace Ga.Silo;

public static class Program
{
    public static void Main(string[] args)
    {
        GameUtil.InitLog("Silo.txt");
        ServerService.Instance.ServerType = (int)Pb.ServerType.Silo;

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


        builder.UseOrleans(ctx =>
        {
            OrleansOptions opt = new();
            ctx.Configuration.GetSection(nameof(OrleansOptions)).Bind(opt);

            string orleansConnStr = ctx.Configuration.GetConnectionString("Orleans") ?? "";
            if (string.IsNullOrEmpty(orleansConnStr))
            {
                throw new InvalidDataException("BadOrleansConnectionString");
            }

            string dbConnStr = ctx.Configuration.GetConnectionString("Database") ?? "";
            if (string.IsNullOrEmpty(dbConnStr))
            {
                throw new InvalidDataException("BadDatabaseConnectionString");
            }

            ctx.Services.AddFreeSql(options =>
            {
                options.UseConnectionString(DataType.PostgreSQL, ctx.Configuration.GetConnectionString("Database"));
                if (GameUtil.IsWin())
                {
                    options.UseMonitorCommand(cmd => Log.Debug($"Sql={cmd.CommandText}"));
                }
            });

            string serverName = ServerService.Instance.ServerName;
            if (string.IsNullOrEmpty(serverName) || serverName != opt.ServiceId)
            {
                throw new InvalidDataContractException("BadServerNameServiceId");
            }

            string serverCluster = ServerService.Instance.ServerCluster;
            if (string.IsNullOrEmpty(serverCluster) || serverCluster != opt.ClusterId)
            {
                throw new InvalidDataContractException("BadServerClusterId");
            }

            ctx.Configure<SiloOptions>(options => { options.SiloName = serverName; });

            if (GameUtil.IsWin())
            {
                ctx.UseLocalhostClustering(serviceId: opt.ServiceId, clusterId: opt.ClusterId,
                    siloPort: opt.SiloPort, gatewayPort: opt.GatewayPort);
            }
            else
            {
                ctx.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = opt.ClusterId;
                        options.ServiceId = opt.ServiceId;
                    })
                    .ConfigureEndpoints(opt.SiloPort, opt.GatewayPort);
            }

            ctx.Services.AddSerializer(serBuilder =>
            {
                serBuilder.AddJsonSerializer(isSupported: type =>
                    type.Namespace?.StartsWith("Grains") ?? false);
            });

            ctx.UseAdoNetClustering(options =>
                {
                    options.Invariant = Def.AdoInvariant;
                    options.ConnectionString = orleansConnStr;
                }).UseAdoNetReminderService(options =>
                {
                    options.Invariant = Def.AdoInvariant;
                    options.ConnectionString = orleansConnStr;
                })
                .AddAdoNetGrainStorage("orleansstorage", options =>
                {
                    options.Invariant = Def.AdoInvariant;
                    options.ConnectionString = orleansConnStr;
                });
        });

        // Init

        CenterClient.Instance.Init();

        // Add services to the container.

        builder.Services.AddSerilog();
        builder.Services.AddHostedService<SiloHostService>();
        builder.Services.AddSingleton(ServerService.Instance);
        builder.Services.AddSingleton(CenterClient.Instance);

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
