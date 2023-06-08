using System.Collections.ObjectModel;
using System.Linq;
using Prisma.Config;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels;

public class CgiApplication : ClassWithPropertiesThatNotify
{
    private string _name;
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

    public CgiApplication()
    {
        this._name = "";
        this._path = "";
        this._arguments = new();
        this._environmentVariables = new();
    }

    public CgiApplication(string name, ApplicationConfig applicationConfig)
    {
        this._name = name;
        this._path = applicationConfig.Path;
        this._arguments = new ObservableCollection<ClassWithStringField>(applicationConfig.Arguments.Select(a => new ClassWithStringField(a)));
        this._environmentVariables = new ObservableCollection<EnvironmentVariable>(applicationConfig.EnvironmentVariables.Select(e => new EnvironmentVariable(e)));
    }

    public static implicit operator ApplicationConfig(CgiApplication application) => new()
    {
        Path = application.Path,
        Arguments = application.Arguments.Select(a => a.Value).ToList(),
        EnvironmentVariables = application.EnvironmentVariables.ToDictionary(a => a.Variable, a => a.Value)
    };
}