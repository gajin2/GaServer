using Ga.GrainInterface;
using Serilog;

namespace Ga.Game;

public sealed class SessionObserver : ISessionObserver
{
    ClientSession _session;

    public SessionObserver(ClientSession clientSession)
    {
        _session = clientSession;
    }

    public Task ToClientMsg(byte[] msg)
    {
        Log.Debug($"ToClientMsg msg len={msg.Length}");
        _session.Send(msg);
        return Task.CompletedTask;
    }
}