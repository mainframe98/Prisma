using System.Windows.Markup;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels;

public class ExtensionInvocation : ClassWithPropertiesThatNotify
{
    private string _extension;
    private string _application;

    [ConstructorArgument("extension")]
    public string Extension
    {
        get => this._extension;
        set
        {
            this._extension = value;
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

    public ExtensionInvocation() : this("", "") {}

    public ExtensionInvocation(string extension, string application)
    {
        this._extension = extension;
        this._application = application;
    }
}