using System.Windows.Markup;
using Prisma.Config;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels
{
    public class FastCgiApplication : ClassWithPropertiesThatNotify
    {
        private string _name;
        private string _socket;

        [ConstructorArgument("name")]
        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                this.NotifyPropertyChanged();
            }
        }

        [ConstructorArgument("socket")]
        public string Socket
        {
            get => this._socket;
            set
            {
                this._socket = value;
                this.NotifyPropertyChanged();
            }
        }

        public FastCgiApplication() : this("", "") {}

        public FastCgiApplication(string name, string socket)
        {
            this._name = name;
            this._socket = socket;
        }

        public FastCgiApplication(string name, FastCgiApplicationConfig applicationConfig) : this(name, applicationConfig.Socket) {}

        public static implicit operator FastCgiApplicationConfig(FastCgiApplication application) => new()
        {
            Socket = application.Socket
        };
    }
}
