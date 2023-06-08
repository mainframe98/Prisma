using System;
using System.Linq;
using System.Net;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Prisma.Config;

public class LogConfig : ICloneable
{
    /// <summary>
    /// Path to a log file.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Minimum log level to display.
    /// </summary>
    public LogEventLevel Level { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Log to the log, specified in <see cref="Path"/>, in JSON
    /// </summary>
    public bool LogAsJson { get; set; } = false;

    /// <summary>
    /// Construct <see cref="LoggerConfiguration"/> for this instance.
    /// </summary>
    /// <returns></returns>
    public LoggerConfiguration MakeLoggerConfiguration()
    {
        LoggerConfiguration logConfig = new LoggerConfiguration()
            .MinimumLevel.Is(this.Level)
            .WriteTo.Debug()
            .Destructure.ByTransforming<HttpListenerRequest>(r => new
            {
                Headers = r.Headers
                    .Cast<string?>()
                    .Where(h => h != null && r.Headers[h] != null)
                    .ToDictionary(h => h, h => r.Headers[h]),
                Method = r.HttpMethod,
                ContentLength = r.ContentLength64,
                HostAddress = r.UserHostAddress,
                Url = r.Url
            });

        if (this.Path == "")
        {
            return logConfig;
        }

        if (this.LogAsJson)
        {
            logConfig.WriteTo.File(new JsonFormatter(renderMessage: true), this.Path);
        }
        else
        {
            logConfig.WriteTo.File(this.Path);
        }

        return logConfig;
    }

    public object Clone() => this.MemberwiseClone();
}