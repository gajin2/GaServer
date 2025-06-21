using Orleans.Concurrency;

namespace Ga.GrainInterface;

public interface IHelloGrain : IGrainWithIntegerKey
{
    Task<string> Hello(string greeting);
}