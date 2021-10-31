using System.Collections.Generic;

namespace Prisma.Exceptions
{
    public class InvalidConfigException : ServerException
    {
        public InvalidConfigException(string message) : base(message) {}
        public InvalidConfigException(IEnumerable<string> messages) : this(string.Join("\n", messages)) {}
    }
}
