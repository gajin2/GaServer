using System.Collections.Concurrent;
using Serilog;

namespace Ga.Common;

public class ServerService
{
    public static readonly ServerService Instance = new();

    private const int NetIdMin = 1;
    private const int NetIdMax = 256;

    public readonly GaOptions Options = new();

    public int ServerId => Options.ServerId;
    public string ServerName => Options.ServerName;
    public string ServerCluster => Options.ServerCluster;
    public int ServerPort => Options.ServerPort;
    public int ServerType { get; set; }

    public bool IsStop { get; set; } = false;
    private readonly List<int> _netIdList = new();
    private readonly ConcurrentDictionary<int, NetSession> _sessions = new();

    private ServerService()
    {
        for (int i = NetIdMin; i <= NetIdMax; i++)
        {
            _netIdList.Add(i);
        }
    }

    public void Stop()
    {
        IsStop = true;

        var list = _sessions.Values.ToArray();
        foreach (var session in list)
        {
            session.Stop();
        }

        _sessions.Clear();
        Log.Information($"ServerService.Stop exit {ServerName}");
    }

    public int GetNetId()
    {
        if (_netIdList.Count == 0)
        {
            return 0;
        }

        int ret = _netIdList[0];
        _netIdList.RemoveAt(0);
        return ret;
    }

    public void RetNetId(int id)
    {
        if (NetIdMin <= id && id <= NetIdMax)
        {
            _netIdList.Add(id);
        }

        Log.Information($"ServerService.RetNetId len={_netIdList.Count}");
    }

    public void Add(NetSession session)
    {
        _sessions[session.NetId] = session;
    }

    public void Remove(NetSession session)
    {
        _sessions.Remove(session.NetId, out _);
    }

    public bool Find(int serverId)
    {
        foreach (var it in _sessions.Values)
        {
            if (it.ServerId == serverId)
            {
                return true;
            }
        }

        return false;
    }
}