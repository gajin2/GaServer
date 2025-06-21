using System.Buffers;
using System.Net.WebSockets;
using Ga.Common;
using Ga.GrainInterface;
using Serilog;

namespace Ga.Game;

public sealed class ClientSession
{
    private readonly WebSocket _ws;
    readonly IGrainFactory _grains;

    public int GameState { get; set; } = Def.GameStateLogin; // login/game/close
    public WebSocket WebSocket => _ws;

    public int NetId { get; internal set; }
    public string RemoteAddr { get; set; } = string.Empty;

    readonly byte[] _readBuf = new byte[0xFFFF * 10];
    readonly ArrayBufferWriter<byte> _readList = new();

    ISessionObserver _observer;
    ISessionObserver _observerRef;

    public ClientSession(WebSocket ws, IGrainFactory grains)
    {
        _ws = ws;
        _grains = grains;
        _observer = new SessionObserver(this);
        _observerRef = grains.CreateObjectReference<ISessionObserver>(_observer);
    }

    public void Send(byte[] data)
    {
        if (_ws.State == WebSocketState.Open)
        {
            _ws.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    public void Send(ReadOnlySequence<byte> buffer)
    {
        Log.Debug($"NetId={NetId} Send len={buffer.Length}");

        if (_ws.State == WebSocketState.Open)
        {
            _ws.SendAsync(buffer.First, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    public async Task Close(string? msg)
    {
        Log.Information($"NetSession.Close NetId={NetId} msg={msg ?? ""}");
        if (_ws.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, msg ?? "ServerClose", CancellationToken.None);
        }
    }

    public async Task Run()
    {
        Task readIn = ReadWs();
        await Task.WhenAll(readIn);

        Log.Information($"WsSession.Run NetId={NetId} exit State={_ws.State}");
    }

    private async Task ReadWs()
    {
        try
        {
            while (_ws.State == WebSocketState.Open)
            {
                var res = await _ws.ReceiveAsync(_readBuf, CancellationToken.None);
                if (res.MessageType == WebSocketMessageType.Close)
                {
                    Log.Information($"ReadWs NetId={NetId} close");
                    break;
                }

                if (res.EndOfMessage)
                {
                    if (_readList.WrittenCount == 0)
                    {
                        OnRecv(_readBuf[..res.Count]);
                    }
                    else
                    {
                        _readList.Write(_readBuf[..res.Count]);
                        OnRecv(_readList.WrittenSpan);
                        _readList.Clear();
                    }
                }
                else
                {
                    Log.Information($"ReadWs NetId={NetId} NOT EndOfMessage res.Count={res.Count}");

                    _readList.Write(_readBuf[..res.Count]);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{nameof(ReadWs)} error={ex.Message}");
        }
        finally
        {
            await Close($"{nameof(ReadWs)}");
        }
    }

    private void OnRecv(ReadOnlySpan<byte> buf)
    {
        Log.Debug($"buf len={buf.Length} GameState={GameState} thr={Thread.CurrentThread.ManagedThreadId}");
        if (buf.Length < Def.ProtoByteLen)
        {
            return;
        }

        int proto = BitConverter.ToInt32(buf);
        Log.Debug($"proto={proto}");
        var req = new GrainRequest()
        {
            NetId = NetId,
            ServerId = ServerService.Instance.ServerId,
            Payload = buf.Slice(Def.ProtoByteLen).ToArray(),
            Proto = proto,
        };

        switch (GameState)
        {
            case Def.GameStateGame:
                break;
            case Def.GameStateLogin:
                OnLoginMsg(req);
                break;
        }
    }

    void OnLoginMsg(GrainRequest req)
    {
        Task.Run(async () =>
        {
            try
            {
                string loginKey = $"{ServerService.Instance.ServerName}.{NetId}";
                var grain = _grains.GetGrain<ILoginGrain>(loginKey);
                await grain.Handle(req, _observerRef);
            }
            catch (Exception ex)
            {
                Log.Error($"OnLoginMsg ex={ex}");
            }
        });
    }
}