using System.Collections.ObjectModel;
using System.Linq;
using Prisma.Config;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels
{
    public class FastCgiApplication : ClassWithPropertiesThatNotify
    {
        private string _name;
        private string _socket;
        private string _path;
        private ObservableCollection<ClassWithStringField> _arguments;
        private ObservableCollection<EnvironmentVariable> _environmentVariables;

        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                this.NotifyPropertyChanged();
            }
        }

        public string Socket
        {
            get => this._socket;
            set
            {
                this._socket = value;
                this.NotifyPropertyChanged();
            }
        }

        public string Path
        {
            get => this._path;
            set
            {
                this._path = value;
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<ClassWithStringField> Arguments
        {
            get => this._arguments;
            set
            {
                this._arguments = value;
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<EnvironmentVariable> EnvironmentVariables
        {
            get => this._environmentVariables;
            set
            {
                this._environmentVariables = value;
                this.NotifyPropertyChanged();
            }
        }

        public FastCgiApplication()
        {
            this._name = "";
            this._socket = "";
            this._path = "";
            this._arguments = new();
            this._environmentVariables = new();
        }

        public FastCgiApplication(string name, FastCgiApplicationConfig applicationConfig)
        {
            this._name = name;
            this._socket = applicationConfig.Socket;
            this._path = applicationConfig.LaunchConfiguration.Path;
            this._arguments = new ObservableCollection<ClassWithStringField>(applicationConfig.LaunchConfiguration.Arguments.Select(a => new ClassWithStringField(a)));
            this._environmentVariables = new ObservableCollection<EnvironmentVariable>(applicationConfig.LaunchConfiguration.EnvironmentVariables.Select(e => new EnvironmentVariable(e)));
        }

        public static implicit operator FastCgiApplicationConfig(FastCgiApplication application) => new()
        {
            Socket = application.Socket,
            LaunchConfiguration = new()
            {
                Path = application.Path,
                Arguments = application.Arguments.Select(a => a.Value).ToList(),
                EnvironmentVariables = application.EnvironmentVariables.ToDictionary(a => a.Variable, a => a.Value)
            }
        };
    }
}
