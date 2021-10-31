using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using Prisma.Config;
using Serilog;

namespace Prisma.DocumentHandlers
{
    public class CgiHandler : DocumentHandler
    {
        private readonly CgiApplicationConfig _applicationConfig;

        public CgiHandler(ServerConfig config, ILogger logger, CgiApplicationConfig applicationConfig) : base(config, logger)
        {
            this._applicationConfig = applicationConfig;
            this.LoggableName = $"CGI: {applicationConfig.Path}";
        }

        /// <inheritdoc cref="DocumentHandler.HandleRequest"/>
        public override void HandleRequest(Request request, HttpListenerResponse response)
        {
            using Process process = new()
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = this._applicationConfig.Path,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            foreach (string arg in this._applicationConfig.Arguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            // Don't let Windows add pointless environment variables.
            // TODO: Is this only needed for Windows?
            //   Linux shell environment variable inheritance is expected and shouldn't be discouraged.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Dictionary<string, string> variablesToKeep = new()
                {
                    ["PATH"] = process.StartInfo.Environment["PATH"],
                    ["TMP"] = process.StartInfo.Environment["TMP"],
                    ["TEMP"] = process.StartInfo.Environment["TEMP"]
                };
                process.StartInfo.Environment.Clear();

                foreach (KeyValuePair<string,string> variable in variablesToKeep)
                {
                    process.StartInfo.Environment.Add(variable);
                }
            }
            foreach ((string variable, string value) in this._applicationConfig.EnvironmentVariables)
            {
                process.StartInfo.Environment.Add(variable, value);
            }
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

            process.ErrorDataReceived += (sender, args) =>
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
