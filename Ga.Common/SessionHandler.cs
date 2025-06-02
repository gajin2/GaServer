using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Serilog;

namespace Ga.Common;


public abstract class SessionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        // 关服状态
        if (ServerService.Instance.IsStop)
        {
            connection.Abort(new ConnectionAbortedException("ServerIsStop"));
            return;
        }

        int netId = ServerService.Instance.GetNetId();
        if (netId <= 0)
        {
            connection.Abort(new ConnectionAbortedException("BadNetId"));
            return;
        }

        Log.Information($"{nameof(OnConnectedAsync)} {connection.RemoteEndPoint} NetId={netId}");

        var session = new NetSession(netId, connection);
        ServerService.Instance.Add(session);
        try
        {
            Task reading = ReadPipeAsync(session, connection.Transport.Input);
            await Task.WhenAll(reading);
        }
        catch (ConnectionAbortedException ex)
        {
            Log.Error($"Error while ReadPipeAsync Abort ex={ex.Message}");
        }
        catch (Exception e)
        {
            Log.Error($"Error while ReadPipeAsync ex={e.Message}");
        }
        finally
        {
            ServerService.Instance.Remove(session);
            ServerService.Instance.RetNetId(session.NetId);

            OnDisconnect(session);
        }

        Log.Information($"{nameof(OnConnectedAsync)} exit {connection.RemoteEndPoint} NetId={netId}");
    }

    async Task ReadPipeAsync(NetSession session, PipeReader reader)
    {
        while (true)
        {
            //Log.Information($"ReadPipeAsync closed?{session.IsClosed}");
            if (session.IsClosed)
            {
                Log.Information(
                    $"ReadPipeAsync session.IsClosed server={session.ServerId} {session.ServerName} reg?{session.IsReg}");
                break;
            }

            ReadResult result = await reader.ReadAtLeastAsync(4);
            if (result.IsCompleted)
            {
                Log.Information(
                    $"ReadPipeAsync result.IsCompleted at packLen server={session.ServerId} {session.ServerName} reg?{session.IsReg}");
                break;
            }

            ReadOnlySequence<byte> buffer = result.Buffer;
            Log.Debug($"ReadPipeAsync buff len={buffer.Length} server={session.ServerId}");

            int packLen = BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
            reader.AdvanceTo(buffer.GetPosition(4));

            result = await reader.ReadAtLeastAsync(packLen);

            if (result.IsCompleted)
            {
                Log.Information(
                    $"ReadPipeAsync result.IsCompleted at packBody server={session.ServerId} {session.ServerName} reg?{session.IsReg}");
                break;
            }

            buffer = result.Buffer;

            int proto = BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
            try
            {
                if (session.IsReg)
                {
                    if (0 < proto) // 心跳
                    {
                        OnRecv(session, proto, buffer.Slice(4));
                    }
                }
                else
                {
                    if (!ProcessReg(session, proto, buffer.Slice(4)))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ReadPipeAsync)} error {ex.Message}");
            }
            finally
            {
                reader.AdvanceTo(buffer.GetPosition(packLen));
            }
        }

        // Mark the PipeReader as complete.
        await reader.CompleteAsync();
    }

    private bool ProcessReg(NetSession session, int proto, ReadOnlySequence<byte> buffer)
    {
        if (proto == (int)Pb.ServerProto.Reg)
        {
            var pbMsg = Pb.ServerReg.Parser.ParseFrom(buffer);

            if (pbMsg.ServerCluster != ServerService.Instance.ServerCluster)
            {
                Log.Error(
                    $"{nameof(ProcessReg)} self cluster={ServerService.Instance.ServerCluster} but reg cluster={pbMsg.ServerCluster}");
                session.Context.Abort(new ConnectionAbortedException("BadServerCluster"));
                return false;
            }

            // 旧连接没有断开
            if (ServerService.Instance.Find(pbMsg.ServerId))
            {
                Log.Error($"{nameof(ProcessReg)} exist old server={pbMsg.ServerId} {pbMsg.ServerName}");
                session.Context.Abort(new ConnectionAbortedException("ExistOldServer"));
                return false;
            }

            session.ServerType = pbMsg.ServerType;
            session.ServerId = pbMsg.ServerId;
            session.ServerName = pbMsg.ServerName;
            session.IsReg = true;

            Log.Information($"{nameof(ProcessReg)} reg server={session.ServerId} {session.ServerName}");

            OnConnect(session, pbMsg);
            return true;
        }
        else
        {
            Log.Error($"{nameof(ProcessReg)} NOT reg read proto={proto} client={session.Context.RemoteEndPoint}");
        }

        return false;
    }

    public abstract void OnRecv(NetSession session, int proto, ReadOnlySequence<byte> buffer);
    public abstract void OnConnect(NetSession session, Pb.ServerReg pbMsg);
    public abstract void OnDisconnect(NetSession session);
}