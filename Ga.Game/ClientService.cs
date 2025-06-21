using System.Collections.Concurrent;
using Serilog;

namespace Ga.Game;

public class ClientService
{
    const int NetIdMax = 8192;
    public static readonly ClientService Instance = new();
    public  ConcurrentQueue<int> IdList { get; } = new();
    public  ConcurrentDictionary<int, ClientSession> Sessions { get; } = new();

    private ClientService()
    {
        InitIdList();
    }

    private void InitIdList()
    {
        for (int i = 0; i < NetIdMax; i++)
        {
            IdList.Enqueue(i);
        }
    }

    internal int GetNetId()
    {
        if (IdList.Count == 0)
        {
            return -1;
        }

        if (IdList.TryDequeue(out int id))
        {
            Log.Information($"{nameof(GetNetId)} {id}");
            return id;
        }

        return -1;
    }

    internal void RetNetId(int id)
    {
        Log.Information($"{nameof(RetNetId)} {id}");

#if DEBUG
        if (IdList.Contains(id))
        {
            throw new InvalidDataException($"NetId={id}");
        }
#endif

        IdList.Enqueue(id);

        Log.Information($"{nameof(RetNetId)} {id} len={IdList.Count}");
    }

    public void Add(ClientSession session)
    {
        Sessions[session.NetId] = session;
    }

    public void Remove(ClientSession session)
    {
        int id = session.NetId;
        if (0 <= id)
        {
            Sessions.Remove(id, out _);
        }
    }

    public void Stop()
    {
        Log.Information($"{nameof(Stop)}");

        Sessions.Values.ToList().ForEach(it => {
            it.Close($"{nameof(Stop)}").Wait();
        });

        Sessions.Clear();
    }
}