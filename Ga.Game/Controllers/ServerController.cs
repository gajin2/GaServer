using Microsoft.AspNetCore.Mvc;

namespace Ga.Game.Controllers;

[Route("/")]
[ApiController]
public class ServerController : ControllerBase
{
    private readonly IGrainFactory _grains;

    public ServerController(IGrainFactory grainFactory)
    {
        _grains = grainFactory;
    }

    public async Task GetWs()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            int netId = ClientService.Instance.GetNetId();
            if (netId < 0)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var session = new ClientSession(webSocket, _grains);
            session.NetId = netId;
            session.RemoteAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            ClientService.Instance.Add(session);

            await session.Run();

            ClientService.Instance.Remove(session);
            ClientService.Instance.RetNetId(netId);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}