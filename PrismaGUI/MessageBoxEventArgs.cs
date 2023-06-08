using System;
using System.Windows;

namespace PrismaGUI;

internal class MessageBoxEventArgs : EventArgs
{
    private readonly string _title;
    private readonly string _messageBoxText;
    private readonly MessageBoxButton _button;
    private readonly MessageBoxImage _image;

    public MessageBoxEventArgs(string title, string messageBoxText, MessageBoxButton button, MessageBoxImage image)
    {
        this._title = title;
        this._messageBoxText = messageBoxText;
        this._button = button;
        this._image = image;
    }

    public void Show(Window owner) => MessageBox.Show(owner, this._messageBoxText, this._title, this._button, this._image);
}