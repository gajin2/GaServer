using System.Collections.Concurrent;
using Serilog;

namespace Ga.Common;


public class EngineBase<T>
{
    private bool Running { get; set; }
    private Thread? RunThread { get; set; }
    private readonly ConcurrentQueue<T> _messages = new();

    public virtual bool Init()
    {
        return true;
    }

    public void Start()
    {
        Log.Information("Engine.Start");
        Running = true;
        RunThread = new Thread(this.Run);
        RunThread.Start();

        OnStart();
    }

    protected virtual void OnStart()
    {
    }

    public void Stop()
    {
        Log.Information("Engine.Stop");
        Running = false;
        RunThread?.Join();

        OnStop();
    }

    protected virtual void OnStop()
    {
    }

    private void Run()
    {
        while (Running)
        {
            if (_messages.TryGetNonEnumeratedCount(out int count))
            {
                for (int i = 0; i < count; i++)
                {
                    if (_messages.TryDequeue(out var item))
                    {
                        OnMessage(item);
                    }
                }
            }

            Thread.Sleep(10);
        }
    }

    protected virtual void OnMessage(T msg)
    {
    }

    public void Post(T msg)
    {
        _messages.Enqueue(msg);
    }
}