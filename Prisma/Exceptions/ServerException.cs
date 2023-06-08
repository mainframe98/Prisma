using System;

namespace Prisma.Exceptions;

public class ServerException : Exception
{
    public ServerException(string message) : base(message) {}
    protected ServerException(string message, Exception? inner) : base(message, inner) {}
}