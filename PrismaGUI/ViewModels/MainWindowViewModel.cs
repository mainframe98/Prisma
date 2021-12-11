using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Prisma;
using Prisma.Config;
using Prisma.Exceptions;
using PrismaGUI.Properties;
using PrismaGUI.ViewModelHelpingClasses;
using PrismaGUI.ViewModels.SubModels;
using Serilog;
using Serilog.Events;

namespace PrismaGUI.ViewModels
{
    public class MainWindowViewModel : ClassWithPropertiesThatNotify, IDisposable
    {
        #region Command handlers

        internal void OnWindowClosing() => this.StopServer();

        internal void CanOnlyExecuteIfServerActiveHandler(CanExecuteRoutedEventArgs e) => e.CanExecute = this._server != null;
        internal void CanOnlyExecuteIfServerNotActiveHandler(CanExecuteRoutedEventArgs e) => e.CanExecute = this._server == null;

        internal void OnExecuteStart() => this.StartServer();

        internal void OnExecuteStop() => this.StopServer();

        internal void OnServerToggleExecuted()
        {
            if (this._server == null)
            {
                this.StartServer();
            }
            else
            {
                this.StopServer();
            }
        }

        internal void OnReloadExecuted()
        {
            this.StopServer();
            this.StartServer();
        }

        internal void OnOpenExecuted(Window owner)
        {
            string previous = this.ConfigFilePath;

            OpenFileDialog openFileDialog = new()
            {
                DefaultExt = "json",
                Filter = this.ConfigFileDialogFilter,
                InitialDirectory = this.FindFileOpenDialogInitialDirectory(this.ConfigFilePath),
                FileName = this.GetFileNameFromFilePathWithFallback(this.ConfigFilePath, DefaultConfigFileName),
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog(owner) != true)
            {
                return;
            }

            this.ConfigFilePath = openFileDialog.FileName;
            try
            {
                this._configuration = ServerConfig.LoadFrom(this.ConfigFilePath);
                // ServerConfig.Normalize will turn this into the working directory, which is fine on the command line,
                // but not for a GUI, as it will point to the location of the .exe.
                this._configuration.DocumentRoot = this.GetUsableDocumentRootWithFallback(this._configuration.DocumentRoot);
                this._configuration.Normalize();
                // Passing null to indicate all properties changed.
                this.NotifyPropertyChanged(null);
            }
            catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
            {
                this.ShowMessageBox(
                    Resources.CannotAccessConfigFileTitle,
                    Resources.CannotAccessConfigFile + $"\n\n{e.Message}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception e) when (e is InvalidConfigException or JsonException)
            {
                this.ShowMessageBox(
                    Resources.InvalidConfigFileTitle,
                    Resources.InvalidConfigFileTitle + $"\n\n{e.Message}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                this.ConfigFilePath = previous;
            }
        }

        internal void OnSaveAsExecuted(Window owner)
        {
            SaveFileDialog saveFileDialog = new()
            {
                DefaultExt = "json",
                Filter = this.ConfigFileDialogFilter,
                FileName = this.GetFileNameFromFilePathWithFallback(this.ConfigFilePath, DefaultConfigFileName),
                InitialDirectory = this.FindFileOpenDialogInitialDirectory(this.ConfigFilePath),
                OverwritePrompt = true,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog(owner) != true)
            {
                return;
            }

            this.ConfigFilePath = saveFileDialog.FileName;

            File.WriteAllText(this.ConfigFilePath, this._configuration.ToString());
        }

        internal void OnSelectDocumentRootExecuted(Window owner)
        {
            string selectedPath = this.GetUsableDocumentRootWithFallback(this.DocumentRoot);

            VistaFolderBrowserDialog documentRootDialog = new()
            {
                SelectedPath = selectedPath
            };

            if (documentRootDialog.ShowDialog(owner) != true)
            {
                return;
            }

            this.DocumentRoot = documentRootDialog.SelectedPath;
        }

        internal void OnSelectLogFileExecuted(Window owner)
        {
            OpenFileDialog openFileDialog = new()
            {
                DefaultExt = "log",
                Filter = Resources.LogFile + " (*.log)|*.log",
                InitialDirectory = this.FindFileOpenDialogInitialDirectory(this.LogFile),
                FileName = this.GetFileNameFromFilePathWithFallback(this.LogFile, "prisma.log"),
                CheckFileExists = false,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog(owner) != true)
            {
                return;
            }

            this.LogFile = openFileDialog.FileName;
        }

        internal void OnNewExecuted()
        {
            this._configuration = this.MakeNewConfig();
            this.ConfigFilePath = "";
            // All configuration has changed.
            this.NotifyPropertyChanged(null);
        }

        #endregion

        #region Helpers

        private const string DefaultConfigFileName = "prisma.config.json";

        private string ConfigFileDialogFilter => Resources.PrismaConfigurationFiles + " (*.json)|*.json";

        /// <summary>
        /// Event handler for showing message boxes that can use the View this ViewModel is invoked on as their owner.
        /// Solution slightly adapted from https://stackoverflow.com/a/6486469/6422957.
        /// </summary>
        internal event EventHandler<MessageBoxEventArgs>? MessageBox;

        /// <summary>
        /// Event handler for connecting the request logger to the rich text box in the UI.
        /// </summary>
        internal event EventHandler<NewLoggerEventArgs>? NewLogger;

        /// <summary>
        /// Show a message box.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="messageBoxText"></param>
        /// <param name="button"></param>
        /// <param name="image"></param>
        private void ShowMessageBox(string title, string messageBoxText, MessageBoxButton button, MessageBoxImage image)
        {
            this.MessageBox?.Invoke(this, new MessageBoxEventArgs(title, messageBoxText, button, image));
        }

        /// <summary>
        /// Start the server, with the current configuration loaded.
        /// </summary>
        private void StartServer()
        {
            // Serve with a cloned config to prevent changes during running from affecting the server, potentially breaking it.
            // TODO: Wouldn't it make more sense to make everything readonly instead? It's more effort though.
            ServerConfig configuration = (ServerConfig)this._configuration.Clone();
            LoggerConfiguration logConfig = configuration.Logging.MakeLoggerConfiguration();
            this.NewLogger?.Invoke(this, new NewLoggerEventArgs(logConfig));

            try
            {
                this._server = new Server(configuration, logConfig.CreateLogger());
                this._server.Start();
            }
            catch (HttpListenerException e)
            {
                string message;
                switch (e.ErrorCode)
                {
                    case 5:
                        message = Resources.AdminPrivilegesRequiredForPrefix;
                        break;
                    case 32:
                        message = Resources.PortOrPrefixAlreadyInUse;
                        break;
                    default:
                        throw;
                }

                this.ShowMessageBox(
                    Resources.FailedToStartServer,
                    message,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (SetupException e)
            {
                this.ShowMessageBox(
                    Resources.FailedToStartServer,
                    e.Message,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            this.TriggerServerStateChangeDependencyPropertiesEvent();
        }

        private void StopServer()
        {
            this._server?.Dispose();
            this._server = null;
            this.TriggerServerStateChangeDependencyPropertiesEvent();
        }

        /// <summary>
        /// Trigger the property changed event for properties that must change if the server starts or stops.
        /// </summary>
        private void TriggerServerStateChangeDependencyPropertiesEvent()
        {
            this.NotifyPropertyChanged(nameof(ServerStatus));
            this.NotifyPropertyChanged(nameof(ServeButtonText));
        }

        /// <summary>
        /// Get a usable directory for a file dialog.
        /// It will fallback to the document root or My Documents if the <paramref name="currentValue"/> is unavailable.
        /// </summary>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        private string FindFileOpenDialogInitialDirectory(string currentValue)
        {
            if (!string.IsNullOrWhiteSpace(currentValue) && Path.IsPathRooted(currentValue))
            {
                // Return currentValue if the result from Path.GetDirectoryName is null,
                // as it indicates currentValue is a root directory.
                return Path.GetDirectoryName(currentValue) ?? currentValue;
            }
            else if (!string.IsNullOrEmpty(this.DocumentRoot) && Path.IsPathRooted(this.DocumentRoot))
            {
                return this.DocumentRoot;
            }
            else
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        /// <summary>
        /// Retrieve a file name from the given path with a fallback if the path is invalid or doesn't contain a file name.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private string GetFileNameFromFilePathWithFallback(string path, string fallback)
        {
            if (Path.IsPathRooted(path))
            {
                string fileName = Path.GetFileName(path);

                if (fileName != "")
                {
                    return fileName;
                }
            }

            return fallback;
        }

        /// <summary>
        /// Get a usable path to the document root, with a fallback if the given path is invalid.
        ///
        /// It will fallback to the directory where the config file is located, or My Documents if not possible.
        /// </summary>
        /// <param name="documentRoot"></param>
        /// <returns></returns>
        private string GetUsableDocumentRootWithFallback(string? documentRoot)
        {
            if (!string.IsNullOrWhiteSpace(documentRoot) && Path.IsPathRooted(documentRoot))
            {
                return documentRoot;
            }

            if (Path.IsPathRooted(this.ConfigFilePath))
            {
                return Path.GetDirectoryName(this.ConfigFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        /// <summary>
        /// Construct a new configuration suitable for use in the UI.
        /// </summary>
        /// <returns></returns>
        private ServerConfig MakeNewConfig()
        {
            ServerConfig configuration = new();
            // Normalize to give some sensible defaults.
            configuration.Normalize();
            // But not the document root! That will select the current directory,
            // which for the GUI is the folder where the .exe is located.
            // That's the wrong location, and makes FindFileOpenDialogInitialDirectory use it as default when it shouldn't!
            configuration.DocumentRoot = "";

            return configuration;
        }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            this._configFilePath = "";
            this._configuration = this.MakeNewConfig();
        }

        #endregion

        #region Model

        private Server? _server;
        private ServerConfig _configuration;
        private string _configFilePath;

        #endregion

        #region Model backing properties

        public ushort Port
        {
            get => this._configuration.Port;
            set
            {
                this._configuration.Port = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool AllowPathInfo
        {
            get => this._configuration.AllowPathInfo;
            set
            {
                this._configuration.AllowPathInfo = value;
                this.NotifyPropertyChanged();
            }
        }
        public bool EnableCgiBin
        {
            get => this._configuration.EnableCgiBin;
            set
            {
                this._configuration.EnableCgiBin = value;
                this.NotifyPropertyChanged();
            }
        }
        public bool TreatCgiExtensionScriptsAsCgiScripts
        {
            get => this._configuration.TreatCgiExtensionScriptsAsCgiScripts;
            set
            {
                this._configuration.TreatCgiExtensionScriptsAsCgiScripts = value;
                this.NotifyPropertyChanged();
            }
        }

        public string DocumentRoot
        {
            get => this._configuration.DocumentRoot;
            set
            {
                this._configuration.DocumentRoot = value;
                this.NotifyPropertyChanged();
            }
        }

        public string LogFile
        {
            get => this._configuration.Logging.Path;
            set
            {
                this._configuration.Logging.Path = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool LogAsJson
        {
            get => this._configuration.Logging.LogAsJson;
            set
            {
                this._configuration.Logging.LogAsJson = value;
                this.NotifyPropertyChanged();
            }
        }

        public LogEventLevel LogLevel
        {
            get => this._configuration.Logging.Level;
            set
            {
                this._configuration.Logging.Level = value;
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<RewriteRule> RewriteRules
        {
            get => new(this._configuration.RewriteRules.Select(r => new RewriteRule(r.Key, r.Value)));
            set
            {
                this._configuration.RewriteRules = new Dictionary<Regex, string>(value.ToDictionary(r => r.Rule, r => r.RewriteTo));
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<FastCgiApplication> FastCgiApplications
        {
            get => new(this._configuration.FastCgiApplications.Select(a => new FastCgiApplication(a.Key, a.Value)));
            set
            {
                this._configuration.FastCgiApplications = value.ToDictionary(a => a.Name, a => (FastCgiApplicationConfig)a);
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(DefinedApplications));
            }
        }

        public ObservableCollection<CgiApplication> CgiApplications
        {
            get => new(this._configuration.CgiApplications.Select(a => new CgiApplication(a.Key, a.Value)));
            set
            {
                this._configuration.CgiApplications = value.ToDictionary(a => a.Name, a => (ApplicationConfig)a);
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(DefinedApplications));
            }
        }

        public ObservableCollection<PathInvocation> InvokeOnPath
        {
            get => new(this._configuration.InvokeOnPath.Select(i => new PathInvocation(i.Key, i.Value)));
            set
            {
                this._configuration.InvokeOnPath = value.ToDictionary(i => i.Path, i => i.Application);
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(DefinedApplications));
            }
        }

        public ObservableCollection<ExtensionInvocation> InvokeOnExtension
        {
            get => new(this._configuration.InvokeOnExtension.Select(i => new ExtensionInvocation(i.Key, i.Value)));
            set
            {
                this._configuration.InvokeOnExtension = value.ToDictionary(i => i.Extension, i => i.Application);
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(DefinedApplications));
            }
        }

        public ObservableCollection<ClassWithStringField> DefaultDocuments
        {
            get => new(this._configuration.DefaultDocuments.Select(d => new ClassWithStringField(d)));
            set
            {
                this._configuration.DefaultDocuments = value.Select(d => d.ToString()).ToList();
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<ClassWithStringField> ListenerPrefixes
        {
            get => new(this._configuration.ListenerPrefixes.Select(l => new ClassWithStringField(l)));
            set
            {
                this._configuration.ListenerPrefixes = value.Select(l => l.ToString()).ToList();
                this.NotifyPropertyChanged();
            }
        }

        #endregion

        #region Properties

        public string ConfigFilePath
        {
            get => this._configFilePath;
            set
            {
                this._configFilePath = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(Title));
            }
        }

        public string Title => string.Format(Resources.MainWindowTitle, this.ConfigFilePath == "" ? Resources.NoConfigurationLoaded : Path.GetFileName(this.ConfigFilePath));

        public string ServerStatus => this._server == null ? Resources.NotServing : string.Format(Resources.ServingAt, string.Join(", ", this._configuration.ListenerPrefixes));

        public string ServeButtonText => this._server == null ? Resources.StartServing : Resources.StopServing;

        public IEnumerable<string> DefinedApplications => this.FastCgiApplications.Select(a => a.Name)
            .Concat(this.CgiApplications.Select(a => a.Name))
            // Tack on applications defined in InvokeOn properties to make undefined applications actually show up in the dropdown.
            // This is a hack - WPF needs to know to display non ItemsSource values instead.
            .Concat(this.InvokeOnExtension.Select(i => i.Application))
            .Concat(this.InvokeOnPath.Select(i => i.Application))
            .Distinct()
            .Where(a => !string.IsNullOrWhiteSpace(a));

        #endregion

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            this._server?.Dispose();
        }
    }
}
