using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PrismaGUI.ViewModels;
using Serilog;
using Serilog.Sinks.RichTextBox.Themes;

namespace PrismaGUI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : IDisposable
{
    // This is an inverted version of the default Literate theme used by Serilog.Sinks.RichTextBox.Wpf.
    private readonly RichTextBoxTheme _theme = new RichTextBoxConsoleTheme(new Dictionary<RichTextBoxThemeStyle, RichTextBoxConsoleThemeStyle>
    {
        [RichTextBoxThemeStyle.Text] = new() { Foreground = "#000000" },
        [RichTextBoxThemeStyle.SecondaryText] = new() { Foreground = "#808080" },
        [RichTextBoxThemeStyle.TertiaryText] = new() { Foreground = "#c0c0c0" },
        [RichTextBoxThemeStyle.Invalid] = new() { Foreground = "#808000" },
        [RichTextBoxThemeStyle.Null] = new() { Foreground = "#000080" },
        [RichTextBoxThemeStyle.Name] = new() { Foreground = "#808080" },
        [RichTextBoxThemeStyle.String] = new() { Foreground = "#008080" },
        [RichTextBoxThemeStyle.Number] = new() { Foreground = "#800080" },
        [RichTextBoxThemeStyle.Boolean] = new() { Foreground = "#000080" },
        [RichTextBoxThemeStyle.Scalar] = new() { Foreground = "#008000" },
        [RichTextBoxThemeStyle.LevelVerbose] = new() { Foreground = "#808080" },
        [RichTextBoxThemeStyle.LevelDebug] = new() { Foreground = "#808080" },
        [RichTextBoxThemeStyle.LevelInformation] = new() { Foreground = "#000000" },
        [RichTextBoxThemeStyle.LevelWarning] = new() { Foreground = "#808000" },
        [RichTextBoxThemeStyle.LevelError] = new() { Foreground = "#ffffff", Background = "#ff0000" },
        [RichTextBoxThemeStyle.LevelFatal] = new() { Foreground = "#ffffff", Background = "#ff0000" }
    });

    private readonly Updater _updater;

    private MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

    public MainWindow()
    {
        this.InitializeComponent();
        this.ViewModel.MessageBox += (_, e) => e.Show(this);
        this.ViewModel.NewLogger += (_, e) => e.LoggerConfiguration.WriteTo.RichTextBox(this.LoggingBox, theme: this._theme);
        this._updater = new Updater();
        // Clear logs at startup because the document of the RichTextBox is not empty upon creation.
        this.LoggingBox.Document.Blocks.Clear();
        this.Dispatcher.ShutdownStarted += (_, _) => this.Dispose();
    }

    #region Code Behind logic for WPF shortcomings

    private bool IsUShort(string? input) => !string.IsNullOrWhiteSpace(input) && ushort.TryParse(input.Trim(), out ushort _);

    private void Port_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !this.IsUShort(e.Text);

    private void Port_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)) || !this.IsUShort(e.DataObject.GetData(typeof(string)) as string))
        {
            e.CancelCommand();
        }
    }

    #endregion

    #region View specific implementations, or not worth redirecting to the ViewModel

    private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) => this.Close();

    private void CanAlwaysExecuteHandler(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

    private void About_Executed(object sender, ExecutedRoutedEventArgs e) => new About() { Owner = this }.ShowDialog();

    private void Help_Executed(object sender, ExecutedRoutedEventArgs e) => Utilities.LaunchBrowserForUri(Updater.RepositoryRoot + "/wiki");

    private void ClearLogs_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = this.LoggingBox?.Document.Blocks.Count > 0;

    private void ClearLogs_Executed(object sender, ExecutedRoutedEventArgs e) => this.LoggingBox.Document.Blocks.Clear();

    private async void CheckForUpdates_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (await this._updater.HasUpdates())
        {
            new Update(this._updater)
            {
                Owner = this
            }.ShowDialog();
        }
        else
        {
            MessageBox.Show(
                this,
                PrismaGUI.Properties.Resources.YouHaveTheLatestVersion,
                PrismaGUI.Properties.Resources.NoUpdatesAvailable,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }

    #endregion

    #region Redirects to the ViewModel

    private void CanOnlyExecuteIfServerNotActiveHandler(object sender, CanExecuteRoutedEventArgs e) => this.ViewModel.CanOnlyExecuteIfServerNotActiveHandler(e);

    private void CanOnlyExecuteIfServerActiveHandler(object sender, CanExecuteRoutedEventArgs e) => this.ViewModel.CanOnlyExecuteIfServerActiveHandler(e);

    private void Restart_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnReloadExecuted();

    private void ServerToggle_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnServerToggleExecuted();

    private void Window_Closing(object sender, CancelEventArgs e) => this.ViewModel.OnWindowClosing();

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnOpenExecuted(this);

    private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnSaveAsExecuted(this);

    private void SelectDocumentRoot_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnSelectDocumentRootExecuted(this);

    private void SelectLogFile_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnSelectLogFileExecuted(this);

    private void Stop_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnExecuteStop();

    private void Start_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnExecuteStart();

    private void New_Executed(object sender, ExecutedRoutedEventArgs e) => this.ViewModel.OnNewExecuted();

    #endregion

    public void Dispose()
    {
        this._updater.Dispose();
        this.ViewModel.Dispose();
    }
}