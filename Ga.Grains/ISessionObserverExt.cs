using Ga.Common;
using Ga.GrainInterface;
using Google.Protobuf;

namespace Ga.Grains;

public static class SessionObserverExt
{
    public static Task ToClientMsg(this ISessionObserver obs, int proto, Google.Protobuf.IMessage msg)
    {
        DataPacket pack = new(proto);
        pack.WriteByteArray(msg.ToByteArray());
        return obs.ToClientMsg(pack.ToBytes());
    }
}