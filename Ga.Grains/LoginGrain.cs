using Ga.Common;
using Ga.GrainInterface;
using Serilog;

namespace Ga.Grains;

public class LoginGrain : Grain, ILoginGrain
{
    public Task Handle(GrainRequest req, ISessionObserver observer)
    {
        Log.Debug($"LoginGrain Handle proto={req.Proto} netid={req.NetId}");

        switch (req.Proto)
        {
            case (int)Pb.LoginProto.ReqHello:
                return OnHello(req, observer);
            case (int)Pb.LoginProto.ReqPing:
                return OnPing(req, observer);
            default:
                Log.Error($"LoginGrain.Handle NOT handle proto={req.Proto} netid={req.NetId}");
                break;
        }

        return Task.CompletedTask;
    }

    private async Task OnPing(GrainRequest req, ISessionObserver observer)
    {
        var pbMsg = Pb.ReqPing.Parser.ParseFrom(req.Payload);
        var reply = new Pb.RspPong();
        reply.Msg = pbMsg.Msg + Guid.NewGuid().ToString("N");
        await observer.ToClientMsg((int)Pb.LoginProto.RspPong, reply);
    }

    private async Task OnHello(GrainRequest req, ISessionObserver observer)
    {
        var pbMsg = Pb.ReqHello.Parser.ParseFrom(req.Payload);
        Log.Debug($"OnHello: {pbMsg.Msg}");
        var helloGrain = GrainFactory.GetGrain<IHelloGrain>(req.NetId);
        string msg = await helloGrain.Hello(pbMsg.Msg);
        Log.Debug($"OnHello: msg={msg}");

        var reply = new Pb.RspHello();
        reply.Msg = msg;
        await observer.ToClientMsg((int)Pb.LoginProto.RspHello, reply);
    }
}