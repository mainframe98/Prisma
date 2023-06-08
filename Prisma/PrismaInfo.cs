using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Prisma;

public static class PrismaInfo
{
    public record UsedComponent(string Name, string License, string Url, Version? Version = null)
    {
        public string Name { get; } = Name;
        public string License { get; } = License;
        public string Url { get; } = Url;
        public Version? Version { get; } = Version;
    }

    public static readonly Version Version = Assembly.GetAssembly(typeof(PrismaInfo))!.GetName().Version!;

    public static readonly string SoftwareName = $"Prisma web server {Version}";

    public static readonly ObservableCollection<UsedComponent> UsedComponents = new()
    {
        new("FastCGI", "MIT", "https://github.com/LukasBoersma/FastCGI", GetVersion(typeof(FastCGI.Constants))),
        new("MimeTypes", "Apache-2.0", "https://github.com/khellang/MimeTypes", new Version(2, 2, 1, 0)),
        new("Mono.Options", "MIT", "https://github.com/xamarin/XamarinComponents/tree/main/XPlat/Mono.Options", GetVersion(typeof(Mono.Options.Option))),
        new("Serilog", "Apache-2.0", "https://serilog.net/", GetVersion(typeof(Serilog.ILogger))),
        new("RRZE Icon Set", "CC BY-SA 3.0", "https://rrze-pp.github.io/rrze-icon-set")
    };

    public static Version? GetVersion(Type type) => Assembly.GetAssembly(type)?.GetName().Version;
}