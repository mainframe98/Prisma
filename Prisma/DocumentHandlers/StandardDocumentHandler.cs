using System.IO;
using System.Net;
using System.Text;
using Prisma.Config;
using Serilog;

namespace Prisma.DocumentHandlers;

public class StandardDocumentHandler : DocumentHandler
{
    public StandardDocumentHandler(ServerConfig config, ILogger logger) : base(config, logger.ForContext<StandardDocumentHandler>())
    {
        this.LoggableName = this.GetType().Name;
    }

    /// <inheritdoc cref="DocumentHandler.HandleRequest"/>
    public override void HandleRequest(Request request, HttpListenerResponse response)
    {
        if (request.RewrittenUrl.LocalPath == "/")
        {
            this.ShowDirectory(request.RewrittenUrl.LocalPath, this.Config.DocumentRoot, response);
            return;
        }

        string path = Path.Combine(this.Config.DocumentRoot, request.RewrittenUrl.LocalPath[1..]);

        if (Directory.Exists(path))
        {
            this.ShowDirectory(request.RewrittenUrl.LocalPath, path, response);
        }
        else if (File.Exists(path))
        {
            FileInfo fi = new(path);

            response.ContentType = MimeTypes.GetMimeType(fi.Name);
            using FileStream stream = fi.OpenRead();
            response.ContentLength64 = stream.Length;
            stream.CopyTo(response.OutputStream);
            response.OutputStream.Close();
        }
        else
        {
            response.StatusCode = (int) HttpStatusCode.NotFound;
            this.WriteToResponse("", response);
        }
    }

    /// <summary>
    /// Show a directory overview.
    /// </summary>
    /// <param name="webPath">Path as shown on the client-side</param>
    /// <param name="localPath">Path as shown server-side</param>
    /// <param name="response"></param>
    private void ShowDirectory(string webPath, string localPath, HttpListenerResponse response)
    {
        DirectoryInfo di = new(localPath);
        StringBuilder sb = new($@"
<html>
    <head>
        <title>Index of {webPath}</title>
    </head>
    <body>
        <h1>Index of {webPath}</h1>
        <ul>
");
        if (localPath != this.Config.DocumentRoot)
        {
            sb.AppendLine($@"<li><a href=""{Path.GetDirectoryName(webPath)}"">⤴ ..</a></li>");
        }

        foreach (DirectoryInfo dir in di.EnumerateDirectories())
        {
            sb.AppendLine($@"<li><a href=""{Path.Combine(webPath, dir.Name)}"">📁 {dir.Name}</a></li>");
        }

        sb.AppendLine( "</ul>\n<ul>" );

        foreach (FileInfo fi in di.EnumerateFiles())
        {
            sb.AppendLine($@"<li><a href=""{Path.Combine(webPath, fi.Name)}"">📄 {fi.Name}</a></li>");
        }

        sb.AppendLine(@$"
        </ul>
        <hr>
        <p>{PrismaInfo.SoftwareName}</p>
    </body>
</html>
");

        this.WriteToResponse(sb.ToString(), response);
    }
}