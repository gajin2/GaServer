using System.Net.WebSockets;
using Ga.Common;
using Google.Protobuf;

namespace Ga.TestClient;

public class TestHello
{
    public async Task Run()
    {
        Console.Write("Input msg:");
        string? msg = Console.ReadLine();

        // 创建 WebSocket 客户端
        using var ws = new ClientWebSocket();

        // 设置要连接的 WebSocket 服务器的 URI
        Uri serverUri = new Uri("ws://localhost:9001/");

        // 连接到 WebSocket 服务器
        await ws.ConnectAsync(serverUri, CancellationToken.None);
        Console.WriteLine("已连接到 WebSocket 服务器");

        // 发送消息到服务器
        await SendMessage(ws, msg);

        // 启动接收消息的任务
        await ReceiveMessages(ws);

        await ws.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);

        // 等待用户输入以关闭程序
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
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
                Console.WriteLine($"body len={body.Length}");
                var pbMsg = Pb.RspHello.Parser.ParseFrom(body[4..]);
                Console.WriteLine(pbMsg.Msg);
            }


            break;
        }
    }

    async Task SendMessage(ClientWebSocket ws, string? msg)
    {
        if (string.IsNullOrEmpty(msg))
        {
            msg = "Test";
        }

        var pbMsg = new Pb.ReqHello();
        pbMsg.Msg = msg;
        var pack = new DataPacket((int)Pb.LoginProto.ReqHello);
        pack.WriteByteArray(pbMsg.ToByteArray());

        await ws.SendAsync(pack.ToBytes(), WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}