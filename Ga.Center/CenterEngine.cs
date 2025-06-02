using Ga.Common;
using Google.Protobuf;
using Serilog;

namespace Ga.Center;

public class CenterEngine : EngineBase<CenterMsg>
{
    public static readonly CenterEngine Instance = new CenterEngine();

    private readonly Dictionary<int, ServerInfo> _serverInfoList = new();

    public override bool Init()
    {
        return true;
    }

    protected override void OnStart()
    {
    }

    protected override void OnStop()
    {
    }

    protected override void OnMessage(CenterMsg msg)
    {
        switch (msg.MsgId)
        {
            case CenterMsgId.ServerConnect:
                OnServerConnect(msg);
                break;
            case CenterMsgId.ServerDisconnect:
                OnServerDisconnect(msg);
                break;
        }
    }

    private void OnServerConnect(CenterMsg msg)
    {
        Log.Information($"OnServerConnect {msg.MsgId}");

        var pbMsg = (Pb.ServerReg)msg.Body!;

        ServerInfo info = new();
        info.ServerType = pbMsg.ServerType;
        info.ServerId = pbMsg.ServerId;
        info.ServerName = pbMsg.ServerName;
        info.Host = pbMsg.Host;
        info.Port = pbMsg.Port;
        info.Session = msg.Session;

        _serverInfoList.Add(info.ServerId, info);

        DataPacket pack = new((int)Pb.ServerProto.ServerConnectCenter);
        pack.WriteByteArray(pbMsg.ToByteArray());

        foreach (var it in _serverInfoList.Values)
        {
            if (it.ServerId != info.ServerId)
            {
                Log.Debug($"OnServerConnect notify {it.ServerId}");
                it.Session?.Send(pack);
            }
        }

        // 通知新连接
        if (info.ServerType == (int)Pb.ServerType.World)
        {
            Pb.ServerGameList pbList = new();
            foreach (var it in _serverInfoList.Values)
            {
                if (it.ServerType == (int)Pb.ServerType.Game)
                {
                    pbList.GameList.Add(new Pb.ServerReg()
                    {
                        ServerType = it.ServerType,
                        ServerId = it.ServerId,
                        ServerName = it.ServerName,
                        Host = it.Host,
                        Port = it.Port,
                    });
                }
                else if (it.ServerType == (int)Pb.ServerType.Silo)
                {
                    pbList.SiloList.Add(new Pb.ServerReg()
                    {
                        ServerType = it.ServerType,
                        ServerId = it.ServerId,
                        ServerName = it.ServerName,
                        Host = it.Host,
                        Port = it.Port,
                    });
                }
            }

            pack = new((int)Pb.ServerProto.ToWorldGameList);
            pack.WriteByteArray(pbList.ToByteArray());

            info.Session?.Send(pack);
        }
    }

    private void OnServerDisconnect(CenterMsg msg)
    {
        Log.Information($"OnServerDisconnect {msg.MsgId}");

        var session = msg.Session!;
        _serverInfoList.Remove(session.ServerId);

        Pb.ServerReg pbMsg = new();
        pbMsg.ServerType = session.ServerType;
        pbMsg.ServerId = session.ServerId;
        pbMsg.ServerName = session.ServerName;

        DataPacket pack = new((int)Pb.ServerProto.ServerDisconnectCenter);
        pack.WriteByteArray(pbMsg.ToByteArray());

        foreach (var it in _serverInfoList.Values)
        {
            Log.Debug($"OnServerDisconnect notify {it.ServerId}");
            it.Session?.Send(pack);
        }
    }
}