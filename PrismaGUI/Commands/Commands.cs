using System.Windows.Input;
using PrismaGUI.Properties;

namespace PrismaGUI.Commands;

public static class Commands
{
    public static readonly RoutedUICommand CheckForUpdates = new(
        Resources.Check_for_Updates,
        Resources.Check_for_Updates,
        typeof(Commands)
    );

    public static readonly RoutedUICommand ClearLogs = new(
        Resources.Clear_logs,
        Resources.Clear_logs,
        typeof(Commands)
    );

    public static readonly RoutedUICommand DialogExit = new(
        Resources.Exit,
        Resources.Exit,
        typeof(Commands),
        new InputGestureCollection
        {
            new KeyGesture(Key.Escape)
        }
    );

    public static readonly RoutedUICommand Exit = new(
        Resources.Exit,
        Resources.Exit,
        typeof(Commands),
        new InputGestureCollection
        {
            new KeyGesture(Key.F4, ModifierKeys.Alt)
        }
    );

    public static readonly RoutedUICommand OpenAbout = new(
        Resources.About,
        Resources.About,
        typeof(Commands)
    );

    public static readonly RoutedUICommand Restart = new(
        Resources.Restart,
        Resources.Restart,
        typeof(Commands),
        new InputGestureCollection
        {
            new KeyGesture(Key.F5)
        }
    );

    public static readonly RoutedUICommand SelectDocumentRoot = new(
        Resources.Select_document_root,
        Resources.Select_document_root,
        typeof(Commands)
    );

    public static readonly RoutedUICommand SelectLogFile = new(
        Resources.Select_log_file,
        Resources.Select_log_file,
        typeof(Commands)
    );

    public static readonly RoutedUICommand ServerToggle = new(
        "Toggle server",
        "Toggle server",
        typeof(Commands)
    );

    public static readonly RoutedUICommand Start = new(
        Resources.Start,
        Resources.Start,
        typeof(Commands)
    );

    public static readonly RoutedUICommand Stop = new(
        Resources.StopServing,
        Resources.StopServing,
        typeof(Commands)
    );
}