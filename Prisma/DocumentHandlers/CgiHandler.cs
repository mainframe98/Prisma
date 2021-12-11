using System.Diagnostics;
using System.IO;
using System.Net;
using Prisma.Config;
using Serilog;

namespace Prisma.DocumentHandlers
{
    public class CgiHandler : DocumentHandler
    {
        private readonly ApplicationConfig _applicationConfig;

        public CgiHandler(ServerConfig config, ILogger logger, ApplicationConfig applicationConfig) : base(config, logger)
        {
            this._applicationConfig = applicationConfig;
            this.LoggableName = $"CGI: {applicationConfig.Path}";
        }

        /// <inheritdoc cref="DocumentHandler.HandleRequest"/>
        public override void HandleRequest(Request request, HttpListenerResponse response)
        {
            using Process process = new()
            {
                StartInfo = this.CreateProcessStartInfo(this._applicationConfig)
            };

            foreach ((string variable, string value) in this.BuildRequestVariables(request, "CGI/1.1"))
            {
                process.StartInfo.Environment.Add(variable, value);
            }

            process.Start();

            if (request.OriginalRequest.ContentLength64 > 0)
            {
                request.OriginalRequest.InputStream.CopyTo(process.StandardInput.BaseStream);
            }

            process.StandardInput.Close();

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null)
                {
                    request.Logger.Error("{ErrorLine}", args.Data);
                }
            };
            process.BeginErrorReadLine();

            // Copy the process response to a memory stream that supports seeking.
            using MemoryStream stream = new();
            process.StandardOutput.BaseStream.CopyTo(stream);
            stream.Position = 0;
            using StreamReader streamReader = new(stream);

            this.ReadCgiResponse(streamReader, response);

            process.WaitForExit();
        }
    }
}
