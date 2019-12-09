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
    /// <summary>Use an instance of this class to trace .NET Core processes.</summary>
    public class TraceClient
    {
        // Copied from https://github.com/dotnet/diagnostics/blob/125ea40662c36fa49e1d742e44154fd9a5b0a4d1/src/Tools/dotnet-trace/CommandLine/Commands/ListProfilesCommandHandler.cs#L46
        private static readonly EventPipeProvider[] CpuSamplingProviders = new[] {
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
        };

        /// <summary>The ID of the process to trace.</summary>
        public int ProcessId { get; }

        /// <summary>The size of the in-memory circular buffer in megabytes.</summary>
        public int CircularBufferSizeMb { get; }

        /// <summary>The EventPipe providers to be enabled in the trace.</summary>
        public IReadOnlyCollection<EventPipeProvider> Providers { get; }

        /// <summary>Creates a new TraceClient.</summary>
        /// <param name="processId">The ID of the process to trace. If null/omitted, the current process will be traced.</param>
        /// <param name="circularBufferSizeMb">The size of the in-memory circular buffer in megabytes. Defaults to 256MB.</param>
        /// <param name="providers">The EventPipe providers to be enabled in the trace. If null/omitted, default providers to track CPU usage and general .NET runtime info will be enabled.</param>
        public TraceClient(int? processId = null, int circularBufferSizeMb = 256, IReadOnlyCollection<EventPipeProvider> providers = null)
        {
            ProcessId = processId ?? GetCurrentProcessId();
            CircularBufferSizeMb = circularBufferSizeMb;
            Providers = providers ?? CpuSamplingProviders;
        }

        /// <summary>Starts a trace, stopping after a period of time.</summary>
        /// <param name="outFilePath">The path to save the nettrace file at.</param>
        /// <param name="duration">How long to perform the trace.</param>
        public async Task CollectAsync(string outFilePath, TimeSpan duration)
        {
            using var cts = new CancellationTokenSource(duration);
            await CollectAsync(outFilePath, cts.Token);
        }

        /// <summary>Starts a trace, stopping when the given `CancellationToken` is cancelled.</summary>
        /// <param name="outFilePath">The path to save the nettrace file at.</param>
        /// <param name="ct">The token which will stop the trace when cancelled.</param>
        public async Task CollectAsync(string outFilePath, CancellationToken ct)
        {
            using var fileStream = new FileStream(outFilePath, FileMode.Create, FileAccess.Write);
            await CollectAsync(fileStream, ct);
        }

        /// <summary>Starts a trace, stopping after a period of time.</summary>
        /// <param name="outStream">The stream to write the nettrace file to.</param>
        /// <param name="duration">How long to perform the trace.</param>
        public async Task CollectAsync(Stream outStream, TimeSpan duration)
        {
            using var cts = new CancellationTokenSource(duration);
            await CollectAsync(outStream, cts.Token);
        }

        /// <summary>Starts a trace, stopping when the given `CancellationToken` is cancelled.</summary>
        /// <param name="outStream">The stream to write the nettrace file to.</param>
        /// <param name="ct">The token which will stop the trace when cancelled.</param>
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
