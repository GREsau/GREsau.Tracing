# basic-trace
basic-trace is a tool for basic performance tracing of .NET Core 3 applications. It is a simplified alternative to [dotnet-trace](https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md) except that it also works with redirected stdin/stdout, so you can also run it without a console attached. When dotnet-trace can be run without a console, basic-trace will be practically obsolete.

basic-trace will only output `nettrace`-format files. These can be viewed on Windows using [PerfView](https://github.com/microsoft/perfview), or converted to `speedscope`-format using dotnet-trace's convert command, and then viewed at https://www.speedscope.app.
```sh
# Converts nettrace file to speedscope, writing to trace.speedscope.json
> dotnet-trace convert --format Speedscope trace.nettrace
```

## Installing
Install via NuGet:
```sh
> dotnet tool install --global basic-trace
```

## Basic Usage

To perform a trace of your app, run:

```sh
> basic-trace -p Contoso.MyApp
```

`-p`/`--process` takes either the PID or the name of the process that you want to trace.

This will write the trace to a file called "trace.nettrace" in the working directory - this can be overridden with the `-o`/`--output` option:

```sh
> basic-trace -p Contoso.MyApp -o "../foo.nettrace"
```
