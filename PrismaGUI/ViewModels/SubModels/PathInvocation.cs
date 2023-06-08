using System.Windows.Markup;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels;

public class PathInvocation : ClassWithPropertiesThatNotify
{
    private string _application;
    private string _path;

    [ConstructorArgument("path")]
    public string Path
    {
        get => this._path;
        set
        {
            this._path = value;
            this.NotifyPropertyChanged();
        }
    }

    [ConstructorArgument("application")]
    public string Application
    {
        get => this._application;
        set
        {
            this._application = value;
            this.NotifyPropertyChanged();
        }
    }

    public PathInvocation() : this("", "") {}

    public PathInvocation(string path, string application)
    {
        this._path = path;
        this._application = application;
    }
}