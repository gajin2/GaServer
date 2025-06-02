using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Serilog;

namespace Ga.Common;

public class NetSession
{
    public int NetId { get; private set; }
    public int ServerType { get; set; }
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public bool IsClosed { get; private set; }
    public bool IsReg { get; set; }
    public ConnectionContext Context { get; }

    public NetSession(int netId, ConnectionContext client)
    {
        NetId = netId;
        Context = client;
        client.ConnectionClosed.Register(OnClose);
    }

    public void Stop()
    {
        Context.Abort(new ConnectionAbortedException($"Stop:{ServerId}"));
    }

    void OnClose()
    {
        IsClosed = true;
        if (IsReg)
        {
            Log.Information($"NetSession.OnClose {Context.RemoteEndPoint} server={ServerId} {ServerName}");
        }
        else
        {
            Log.Information($"NetSession.OnClose {Context.RemoteEndPoint} NOT reg");
        }
    }

    public void Send(DataPacket pack)
    {
        Task.Run(async () =>
        {
            await Context.Transport.Output.WriteAsync(pack.ToPackBytes());
            await Context.Transport.Output.FlushAsync();

            Log.Debug($"Send {ServerId} len={pack.ToPackBytes().Length}");
        });
    }

    public async Task SendAsync(DataPacket pack)
    {
        await Context.Transport.Output.WriteAsync(pack.ToPackBytes());
        await Context.Transport.Output.FlushAsync();
    }

    public void Send(ReadOnlySequence<byte> buf)
    {
        Task.Run(async () =>
        {
            await Context.Transport.Output.WriteAsync(buf.First);
            await Context.Transport.Output.FlushAsync();
        });
    }

    public async Task SendAsync(ReadOnlySequence<byte> buf)
    {
        await Context.Transport.Output.WriteAsync(buf.First);
        await Context.Transport.Output.FlushAsync();
    }
}