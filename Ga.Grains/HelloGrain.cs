using Ga.GrainInterface;
using Serilog;

namespace Ga.Grains;

public class HelloGrain : Grain, IHelloGrain
{
    public Task<string> Hello(string greeting)
    {
        Log.Information($"Hello: {greeting}");
        return Task.FromResult("Hello " + greeting);
    }
}