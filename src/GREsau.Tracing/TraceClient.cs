using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace GREsau.Tracing
{
    public class TraceClient
    {
        // Copied from https://github.com/dotnet/diagnostics/blob/125ea40662c36fa49e1d742e44154fd9a5b0a4d1/src/Tools/dotnet-trace/CommandLine/Commands/ListProfilesCommandHandler.cs#L46
        private static readonly EventPipeProvider[] CpuSamplingProviders = new[] {
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
        };

        public int ProcessId { get; }
        public int CircularBufferSizeMb { get; }
        public IReadOnlyCollection<EventPipeProvider> Providers { get; }

        public TraceClient(int? processId = null, int circularBufferSizeMb = 256, IReadOnlyCollection<EventPipeProvider> providers = null)
        {
            ProcessId = processId ?? GetCurrentProcessId();
            CircularBufferSizeMb = circularBufferSizeMb;
            Providers = providers ?? CpuSamplingProviders;
        }

        public async Task CollectAsync(string outFilePath, TimeSpan duration)
        {
            using var cts = new CancellationTokenSource(duration);
            await CollectAsync(outFilePath, cts.Token);
        }

        public async Task CollectAsync(string outFilePath, CancellationToken ct)
        {
            using var fileStream = new FileStream(outFilePath, FileMode.Create, FileAccess.Write);
            await CollectAsync(fileStream, ct);
        }

        public async Task CollectAsync(Stream outStream, TimeSpan duration)
        {
            using var cts = new CancellationTokenSource(duration);
            await CollectAsync(outStream, cts.Token);
        }

        public async Task CollectAsync(Stream outStream, CancellationToken ct)
        {
            var diagnosticsClient = new DiagnosticsClient(ProcessId);
            using var session = diagnosticsClient.StartEventPipeSession(Providers, circularBufferMB: CircularBufferSizeMb);

            var stoppedReading = false;

            // If ct is already cancelled, then ct.Register will run its callback synchronously.
            // Calling Stop synchronously here would deadlock as there would be nothing to
            // consume traceStream - to avoid this, we call Stop from a different thread.
            // TODO is this is still necessary now we're using the new diagnostics client?
            ct.Register(() => Task.Run(() =>
            {
                if (!stoppedReading)
                {
                    session.Stop();
                }
            }));

            try
            {
                await session.EventStream.CopyToAsync(outStream);
            }
            finally
            {
                stoppedReading = true;
            }
        }

        private static int GetCurrentProcessId()
        {
            using var process = Process.GetCurrentProcess();
            return process.Id;
        }
    }
}
