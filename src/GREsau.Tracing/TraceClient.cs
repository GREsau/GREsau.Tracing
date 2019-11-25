using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace GREsau.Tracing
{
    public class TraceClient
    {
        // Copied from https://github.com/dotnet/diagnostics/blob/8ccd54cde5a7c671c44051336ee5c2500f6aff19/src/Tools/dotnet-trace/CommandLine/Commands/ListProfilesCommandHandler.cs#L46
        private static readonly Provider[] CpuSamplingProviders = new[] {
            new Provider("Microsoft-DotNETCore-SampleProfiler"),
            new Provider("Microsoft-Windows-DotNETRuntime", (ulong)ClrTraceEventParser.Keywords.Default, EventLevel.Informational),
        };

        public SessionConfiguration SessionConfiguration { get; }
        public int ProcessId { get; }

        public TraceClient(uint circularBufferSizeMb = 256, int? processId = null)
            : this(CpuSamplingProviders, circularBufferSizeMb, processId)
        {
        }

        public TraceClient(IReadOnlyCollection<Provider> providers, uint circularBufferSizeMb = 256, int? processId = null)
            : this(new SessionConfiguration(circularBufferSizeMb, EventPipeSerializationFormat.NetTrace, providers), processId)
        {
        }

        public TraceClient(SessionConfiguration sessionConfiguration, int? processId = null)
        {
            SessionConfiguration = sessionConfiguration;
            ProcessId = processId ?? GetCurrentProcessId();
        }

        public async Task CollectAsync(string outFilePath, TimeSpan duration)
        {
            using var cts = new CancellationTokenSource(duration);
            await CollectAsync(outFilePath, cts.Token);
        }

        public async Task CollectAsync(string outFilePath, CancellationToken ct)
        {
            var processId = GetCurrentProcessId();
            using var traceStream = EventPipeClient.CollectTracing(processId, SessionConfiguration, out var sessionId);

            if (sessionId == 0)
            {
                throw new Exception("Unable to create trace session.");
            }

            var stoppedReading = false;
            var buffer = new byte[16 * 1024];
            using var fileStream = new FileStream(outFilePath, FileMode.Create, FileAccess.Write);

            // If ct is already cancelled, then ct.Register will run its callback synchronously.
            // Calling StopTracing synchronously here would deadlock as there would be nothing to
            // consume traceStream - to avoid this, we call StopTracing from a different thread.
            ct.Register(() => Task.Run(() =>
            {
                if (!stoppedReading)
                {
                    EventPipeClient.StopTracing(processId, sessionId);
                }
            }));

            try
            {
                while (true)
                {
                    var nBytesRead = await traceStream.ReadAsync(buffer, 0, buffer.Length);
                    if (nBytesRead == 0)
                    {
                        break;
                    }
                    fileStream.Write(buffer, 0, nBytesRead);
                }
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
