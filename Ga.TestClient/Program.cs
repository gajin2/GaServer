using System.Diagnostics;
using System.Net.WebSockets;

namespace Ga.TestClient;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                break;
            }

            if (input == "1")
            {
                await new TestHello().Run();
            }
            else if (input == "2")
            {
                Stopwatch sw = Stopwatch.StartNew();
                await new TestPing().Run();
                sw.Stop();
                Console.WriteLine($"{sw.ElapsedMilliseconds}ms");
            }
        }
    }
}