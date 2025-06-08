using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Pb;
using Serilog;

namespace Ga.Common;

public class NetClientBase
{
    private readonly ConcurrentQueue<DataPacket> _wQueue = new();


    public NetClientBase()
    {
        OnRecvCallback = DefRecvCallback;
    }


    public CancellationTokenSource Cts { get; } = new();
    private Task? RunTask { get; set; }

    public string Host { get; set; } = IPAddress.Loopback.ToString();
    public int Port { get; set; }
    public string ClientName { get; set; } = string.Empty;
    private Socket? ClientSocket { get; set; }

    public Action<int, ReadOnlySequence<byte>> OnRecvCallback { get; set; }

    private void DefRecvCallback(int proto, ReadOnlySequence<byte> data)
    {
        Log.Information($"{ClientName}.DefRecvCallback: proto={proto}");
    }

    public virtual void Init()
    {
    }

    public void Start()
    {
        RunTask = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
    }

    public void Stop()
    {
        Cts.Cancel();
        RunTask?.Wait();
    }

    private async Task Run()
    {
        while (!Cts.IsCancellationRequested)
        {
            Task? wTask = null;
            Task? heartTask = null;

            try
            {
                Thread.Sleep(1000);

                var ipEndPoint = new IPEndPoint(IPAddress.Parse(Host), Port);
                if (string.IsNullOrEmpty(ClientName))
                {
                    ClientName = ipEndPoint.ToString();
                }

                var client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ClientSocket = client;
                client.Blocking = false;
                await client.ConnectAsync(ipEndPoint, Cts.Token);
                Log.Information(
                    $"{ClientName}.Run connect ok {client.LocalEndPoint} -> {client.RemoteEndPoint}");

                // 创建管道
                var pipe = new Pipe();
                PacketPipeReader pipeReader = new(pipe.Reader, Cts.Token);
                _wQueue.Clear();

                // reg
                SendReg();

                wTask = Task.Factory.StartNew(async () => await WriteSendMsg(client),
                    TaskCreationOptions.LongRunning);
                heartTask = Task.Factory.StartNew(async () => await Heartbeat(client),
                    TaskCreationOptions.LongRunning);

                while (!Cts.IsCancellationRequested)
                {
                    var rlen = client.Available;
                    if (rlen == 0) rlen = 1024 * 4;

                    var memory = pipe.Writer.GetMemory(rlen);
                    var bytesRead = await client.ReceiveAsync(memory, SocketFlags.None, Cts.Token);
                    if (bytesRead == 0)
                    {
                        await Task.Delay(10);
                    }
                    else
                    {
                        pipe.Writer.Advance(bytesRead);
                        await pipe.Writer.FlushAsync(Cts.Token);

                        if (!await pipeReader.Read(OnRecv))
                        {
                            break;
                        }
                    }
                }

                wTask.Wait();
                heartTask.Wait();
            }
            catch (Exception ex)
            {
                Log.Error($"{ClientName}.Run ex={ex.Message}");
            }
            finally
            {
                wTask?.Wait();
                heartTask?.Wait();
                ClientSocket?.Dispose();
            }
        }

        Log.Information($"{ClientName}.Run exit");
    }

    private void OnRecv(int proto, ReadOnlySequence<byte> data)
    {
        OnRecvCallback.Invoke(proto, data);
    }

    private void SendReg()
    {
        ServerReg regMsg = new();
        regMsg.ServerId = ServerService.Instance.ServerId;
        regMsg.ServerName = ServerService.Instance.ServerName;
        regMsg.ServerCluster = ServerService.Instance.ServerCluster;
        regMsg.ServerType = ServerService.Instance.ServerType;
        regMsg.Host = GameUtil.HostIp();
        regMsg.Port = ServerService.Instance.ServerPort;

        DataPacket pack = new((int)ServerProto.Reg);
        pack.WriteByteArray(regMsg.ToByteArray());

        _wQueue.Enqueue(pack);
    }

    private async Task WriteSendMsg(Socket client)
    {
        var localEp = client.LocalEndPoint;
        var remoteEp = client.RemoteEndPoint;

        try
        {
            client.SendTimeout = 10 * 1000;
            while (!Cts.IsCancellationRequested)
            {
                if (ClientSocket == null || ClientSocket != client) break;

                if (_wQueue.Count > 0 && _wQueue.TryDequeue(out var pack))
                {
                    await client.SendAsync(pack.ToPackBytes(), SocketFlags.None, Cts.Token);
                }
                else
                {
                    await Task.Delay(10, Cts.Token); // ms
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{ClientName}.WriteSendMsg ex={ex.Message}");
        }

        Log.Error($"{ClientName}.WriteSendMsg exit client={localEp} -> {remoteEp}");
    }

    private async Task Heartbeat(Socket client)
    {
        var localEp = client.LocalEndPoint;
        var remoteEp = client.RemoteEndPoint;
        var pack = new DataPacket((int)ServerProto.Heart);

        while (!Cts.IsCancellationRequested)
        {
            if (ClientSocket == null || ClientSocket != client ||
                ClientSocket.LocalEndPoint != client.LocalEndPoint)
                break;

            await Task.Delay(10 * 1000);

            _wQueue.Enqueue(pack);
        }

        Log.Information($"{ClientName}.Heartbeat exit client={localEp} -> {remoteEp}");
    }

    public void Send(DataPacket pack)
    {
        _wQueue.Enqueue(pack);
    }
}