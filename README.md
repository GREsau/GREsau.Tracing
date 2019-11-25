# GREsau.Tracing
GREsau.Tracing is a simplified alternative to [dotnet-trace](https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md), consumable as a C# library rather than a CLI tool.

GREsau.Tracing will only output `nettrace`-format files. These can be viewed on Windows using [PerfView](https://github.com/microsoft/perfview), or converted to `speedscope`-format using dotnet-trace's convert command, and then viewed at https://www.speedscope.app.
```sh
# Converts nettrace file to speedscope, writing to trace.speedscope.json
$ dotnet-trace convert --format Speedscope trace.nettrace
```

## Basic Usage

To trace the current process for a period of time, saving the file as "trace.nettrace":

```csharp
var client = new TraceClient();
await client.CollectAsync("trace.nettrace", TimeSpan.FromMinutes(1));
```

You can also trace a different process by passing in its ID:

```csharp
var client = new TraceClient(processId: 123);
await client.CollectAsync("trace.nettrace", TimeSpan.FromMinutes(1));
```

By default, `TraceClient` will track CPU usage and general .NET runtime information, equivalent to running dotnet-trace with the cpu-sampling (default) profile. If you want to trace something different, you can pass in a collection of `Microsoft.Diagnostics.Tools.RuntimeClient.Provider`s:

```csharp
var gcCollectProvider = new Provider(
    "Microsoft-Windows-DotNETRuntime",
    (ulong)ClrTraceEventParser.Keywords.GC | (ulong)ClrTraceEventParser.Keywords.Exception,
    EventLevel.Informational);
var client = new TraceClient(new[] { gcCollectProvider });
await client.CollectAsync("trace.nettrace", TimeSpan.FromMinutes(1));
```