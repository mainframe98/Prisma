using System;

namespace Prisma.Config;

public class FastCgiApplicationConfig : ICloneable
{
    /// <summary>
    /// FastCGI application socket.
    /// </summary>
    public string Socket { get; set; } = "";

    /// <summary>
    /// Configuration for starting the FastCGI application with Prisma.
    /// Only when a valid path to an application is provided will Prisma launch it before serving requests.
    /// </summary>
    public ApplicationConfig LaunchConfiguration { get; set; } = new();

    public object Clone() => this.MemberwiseClone();
}