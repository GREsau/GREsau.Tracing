using System;
using System.Threading;
using System.Threading.Tasks;
using GREsau.Tracing;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var stuffTask = DoStuff(cts.Token);

            var client = new TraceClient();
            var traceTask = client.CollectAsync("trace.nettrace", cts.Token);

            await Task.WhenAll(stuffTask, traceTask);
        }

        static async Task DoStuff(CancellationToken ct)
        {
            var counter = 0;
            while (!ct.IsCancellationRequested)
            {
                Console.WriteLine($"Hello world! {counter++}");
                await Task.Delay(10);
            }
        }
    }
}
