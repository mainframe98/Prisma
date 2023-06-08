namespace Prisma;

internal static class Program
{
    public static void Main(string[] args)
    {
        new Cli()
            .ProcessInput(args)
            .Run();
    }
}
