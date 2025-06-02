using System.Buffers;
using Ga.Common;
using Pb;

namespace Ga.Center;

public sealed class ServerHandler : SessionHandler
{
    public override void OnRecv(NetSession session, int proto, ReadOnlySequence<byte> buffer)
    {
    }

    public override void OnConnect(NetSession session, ServerReg pbMsg)
    {
    }

    public override void OnDisconnect(NetSession session)
    {
    }
}