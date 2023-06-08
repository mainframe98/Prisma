using System;

namespace Prisma.Exceptions;

public class SetupException : ServerException
{
    public SetupException(string message) : base(message) {}
    public SetupException(string message, Exception inner) : base(message, inner) {}
}