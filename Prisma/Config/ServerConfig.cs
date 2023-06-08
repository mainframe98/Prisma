using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Prisma.Exceptions;

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace Prisma.Config
{
    public class ServerConfig : ICloneable
    {
        /// <summary>
        /// Available CGI applications, keyed by name.
        /// </summary>
        public Dictionary<string, ApplicationConfig> CgiApplications { get; set; } = new();

        /// <summary>
        /// Available FastCGI applications, keyed by name.
        /// </summary>
        public Dictionary<string, FastCgiApplicationConfig> FastCgiApplications { get; set; } = new();

        /// <summary>
        /// Document root from which to serve the files.
        /// </summary>
        public string DocumentRoot { get; set; } = "";

        /// <summary>
        /// Logging configuration.
        /// </summary>
        public LogConfig Logging { get; set; } = new();

        /// <summary>
        /// Port to listen to.
        /// </summary>
        public ushort Port { get; set; } = 0;

        /// <summary>
        /// Map of file extensions mapped to an interpreter listed in <see cref="CgiApplications"/> or <see cref="FastCgiApplications"/>.
        /// </summary>
        public Dictionary<string, string> InvokeOnExtension { get; set; } = new();

        /// <summary>
        /// Map of paths, relative to the document root, mapped to an interpreter listed in <see cref="CgiApplications"/> or <see cref="FastCgiApplications"/>.
        /// </summary>
        public Dictionary<string, string> InvokeOnPath { get; set; } = new();

        /// <summary>
        /// Allow path info.
        /// This setting makes the server iterate the path to find a file,
        /// rather than interpreting the whole path as a file on the server.
        /// </summary>
        public bool AllowPathInfo { get; set; } = false;

        /// <summary>
        /// Enable the cgi-bin/ directory.
        /// </summary>
        public bool EnableCgiBin { get; set; } = false;

        /// <summary>
        /// Treat documents with a .cgi extension as CGI scripts.
        /// </summary>
        public bool TreatCgiExtensionScriptsAsCgiScripts { get; set; } = false;

        /// <summary>
        /// Default documents to look for when a directory is requested. When any of the files in the list exist,
        /// they will be served instead of the directory overview.
        /// </summary>
        public List<string> DefaultDocuments { get; set; } = new();

        /// <summary>
        /// Rewrite rules. Keys define a regex for a path, values the rewritten path.
        /// </summary>
        public Dictionary<Regex, string> RewriteRules { get; set; } = new();

        /// <summary>
        /// Uri prefixes to listen to. These must be in a format permitted by<see cref="System.Net.HttpListener.Prefixes"/>.
        ///
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener#remarks for all supported formats.
        /// </summary>
        public List<string> ListenerPrefixes { get; set; } = new();

        /// <summary>
        /// Normalize config options by making paths absolute and filtering invalid values.
        /// </summary>
        public void Normalize()
        {
            // Turn relative paths into a path relative to the working directory.
            this.MakeRelativePathsAbsolute(Directory.GetCurrentDirectory());

            if (this.Port == 0)
            {
                this.Port = 8080;
            }

            if (this.ListenerPrefixes.Count == 0)
            {
                this.ListenerPrefixes.Add($"http://127.0.0.1:{this.Port}/");
            }

            string[] errorMessages = this.Validate().ToArray();
            if (errorMessages.Length > 0)
            {
                throw new InvalidConfigException(errorMessages);
            }
        }

        private IEnumerable<string> Validate()
        {
            if (!Directory.Exists(this.DocumentRoot))
            {
                yield return $"Document root {this.DocumentRoot} does not exist.";
            }

            List<string> applicationNames = this.CgiApplications.Keys.Concat(this.FastCgiApplications.Keys).ToList();

            foreach (string application in this.CgiApplications.Keys.Intersect(this.FastCgiApplications.Keys))
            {
                yield return $"{application} is defined twice.";
            }

            foreach (var (extension, application) in this.InvokeOnExtension.Where(e => !applicationNames.Contains(e.Value)))
            {
                yield return $"Extension {extension} has unknown application {application} assigned to it.";
            }

            foreach (var (path, application) in this.InvokeOnPath.Where(p => !applicationNames.Contains(p.Value)))
            {
                yield return $"Path {path} has unknown application {application} assigned to it.";
            }

            foreach ((string application, FastCgiApplicationConfig fastCgiApplication) in this.FastCgiApplications)
            {
                if (fastCgiApplication.Socket == "")
                {
                    yield return $"{application} has no socket defined.";
                }
            }

            foreach (string prefix in this.ListenerPrefixes)
            {
                if (!prefix.StartsWith("http://") && !prefix.StartsWith("https://"))
                {
                    yield return "Prefixes must start with http:// or https://.";
                }

                if (prefix.Last() != '/')
                {
                    yield return "Prefixes must end with a slash.";
                }
            }
        }

        /// <summary>
        /// Creates a new config by loading from the given path by deserializing it.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static ServerConfig LoadFrom(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The config file does not exist", path);
            }

            ServerConfig config = JsonSerializer.Deserialize<ServerConfig>(
                File.ReadAllText(path),
                new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                        new JsonRegexKeyConverter()
                    }
                }
            );

            // If the config file contains relative paths, then that means relative to where the config file is,
            // not the working directory.
            config.MakeRelativePathsAbsolute(Path.GetDirectoryName(path) ?? path);

            return config;
        }

        /// <summary>
        /// Turns relative paths into absolute paths.
        /// </summary>
        /// <param name="rootDirectory">Directory to which the paths are relative.</param>
        private void MakeRelativePathsAbsolute(string rootDirectory)
        {
            if (this.DocumentRoot == "")
            {
                this.DocumentRoot = rootDirectory;
            }
            else if (!Path.IsPathRooted(this.DocumentRoot))
            {
                this.DocumentRoot = Path.Combine(rootDirectory, this.DocumentRoot);
            }

            if (this.Logging.Path != "" && !Path.IsPathRooted(this.Logging.Path))
            {
                this.Logging.Path = Path.Combine(rootDirectory, this.Logging.Path);
            }
        }

        public object Clone()
        {
            ServerConfig clone = (ServerConfig)this.MemberwiseClone();

            clone.CgiApplications = this.CgiApplications.ToDictionary(p => p.Key, p => (ApplicationConfig)p.Value.Clone());
            clone.FastCgiApplications = this.FastCgiApplications.ToDictionary(p => p.Key, p => (FastCgiApplicationConfig)p.Value.Clone());
            clone.Logging = (LogConfig)this.Logging.Clone();
            clone.InvokeOnPath = new Dictionary<string, string>(this.InvokeOnPath);
            clone.InvokeOnExtension = new Dictionary<string, string>(this.InvokeOnExtension);
            clone.DefaultDocuments = new List<string>(this.DefaultDocuments);
            clone.RewriteRules = new Dictionary<Regex, string>(this.RewriteRules);
            clone.ListenerPrefixes = new List<string>(this.ListenerPrefixes);

            return clone;
        }

        public override string ToString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new JsonRegexKeyConverter()
            }
        });
    }
}
