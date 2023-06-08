using System.Windows;
using System.Windows.Navigation;

namespace PrismaGUI.Views;

/// <summary>
/// Interaction logic for About.xaml
/// </summary>
public partial class About
{
    public About()
    {
        this.InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) => Utilities.LaunchBrowserForUri(e.Uri);
}