using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Prisma.Config;
using Prisma.DocumentHandlers;
using Serilog;

namespace Prisma
{
    public class RequestHandler : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ServerConfig _config;
        private readonly DocumentHandler _standardDocumentHandler;
        private readonly Dictionary<string, DocumentHandler> _handlers;

        public RequestHandler(ServerConfig config, ILogger logger)
        {
            this._config = config;
            this._standardDocumentHandler = new StandardDocumentHandler(this._config, logger);
            this._handlers = new Dictionary<string, DocumentHandler>();
            this._logger = logger;
        }

        /// <summary>
        /// Do initialization work that shouldn't happen inside the constructor.
        /// </summary>
        public void InitializeHandlers()
        {
            List<string> usedHandlers = this._config.InvokeOnExtension.Values.Concat(this._config.InvokeOnPath.Values).ToList();

            foreach ((string application, ApplicationConfig cgiConfig) in this._config.CgiApplications)
            {
                this._handlers[application] = new CgiHandler(this._config, this._logger.ForContext("Application", application), cgiConfig)
                {
                    LoggableName = application
                };
            }

            foreach ((string application, FastCgiApplicationConfig fastCgiConfig) in this._config.FastCgiApplications.Where(p => usedHandlers.Contains(p.Key)))
            {
                this._handlers[application] = new FastCgiHandler(this._config, this._logger.ForContext("Application", application), fastCgiConfig)
                {
                    LoggableName = application
                };
            }

            foreach ((string _, DocumentHandler handler) in this._handlers)
            {
                handler.Initialize();
            }
        }

        /// <summary>
        /// Handler for <see cref="HttpListener"/> requests.
        /// </summary>
        /// <param name="result"></param>
        private void Handle(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState!;
            if (!listener.IsListening)
            {
                this._logger.Debug("{HttpListener} is not listening!", nameof(HttpListener));
                return;
            }

            HttpListenerContext context = listener.EndGetContext(result);
            listener.BeginGetContext(Handle, listener);
            // Somewhere this gets set to true.
            context.Response.SendChunked = false;

            Request request = new(context.Request, this._logger)
            {
                RewrittenUrl = this.RewritePath(context.Request.Url)
            };

            this._logger.Information("Incoming request: {Request}", request);

            DocumentHandler handler = this.FindHandler(request);
            request.Logger.Debug("This request is handled by {HandlerType}", handler.LoggableName);
            request.Logger.Debug(".NET HttpListenerRequest: {@Properties}", request.OriginalRequest);

            Stopwatch? stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                handler.HandleRequest(request, context.Response);
            }
            catch (Exception e)
            {
                request.Logger.Error(e, "Request handling failed");
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                try
                {
                    this._standardDocumentHandler.WriteToResponse($@"
<html>
    <head>
        <title>Internal error</title>
    </head>
    <body>
        <h1>Internal error</h1>
        <p>{e.Message}</p>
        <pre>{e.StackTrace}</pre>
    </body>
</html>", context.Response);
                }
                catch (InvalidOperationException ioe)
                {
                   this._logger.Warning(ioe, "Cannot write 500 error");
                }
            }
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    request.Logger.Information(
                        "{RequestUniqueId} took {ElapsedMilliseconds}ms",
                        request.UniqueId,
                        stopwatch.ElapsedMilliseconds.ToString()
                    );
                }
            }

            context.Response.Close();
        }

        /// <summary>
        /// Find the handler for the given request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private DocumentHandler FindHandler(Request request)
        {
            if (this._config.EnableCgiBin && request.RewrittenUrl.LocalPath.StartsWith("/cgi-bin/"))
            {
                this.RewriteForPathInfo(request);
                string path = Path.Combine(this._config.DocumentRoot, request.RewrittenUrl.LocalPath.TrimStart('/'));

                if (!File.Exists(path))
                {
                    // The standard document handler also serves as 404 handler.
                    return this._standardDocumentHandler;
                }

                return new CgiHandler(this._config, this._logger.ForContext("CGI script", path), new ApplicationConfig
                {
                    Path = path
                });
            }

            foreach ((string path, string application) in this._config.InvokeOnPath)
            {
                if (request.RewrittenUrl.LocalPath.StartsWith(path))
                {
                   return this._handlers[application];
                }
            }

            if (this._config.AllowPathInfo)
            {
                this.RewriteForPathInfo(request);
            }

            string localPath = Path.Combine(this._config.DocumentRoot, request.RewrittenUrl.LocalPath.TrimStart('/'));

            DocumentHandler? handler = this.HandlerFromExtension(request, localPath);

            if (handler == null && Directory.Exists(localPath))
            {
                foreach (string defaultFile in this._config.DefaultDocuments)
                {
                    if (File.Exists(Path.Combine(localPath, defaultFile)))
                    {
                        UriBuilder uriBuilder = new(request.RewrittenUrl);
                        // Paths might not have a trailing slash.
                        uriBuilder.Path = uriBuilder.Path.TrimStart('/') + '/' + defaultFile;

                        request.RewrittenUrl = uriBuilder.Uri;

                        return this.HandlerFromExtension(request, localPath) ?? this._standardDocumentHandler;
                    }
                }
            }

            return handler ?? this._standardDocumentHandler;
        }

        /// <summary>
        /// Find a handler based on the extension of the requested document.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="localPath"></param>
        /// <returns>DocumentHandler, or null if no matching handler could be found</returns>
        private DocumentHandler? HandlerFromExtension(Request request, string localPath)
        {
            if (this._config.TreatCgiExtensionScriptsAsCgiScripts && request.RequestExtension == "cgi")
            {
                return new CgiHandler(this._config, this._logger.ForContext("CGI script", localPath), new ApplicationConfig
                {
                    Path = localPath
                });
            }

            if (request.RequestExtension != "")
            {
                foreach ((string extension, string application) in this._config.InvokeOnExtension)
                {
                    if (request.RequestExtension == extension)
                    {
                        return this._handlers[application];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Rewrite the given url if it matches a rewrite rule.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private Uri RewritePath(Uri url)
        {
            UriBuilder rewritten = new(url);

            foreach ((Regex regex, string replacement) in this._config.RewriteRules)
            {
                Match m = regex.Match(url.LocalPath);

                if (m.Success)
                {
                    // string.Format takes a params object[].
                    // Type this variable with object as T to prevent a compiler warning.
                    List<object> replacements = new();

                    foreach (Group? group in m.Groups.Skip<Group?>(1))
                    {
                        if (group?.Captures[0].Value != null)
                        {
                            replacements.Add(group.Captures[0].Value);
                        }
                    }

                    string newPath = string.Format(replacement, replacements.ToArray());

                    if (newPath == "" || newPath[0] != '/')
                    {
                        newPath = '/' + newPath;
                    }

                    int queryStringQuestionMark = newPath.IndexOf('?');
                    if (queryStringQuestionMark != -1)
                    {
                        string extraQuery = newPath.Substring(queryStringQuestionMark + 1);
                        if (rewritten.Query == "")
                        {
                            rewritten.Query = extraQuery;
                        }
                        else if (extraQuery != "")
                        {
                            rewritten.Query = extraQuery + "&" + rewritten.Query.TrimStart('?');
                        }

                        newPath = newPath.Substring(0, queryStringQuestionMark);
                    }

                    rewritten.Path = newPath;

                    break;
                }
            }

            return rewritten.Uri;
        }

        /// <summary>
        /// Rewrite the url for path info. This will iterate the file structure until a file is found.
        /// </summary>
        /// <param name="request"></param>
        private void RewriteForPathInfo(Request request)
        {
            UriBuilder rewrittenUrl = new(request.RewrittenUrl);
            string path = "";
            for (int i = 0; i < rewrittenUrl.Uri.Segments.Length; i++)
            {
                string segment = rewrittenUrl.Uri.Segments[i].TrimEnd('/');
                path = Path.Combine(path, segment);
                string pathOnDisk = Path.Combine(this._config.DocumentRoot, path);
                if (File.Exists(pathOnDisk))
                {
                    // Segments have a trailing slash where needed, and do not need another.
                    request.PathInfo = "/" + string.Join("", rewrittenUrl.Uri.Segments.Skip(i + 1));
                    break;
                }
                else if (!Directory.Exists(pathOnDisk))
                {
                    break;
                }
            }

            rewrittenUrl.Path = path;
            request.RewrittenUrl = rewrittenUrl.Uri;
        }

        /// <summary>
        /// Handler for the <see cref="HttpListener"/>.
        /// </summary>
        /// <param name="listener"></param>
        public void Handle(HttpListener listener)
        {
            listener.BeginGetContext(this.Handle, listener);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            foreach ((string _, DocumentHandler handler) in this._handlers)
            {
                if (handler is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
