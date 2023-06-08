using System;
using System.IO;
using System.Net;
using Serilog;

namespace Prisma;

public class Request
{
    /// <summary>
    /// Original request provided by <see cref="HttpListener"/>.
    /// </summary>
    public HttpListenerRequest OriginalRequest { get; }

    /// <summary>
    /// Url as rewritten by the rewrite rule.
    /// </summary>
    public Uri RewrittenUrl { get; internal set; }

    /// <summary>
    /// Path info.
    /// </summary>
    public string PathInfo { get; internal set; }

    /// <summary>
    /// Logger for event related to this specific request.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Extension of the document requested.
    /// </summary>
    public string RequestExtension => Path.HasExtension(this.RewrittenUrl.AbsolutePath) ? Path.GetExtension(this.RewrittenUrl.AbsolutePath).TrimStart('.') : "";

    /// <summary>
    /// Unique Id to distinguish requests.
    /// </summary>
    public string UniqueId { get; }

    internal Request(HttpListenerRequest originalRequest, ILogger logger)
    {
        this.OriginalRequest = originalRequest;
        this.RewrittenUrl = originalRequest.Url!;
        this.PathInfo = "";
        this.Logger = logger.ForContext("Request", this.ToString());
        this.UniqueId = Guid.NewGuid().ToString();
    }

    /// <inheritdoc cref="object.ToString"/>
    public sealed override string ToString()
    {
        return $"{this.UniqueId}: [{this.OriginalRequest.HttpMethod}] {this.OriginalRequest.RawUrl}";
    }
}
