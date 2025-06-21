using Ga.Common;
using Orleans.Concurrency;

namespace Ga.GrainInterface;

public interface ISessionObserver : IGrainObserver
{
    [OneWay]
    Task ToClientMsg(byte[] msg);

    [OneWay]
    Task ToClientMsg(int proto, byte[] body)
    {
        DataPacket pack = new(proto);
        pack.WriteByteArray(body);

        return ToClientMsg(pack.ToBytes());
    }
}