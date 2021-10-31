using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Serilog;

namespace PrismaGUI
{
    public static class Utilities
    {
        public static void LaunchBrowserForUri(Uri uri) => LaunchBrowserForUri(uri.ToString());
        public static void LaunchBrowserForUri(string url) => Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });

        public static string ToJsonString(this JsonDocument json)
        {
            using MemoryStream stream = new();
            Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
            json.WriteTo(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Logger for application level events.
        /// </summary>
        public static ILogger ApplicationLogger { get; }

        static Utilities()
        {
            LoggerConfiguration loggerConfiguration = new();
            loggerConfiguration
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.File(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, "app.log"));

            ApplicationLogger = loggerConfiguration.CreateLogger();
        }
    }
}
