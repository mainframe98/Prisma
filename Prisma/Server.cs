using System;
using System.Net;
using Prisma.Config;
using Prisma.Exceptions;
using Serilog;

namespace Prisma;

public sealed class Server : IDisposable
{
    private readonly ServerConfig _config;
    private readonly ILogger _logger;
    private readonly RequestHandler _handler;
    private readonly HttpListener _listener;

    public Server(ServerConfig config, ILogger logger)
    {
        this._config = config;
        this._logger = logger;
        this._handler = new(this._config, this._logger);
        this._listener = new();

        foreach (string prefix in this._config.ListenerPrefixes)
        {
            this._listener.Prefixes.Add(prefix);
        }
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    /// <exception cref="SetupException"></exception>
    /// <exception cref="HttpListenerException"></exception>
    public void Start()
    {
        this._handler.InitializeHandlers();
        this._listener.Start();

        this._logger.Information("Listening to {Url}...", string.Join(", ", this._config.ListenerPrefixes));

        this._handler.Handle(this._listener);
    }

    public void Dispose()
    {
        this._listener.Stop();
        this._handler.Dispose();
        ((IDisposable)this._listener).Dispose();
    }
}
