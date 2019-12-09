# GREsau.Tracing
GREsau.Tracing is a simplified alternative to [dotnet-trace](https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md), consumable as a C# library rather than a CLI tool.

GREsau.Tracing will only output `nettrace`-format files. These can be viewed on Windows using [PerfView](https://github.com/microsoft/perfview), or converted to `speedscope`-format using dotnet-trace's convert command, and then viewed at https://www.speedscope.app.
```sh
# Converts nettrace file to speedscope, writing to trace.speedscope.json
> dotnet-trace convert --format Speedscope trace.nettrace
```

This library makes use of [Microsoft.Diagnostics.NETCore.Client](https://www.nuget.org/packages/Microsoft.Diagnostics.NETCore.Client). If you're looking for something more powerful/flexible and you're willing to accept the complexity that comes with that, consider using that library directly.

## Installing
Install via NuGet:
```sh
> dotnet add package GREsau.Tracing
```

## Basic Usage

To trace the current process for a period of time, saving the output file as "trace.nettrace":

```csharp
var client = new TraceClient();
await client.CollectAsync("trace.nettrace", TimeSpan.FromMinutes(1));
```

You can also trace a different process by passing in its ID:

```csharp
var client = new TraceClient(123);
await client.CollectAsync("trace.nettrace", TimeSpan.FromMinutes(1));
```

By default, `TraceClient` will track CPU usage and general .NET runtime information, equivalent to running dotnet-trace with the cpu-sampling (default) profile. If you want to trace something different, you can pass in a collection of `Microsoft.Diagnostics.NETCore.Client.EventPipeProvider`s:

```csharp
var gcCollectProvider = new EventPipeProvider(
    "Microsoft-Windows-DotNETRuntime",
    EventLevel.Informational,
    (long)(ClrTraceEventParser.Keywords.GC | ClrTraceEventParser.Keywords.Exception));
var client = new TraceClient(providers: new[] { gcCollectProvider });
await client.CollectAsync("trace.nettrace", TimeSpan.FromMinutes(1));
```