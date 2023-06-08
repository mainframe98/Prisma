using System;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading;
using Mono.Options;
using Prisma.Config;
using Prisma.Exceptions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Prisma;

public class Cli
{
    private ServerConfig _config;
    private Action _action;
    private ILogger _logger;
    private readonly OptionSet _optionSet;

    internal Cli()
    {
        this._config = new ServerConfig();
        this._action = this.Host;
        this._logger = Logger.None;
        string supportedLogLevels =  string.Join(", ", Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>());
        this._optionSet = new OptionSet
        {
            {"?|help|h", "Display this help and exit.", h => this._action = this.DisplayHelp},
            {"v|version", "Show version information.", v => this._action = this.DisplayVersion},
            {"c|config=", "Path to the configuration file.", c => this._config = ServerConfig.LoadFrom(c)},
            {"l|log=", "Path to the log file.", l => this._config.Logging.Path = l},
            {"p|port=", "Port to listen to. Defaults to 8080.", p => this._config.Port = ushort.Parse(p, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)},
            {"d|docroot=", "Path to the document root (from which to serve the files). Defaults to the working directory.", d => this._config.DocumentRoot = d},
            {"a|allow-pathinfo", "Allow path info.", a => this._config.AllowPathInfo = true},
            {"enable-cgi-bin", "Enable the cgi-bin/ directory.", e => this._config.EnableCgiBin = true},
            {"enable-cgi-extension", "Treat all requests to scripts with a cgi extension as cgi scripts.", d => this._config.TreatCgiExtensionScriptsAsCgiScripts = true},
            {"listener-prefix=", "Listen to these URI prefixes. This will overwrite the default of 127.0.0.1 and the provided port, and might require administrator privileges.", l => this._config.ListenerPrefixes.Add(l)},
            {"default-document=", "Files to look for when a directory is requested.", d => this._config.DefaultDocuments.Add(d)},
            {"j|log-as-json", "Log to the log file in the JSON format.", j => this._config.Logging.LogAsJson = true},
            {"log-level=", "Level of events to log. Supported levels: " + supportedLogLevels, l =>
            {
                if (Enum.TryParse<LogEventLevel>(l, true, out var level))
                {
                    this._config.Logging.Level = level;
                }
            }}
        };

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (this._logger is Logger logger)
            {
                logger.Fatal((Exception) eventArgs.ExceptionObject, "Unhandled exception");
                logger.Dispose();
            }
        };
    }

    /// <summary>
    /// Parse command line input.
    /// </summary>
    /// <param name="args">Command line input</param>
    /// <returns></returns>
    internal Cli ProcessInput(string[] args)
    {
        try
        {
            this._optionSet.Parse(args);

            this._config.Normalize();
            this._logger = this._config.Logging.MakeLoggerConfiguration()
                .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Error)
                .CreateLogger();
        }
        catch (Exception e) when (e is OptionException | e is FileNotFoundException | e is InvalidConfigException)
        {
            this._action = () =>
            {
                this.DisplayVersion();
                Console.Error.WriteLine(e.Message);
                Console.WriteLine("More information is available using the --help option.");
            };
        }

        return this;
    }

    /// <summary>
    /// Execute the action specified by the user.
    /// </summary>
    internal void Run() => this._action();

    /// <summary>
    /// Host the web server.
    /// </summary>
    private void Host()
    {
        ManualResetEvent exitEvent = new(false);

        // Register handler here. Any other code paths do not do anything but print to the console.
        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            exitEvent.Set();
        };

        using Server server = new(this._config, this._logger);

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            WindowsConsoleHandler.RegisterShutdownHandler(() => server.Dispose());
        }

        try
        {
            server.Start();
        }
        catch (SetupException e)
        {
            this._logger.Fatal(e, "Handler setup failed: {ExceptionMessage}", e.Message);
            return;
        }
        catch (HttpListenerException e)
        {
            switch (e.ErrorCode)
            {
                case 5:
                    this._logger.Fatal("Using a URI prefix other than localhost or 127.0.0.1, or a port below 1024 requires administrative privileges");
                    return;
                case 32:
                    this._logger.Fatal("A port and/or prefix is already in use by another process");
                    return;
                default:
                    throw;
            }
        }

        exitEvent.WaitOne();
        (this._logger as Logger)?.Dispose();
    }

    /// <summary>
    /// Displays the help menu.
    /// </summary>
    private void DisplayHelp()
    {
        this.DisplayVersion();
        Console.WriteLine();
        Console.WriteLine("Usage: prisma");
        Console.WriteLine();
        Console.WriteLine("Options:");
        this._optionSet.WriteOptionDescriptions(Console.Out);
        Console.WriteLine();
        Console.WriteLine("Prisma utilizes the following external projects:");
        foreach (var package in PrismaInfo.UsedComponents)
        {
            Console.WriteLine($"\t{package.Name}");
        }
    }

    /// <summary>
    /// Displays the version.
    /// </summary>
    private void DisplayVersion()
    {
        Console.WriteLine(PrismaInfo.SoftwareName);
        Console.WriteLine($"© {DateTime.Now.Year} Mainframe98");
    }
}
