using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;

namespace GREsau.Tracing.BasicTrace
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option(
                    new [] { "-p", "--process"},
                    $"Required. The ID or name of the process to collect the trace from.")
                {
                    Argument = new Argument<string>("processIdOrName"),
                    Required = true
                },
                new Option(
                    new [] { "-o", "--output"},
                    $"The output path for the collected trace data. Defaults to 'trace.nettrace'.")
                {
                    Argument = new Argument<string>("file", "trace.nettrace")
                },
                new Option(
                    "--buffersize",
                    "Sets the size of the in-memory circular buffer in megabytes. Defaults to 256 MB.")
                {
                    Argument = new Argument<int>("size", 256)
                },
            };

            rootCommand.Handler = CommandHandler.Create<string, string, int, CancellationToken>(StartTrace);

            return rootCommand.InvokeAsync(args);
        }

        private static async Task<int> StartTrace(string process, string output, int bufferSize, CancellationToken ct)
        {
            int pid;
            string processName;
            try
            {
                (pid, processName) = GetProcessInfo(process);
            }
            catch (ArgumentException e)
            {
                await Console.Error.WriteLineAsync(e.Message);
                return 1;
            }

            var client = new TraceClient(pid, bufferSize);

            Console.WriteLine($"Starting trace of process {pid} ({processName}) - press Ctrl+C to stop...");
            await client.CollectAsync(output, ct);

            Console.WriteLine($"Trace written to '{output}'.");
            return 0;
        }

        private static (int pid, string processName) GetProcessInfo(string processIdOrName)
        {
            if (string.IsNullOrWhiteSpace(processIdOrName))
            {
                throw new ArgumentException("Missing process ID/name.");
            }

            if (int.TryParse(processIdOrName, out var parsedId))
            {
                try
                {
                    using var process = System.Diagnostics.Process.GetProcessById(parsedId);
                    return (parsedId, process.ProcessName);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"Could not find process with ID '{parsedId}'.");
                }
            }

            foreach (var pid in DiagnosticsClient.GetPublishedProcesses())
            {
                try
                {
                    using var process = System.Diagnostics.Process.GetProcessById(pid);
                    if (process.ProcessName.Contains(processIdOrName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return (pid, process.ProcessName);
                    }
                }
                catch (ArgumentException)
                {
                    // ignore - just try the next process
                }
            }

            throw new ArgumentException($"Could not find process with name '{processIdOrName}'.");
        }
    }
}
