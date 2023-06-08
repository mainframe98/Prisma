using System;

namespace Prisma.Exceptions;

public class ConnectionException : ServerException
{
    public ConnectionException(string message) : base(message) {}
    public ConnectionException(string message, Exception inner) : base(message, inner) {}
}