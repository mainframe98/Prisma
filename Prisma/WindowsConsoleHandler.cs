using System;
using System.Runtime.InteropServices;

namespace Prisma;

/// <summary>
/// Handler for Windows console events through SetConsoleCtrlHandler.
///
/// This code is based on code from Gérald Barré: https://www.meziantou.net/detecting-console-closing-in-dotnet.htm.
/// </summary>
public static class WindowsConsoleHandler
{
    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

    private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

    /// <summary>
    /// Keep a reference to the actively set shutdown handler to prevent it from being garbage collected.
    /// </summary>
    private static Action? _activeShutdownHandlerHolder;

    private enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    public static void RegisterShutdownHandler(Action action)
    {
        _activeShutdownHandlerHolder = action;
        SetConsoleCtrlHandler(signal =>
        {
            switch (signal)
            {
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    action();
                    break;
            }

            return false;
        }, true);
    }
}