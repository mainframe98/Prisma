using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PrismaGUI.ViewModels;

namespace PrismaGUI.Views
{
    public partial class Update
    {
        private UpdateViewModel ViewModel => (UpdateViewModel)this.DataContext;
        private readonly Updater _updater;

        public Update(Updater updater)
        {
            this.InitializeComponent();
            this.ViewModel.NewVersion = updater.CachedNewVersion;
            this._updater = updater;
        }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) => this.Close();

        private void CanAlwaysExecuteHandler(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void VisitWebsite_Click(object sender, RoutedEventArgs e) => Utilities.LaunchBrowserForUri(Updater.RepositoryRoot + "/releases/latest");

        private async void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await this._updater.Update();
            }
            catch (Win32Exception exception)
            {
                Utilities.ApplicationLogger.Error(exception, "Executing the installer failed");
                MessageBox.Show(this, PrismaGUI.Properties.Resources.CannotRunInstaller, PrismaGUI.Properties.Resources.DownloadNewVersionManually + $"\n\n{exception.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
