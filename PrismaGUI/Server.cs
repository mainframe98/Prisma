using System.Net;
using Prisma;
using Prisma.Config;
using Prisma.Exceptions;
using PrismaGUI.Properties;
using Serilog;

namespace PrismaGUI
{
    internal sealed class Server
    {
        private HttpListener? _listener;

        public bool IsServing { get; private set; }

        public void Serve(ServerConfig configuration, ILogger logger)
        {
            if (this.IsServing)
            {
                throw new ServerException("Already serving!");
            }

            RequestHandler handler = new(configuration, logger);

            this._listener = new();
            foreach (string prefix in configuration.ListenerPrefixes)
            {
                this._listener.Prefixes.Add(prefix);
            }

            try
            {
                this._listener.Start();
            }
            catch (HttpListenerException e)
            {
                this._listener.Close();

                switch (e.ErrorCode)
                {
                    case 5:
                        throw new SetupException(Resources.AdminPrivilegesRequiredForPrefix, e);
                    case 32:
                        throw new SetupException(Resources.PortOrPrefixAlreadyInUse);
                    default:
                        throw;
                }
            }

            // Only serve if there hasn't been an issue during setup.
            this.IsServing = true;

            handler.Handle(this._listener);
        }

        public void Stop() {
            this.IsServing = false;
            this._listener?.Stop();
            this._listener?.Close();
            this._listener = null;
        }
    }
}
