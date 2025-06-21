using System.Net.WebSockets;
using Ga.Common;
using Google.Protobuf;

namespace Ga.TestClient;

public class TestPing
{
    public async Task Run()
    {
        // 创建 WebSocket 客户端
        using var ws = new ClientWebSocket();

        // 设置要连接的 WebSocket 服务器的 URI
        Uri serverUri = new Uri("ws://localhost:9001/");

        // 连接到 WebSocket 服务器
        await ws.ConnectAsync(serverUri, CancellationToken.None);

        const int k = 1000000;
        for (int i = 0; i < k; i++)
        {
            // 发送消息到服务器
            await SendMessage(ws);
            // 启动接收消息的任务
            await ReceiveMessages(ws);
        }

        await ws.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
    }


    async Task ReceiveMessages(ClientWebSocket ws)
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);

        while (ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (buffer.Array != null)
                    {
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                var body = ms.ToArray();
                // Console.WriteLine($"body len={body.Length}");
                Pb.RspPong.Parser.ParseFrom(body[4..]);
                // Console.WriteLine(pbMsg.Msg);
            }

            break;
        }
    }

    async Task SendMessage(ClientWebSocket ws)
    {
        string msg = Guid.NewGuid().ToString("N");

        var pbMsg = new Pb.ReqPing();
        pbMsg.Msg = msg;
        var pack = new DataPacket((int)Pb.LoginProto.ReqPing);
        pack.WriteByteArray(pbMsg.ToByteArray());

        await ws.SendAsync(pack.ToBytes(), WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}