using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FastCGI;
using Prisma.Config;
using Prisma.Exceptions;
using Serilog;

namespace Prisma.DocumentHandlers;

public sealed class FastCgiHandler : DocumentHandler, IDisposable
{
    private const byte ZeroByte = 0x00;
    private readonly EndPoint _endPoint;
    private readonly Process? _fastCgiApplicationProcess;
    private int _requestIdCounter;

    public FastCgiHandler(ServerConfig config, ILogger logger, FastCgiApplicationConfig applicationConfig) : base(config, logger)
    {
        this.LoggableName = $"FastCGI: {applicationConfig.Socket}";

        if (IPEndPoint.TryParse(applicationConfig.Socket, out var ipEndPoint))
        {
            logger.Debug("Connecting through a TCP socket on {Uri}", ipEndPoint);
            this._endPoint = ipEndPoint;
        }
        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            logger.Debug("Connecting through a Unix socket on {Socket}", applicationConfig.Socket);
            this._endPoint = new UnixDomainSocketEndPoint(applicationConfig.Socket);
        }
        else
        {
            throw new InvalidConfigException($"Socket {applicationConfig.Socket} is not a valid socket.");
        }

        if (applicationConfig.LaunchConfiguration.Path != "")
        {
            this._fastCgiApplicationProcess = new()
            {
                StartInfo = this.CreateProcessStartInfo(applicationConfig.LaunchConfiguration)
            };
        }
    }

    public override void Initialize()
    {
        if (this._fastCgiApplicationProcess != null)
        {
            this._fastCgiApplicationProcess.Start();
            this.Logger.Debug("Launched FastCGI application for {Application}", this.LoggableName);

            this._fastCgiApplicationProcess.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null)
                {
                    this.Logger.Error("FastCGI application error: {ErrorLine}", args.Data);
                }
            };
            this._fastCgiApplicationProcess.BeginErrorReadLine();

            // This check seems to required to make applications actually be ready to connect to.
            if (!this._fastCgiApplicationProcess.Responding)
            {
                throw new SetupException($"The FastCGI application \"{this.LoggableName}\" is not responding");
            }
        }

        Socket? socket = null;
        try
        {
            socket = new Socket(this._endPoint.AddressFamily, SocketType.Stream, ProtocolType.IP);
            socket.Connect(this._endPoint);
            this.InitializeFastCgi(socket);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
        {
            throw new SetupException($"The FastCGI application \"{this.LoggableName}\" on socket {this._endPoint} is not available", e);
        }
        finally
        {
            socket?.Dispose();
        }
    }

    private void InitializeFastCgi(Socket socket)
    {
        using NetworkStream stream = new(socket, false);

        Dictionary<string, byte[]> nameValuePairs = new()
        {
            {"FCGI_MAX_CONNS", Encoding.ASCII.GetBytes("")},
            {"FCGI_MAX_REQS", Encoding.ASCII.GetBytes("")},
            {"FCGI_MPXS_CONNS", Encoding.ASCII.GetBytes("")}
        };
        Record getValuesRequest = new()
        {
            RequestId = 0,
            Type = Record.RecordType.GetValues
        };
        getValuesRequest.SetNameValuePairs(nameValuePairs);
        getValuesRequest.WriteToStream(stream);
        stream.Flush();

        Record? getValuesResult = Record.ReadRecord(stream);
        if (getValuesResult?.Type != Record.RecordType.GetValuesResult)
        {
            this.Logger.Error("Tried to get FastCGI app info but got {Value} instead", getValuesResult == null ? "null" : getValuesResult.Type.ToString());
        }
        else
        {
            Dictionary<string, string> pairs = getValuesResult.GetNameValuePairs()
                .ToDictionary(p => p.Key, p => Encoding.ASCII.GetString(p.Value));

            this.Logger.Debug("App info reports FCGI_MAX_CONNS as {FcgiMaxConns}; FCGI_MAX_REQS as {FcgiMaxReqs}; FCGI_MPXS_CONNS as {FcgiMpxsConns}", pairs["FCGI_MAX_CONNS"], pairs["FCGI_MAX_REQS"], pairs["FCGI_MPXS_CONNS"]);

            if (pairs["FCGI_MPXS_CONNS"] == "1")
            {
                throw new Exception("FCGI_MPXS_CONNS is 1, which is not supported!");
            }
        }
    }

    /// <inheritdoc cref="DocumentHandler.HandleRequest"/>
    public override void HandleRequest(Request request, HttpListenerResponse response)
    {
        Interlocked.Increment(ref this._requestIdCounter);
        // Prevent negative request ids.
        Interlocked.CompareExchange(ref this._requestIdCounter, 1, int.MinValue);

        int requestId = this._requestIdCounter;

        request.Logger.Debug("This request has the ID {RequestId}", requestId);

        using Socket socket = new(this._endPoint.AddressFamily, SocketType.Stream, ProtocolType.IP);

        try
        {
            socket.Connect(this._endPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
        {
            throw new ConnectionException($"Cannot connect to socket on {this._endPoint}", e);
        }

        using NetworkStream stream = new(socket, false);

        byte[] beginRequestContent =
        {
            ZeroByte,
            Constants.FCGI_RESPONDER,
            ZeroByte,
            ZeroByte, ZeroByte, ZeroByte, ZeroByte, ZeroByte
        };

        Record beginRequest = new()
        {
            Type = Record.RecordType.BeginRequest,
            RequestId = requestId,
            ContentLength = beginRequestContent.Length,
            ContentData = beginRequestContent
        };

        beginRequest.WriteToStream(stream);

        Dictionary<string, string> requestVariables = this.BuildRequestVariables(request, "FastCGI/1.0");
        requestVariables["SCRIPT_FILENAME"] = requestVariables["PATH_TRANSLATED"];

        Record parameterRequest = new()
        {
            Type = Record.RecordType.Params,
            RequestId = requestId
        };

        parameterRequest.SetNameValuePairs(requestVariables.ToDictionary(p => p.Key, p => Encoding.UTF8.GetBytes(p.Value)));
        parameterRequest.WriteToStream(stream);

        Record endParametersRequest = new()
        {
            Type = Record.RecordType.Params,
            RequestId = requestId,
            ContentLength = 0,
            ContentData = Array.Empty<byte>()
        };
        endParametersRequest.WriteToStream(stream);

        if (request.OriginalRequest.ContentLength64 > 0)
        {
            // Copying to a memory stream abstracts the whole read from stream component.
            // By defining a stream length, the internal buffer is sized appropriately,
            // making this an efficient operation too.
            using MemoryStream memoryStream = new((int) request.OriginalRequest.ContentLength64);
            request.OriginalRequest.InputStream.CopyTo(memoryStream);

            Record standardInputRequest = new()
            {
                Type = Record.RecordType.Stdin,
                RequestId = requestId,
                ContentLength = memoryStream.GetBuffer().Length,
                ContentData = memoryStream.GetBuffer()
            };
            standardInputRequest.WriteToStream(stream);
        }

        Record endRequest = new()
        {
            Type = Record.RecordType.Stdin,
            RequestId = requestId,
            ContentLength = 0,
            ContentData = Array.Empty<byte>()
        };
        endRequest.WriteToStream(stream);

        // Writing done, ensure everything is send to the FastCGI application.
        stream.Flush();

        List<Record> responseRecords = new();

        Record? activeRecord;
        do
        {
            try
            {
                activeRecord = Record.ReadRecord(stream);
            }
            catch (InvalidDataException e)
            {
                this.Logger.Error(e, "One of the FastCGI records (for request {Request}) is corrupt", requestId);
                activeRecord = Record.ReadRecord(stream);
            }

            if (activeRecord == null)
            {
                break;
            }

            if (activeRecord.RequestId != requestId)
            {
                this.Logger.Debug("Found request with ID {OtherId}, but expected {RequestId}", activeRecord.RequestId, requestId);
                continue;
            }

            responseRecords.Add(activeRecord);
        } while (activeRecord.Type != Record.RecordType.EndRequest || activeRecord.RequestId != requestId);

        byte[][] stdOutChunks = responseRecords
            .Where(r => r.Type == Record.RecordType.Stdout)
            .Select(r => r.ContentData)
            .ToArray();

        using MemoryStream stdOutStream = this.BuildStreamFromByteArrays(stdOutChunks);
        using StreamReader stdOutStreamReader = new(stdOutStream);

        this.ReadCgiResponse(stdOutStreamReader, response);

        byte[][] stdErrChunks = responseRecords
            .Where(r => r.Type == Record.RecordType.Stderr)
            .Select(r => r.ContentData)
            .ToArray();

        using MemoryStream stdErrStream = this.BuildStreamFromByteArrays(stdErrChunks);
        using StreamReader stdErrStreamReader = new(stdErrStream);

        string? line = stdErrStreamReader.ReadLine();
        while (line != null)
        {
            request.Logger.Error("{ErrorLine}", line);
            line = stdErrStreamReader.ReadLine();
        }
    }

    private MemoryStream BuildStreamFromByteArrays(byte[][] chunks)
    {
        MemoryStream stream = new(chunks.Sum(r => r.Length));
        foreach (byte[] chunk in chunks)
        {
            stream.Write(chunk, 0, chunk.Length);
        }

        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (this._fastCgiApplicationProcess != null)
        {
            bool hasExited = this._fastCgiApplicationProcess.HasExited;
            if (!hasExited && this._fastCgiApplicationProcess.CloseMainWindow())
            {
                this.Logger.Verbose("Signalling {Application} that it should close. Waiting for 1500ms", this.LoggableName);
                hasExited = this._fastCgiApplicationProcess.WaitForExit(1500);
            }

            if (!hasExited)
            {
                this.Logger.Information("{Application} doesn't support politely asking to close or loitered. Killing it now", this.LoggableName);
                this._fastCgiApplicationProcess.Kill();
            }

            this._fastCgiApplicationProcess.Dispose();
        }
    }
}
