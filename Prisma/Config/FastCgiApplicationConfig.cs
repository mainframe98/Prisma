using System;

namespace Prisma.Config
{
    public class FastCgiApplicationConfig : ICloneable
    {
        /// <summary>
        /// FastCGI application socket.
        /// </summary>
        public string Socket { get; set; } = "";

        public object Clone() => this.MemberwiseClone();
    }
}
