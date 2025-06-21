using Ga.Common;
using Orleans.Concurrency;

namespace Ga.GrainInterface;

public interface ILoginGrain : IGrainWithStringKey
{
    [OneWay]
    Task Handle(GrainRequest req, ISessionObserver observer);
}