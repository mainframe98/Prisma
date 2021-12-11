using System;
using System.Windows;

namespace PrismaGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
        }

        private void OnCurrentDomainOnUnhandledException(object _, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;
            Utilities.ApplicationLogger.Fatal(exception, "Unhandled exception");

            string messageBoxText = PrismaGUI.Properties.Resources.InternalErrorOccurred + $"\n\n{exception.Message}";
            string title = PrismaGUI.Properties.Resources.InternalErrorTitle;

            if (this.MainWindow is null)
            {
                MessageBox.Show(messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(this.MainWindow, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            (this.MainWindow as IDisposable)?.Dispose();
        }
    }
}
