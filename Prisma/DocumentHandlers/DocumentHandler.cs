using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Prisma.Config;
using Serilog;

namespace Prisma.DocumentHandlers
{
    public abstract class DocumentHandler
    {
        /// <summary>
        /// Server configuration.
        /// </summary>
        protected readonly ServerConfig Config;

        /// <summary>
        /// Logger for this specific handler.
        /// For request specific logging, use <see cref="Request.Logger"/>
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Human readable interpretation of the invocation.
        /// </summary>
        public string LoggableName { get; set; }

        protected DocumentHandler(ServerConfig config, ILogger logger)
        {
            this.Config = config;
            this.Logger = logger;
            this.LoggableName = this.GetType().Name;
        }

        /// <summary>
        /// Handle the given request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public abstract void HandleRequest(Request request, HttpListenerResponse response);

        /// <summary>
        /// Write the provided text to the response.
        /// </summary>
        /// <param name="content">Content encoded in UTF-8</param>
        /// <param name="response"></param>
        protected internal void WriteToResponse(string content, HttpListenerResponse response)
        {
            response.ContentType = "text/html; charset=UTF-8";
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer,0,buffer.Length);
            response.OutputStream.Close();
        }

        /// <summary>
        /// Build CGI required environment variables, per https://datatracker.ietf.org/doc/html/rfc3875.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="gatewayInterface">Value of the GATEWAY_INTERFACE variable</param>
        /// <returns></returns>
        protected Dictionary<string, string> BuildRequestVariables(Request request, string gatewayInterface)
        {
            HttpListenerRequest originalRequest = request.OriginalRequest;
            string? host;
            if (string.IsNullOrEmpty(originalRequest.UserHostName))
            {
                host = originalRequest.LocalEndPoint?.Address.ToString();
            }
            else
            {
                host = originalRequest.UserHostName;
                string port = ":" + this.Config.Port;
                if (originalRequest.UserHostName.EndsWith(port))
                {
                    host = host.Substring(0,host.Length - port.Length);
                }
            }

            Dictionary<string, string?> variables = new()
            {
                {"CONTENT_LENGTH", originalRequest.ContentLength64 <= 0 ? null : originalRequest.ContentLength64.ToString() },
                {"CONTENT_TYPE", originalRequest.ContentType},
                {"GATEWAY_INTERFACE", gatewayInterface},
                {"PATH_INFO", request.PathInfo},
                {"PATH_TRANSLATED", Path.Combine(this.Config.DocumentRoot, request.RewrittenUrl.LocalPath[1..])},
                // RFC 3875 specifies the query string as being without the leading ?.
                {"QUERY_STRING", request.RewrittenUrl.Query.TrimStart('?')},
                {"REMOTE_ADDR", originalRequest.RemoteEndPoint?.Address.ToString()},
                {"REMOTE_HOST", originalRequest.RemoteEndPoint?.Address.ToString()},
                {"REQUEST_METHOD", originalRequest.HttpMethod},
                {"SCRIPT_NAME", request.RewrittenUrl.LocalPath},
                {"SERVER_NAME", host},
                {"SERVER_PORT", this.Config.Port.ToString()},
                {"SERVER_PROTOCOL", "HTTP/" + originalRequest.ProtocolVersion},
                {"SERVER_SOFTWARE", PrismaInfo.SoftwareName},
                // Non-spec variables, but either commonly used, or available and useful.
                {"DOCUMENT_ROOT", this.Config.DocumentRoot},
                {"REMOTE_PORT", originalRequest.RemoteEndPoint?.Port.ToString()},
                {"REQUEST_URI", request.RewrittenUrl.LocalPath + request.PathInfo + request.RewrittenUrl.Query},
                // This id is set in the request. Providing this to applications make the log files more usable.
                {"UNIQUE_ID", request.UniqueId}
            };

            foreach (string? header in originalRequest.Headers)
            {
                string cgiHeader = (header ?? "").ToUpper().Replace('-', '_');
                if (cgiHeader == "" || variables.ContainsKey(cgiHeader))
                {
                    continue;
                }

                variables.Add("HTTP_" + cgiHeader, originalRequest.Headers[header]);
            }

            Dictionary<string, string> filteredVariables =  variables
                .Where(p => p.Value != null)
                .ToDictionary(p => p.Key, p => p.Value ?? throw new NullReferenceException($"{p.Key} has a null value!"));

            request.Logger.Debug("Request generated environment variables: {EnvironmentVariables}", filteredVariables);

            return filteredVariables;
        }

        /// <summary>
        /// Read the response from the CGI application and write it to the response object.
        /// </summary>
        /// <param name="streamReader">Stream reader to read the response from. This MUST be backed by a stream that is seekable</param>
        /// <param name="response"></param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="streamReader"/> isn't backed by a stream that is seekable</exception>
        protected void ReadCgiResponse(StreamReader streamReader, HttpListenerResponse response)
        {
            if (!streamReader.BaseStream.CanSeek)
            {
                throw new ArgumentException("The provided stream reader must be backed by a stream that is seekable.", nameof(streamReader));
            }

            string? line = streamReader.ReadLine();
            int streamCounter = 0;
            // This assumes that nothing will ever violate the specification and terminate with non CRLF line-endings...
            const int lineEndingByteCount = 2;
            while (line != null)
            {
                streamCounter += streamReader.CurrentEncoding.GetByteCount(line) + lineEndingByteCount;
                if (line == "")
                {
                    streamReader.BaseStream.Position = streamCounter;
                    response.ContentLength64 = streamReader.BaseStream.Length - streamReader.BaseStream.Position;
                    streamReader.BaseStream.CopyTo(response.OutputStream);
                    return;
                }

                string[] header = line.Split(':', 2);

                if (header.Length == 2)
                {
                    if (header[0].ToLower() == "status")
                    {
                        string[] status = header[1].TrimStart(' ').Split(' ', 2);
                        response.StatusCode = int.Parse(status[0]);
                        if (status.Length > 1)
                        {
                            response.StatusDescription = status[1];
                        }
                    }
                    else
                    {
                        response.AppendHeader(header[0], header[1].TrimStart(' '));
                    }
                }

                line = streamReader.ReadLine();
            }
        }
    }
}
