using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Ga.Common;

public static class GameUtil
{
    public static void InitLog(string logFile)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        if (string.IsNullOrEmpty(logFile))
        {
            Log.Fatal("BadLogFile");
        }

        Log.Information($"CurDir={Directory.GetCurrentDirectory()}");
        string? runDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        Log.Information("RunDir={string}", runDir);
        if (runDir != null)
        {
            Directory.SetCurrentDirectory(runDir);
        }

        string logDir = ".." + Path.DirectorySeparatorChar + "log";
        logDir = Path.GetFullPath(logDir);
        Directory.CreateDirectory(logDir);
        string logPath = logDir + Path.DirectorySeparatorChar + logFile;
        Log.Information($"Log={logPath}");

        bool buffered = !IsWin();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(GetConfiguration())
            .WriteTo.Console()
            .WriteTo.Async(a =>
                a.File(logPath, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: null, buffered: buffered))
            .CreateLogger();
    }

    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json")
            .Build();
    }

    public static bool IsWin()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public static string HostIp()
    {
        string hostName = Dns.GetHostName(); // 获取本机主机名
        IPHostEntry hostEntry = Dns.GetHostEntry(hostName); // 获取主机的 IP 地址信息

        foreach (IPAddress ipAddress in hostEntry.AddressList)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork) // 过滤 IPv4 地址
            {
                return ipAddress.ToString();
            }
        }

        return "";
    }
}