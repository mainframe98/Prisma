using System;
using System.Collections.ObjectModel;
using Prisma;
using PrismaGUI.Properties;

namespace PrismaGUI.ViewModels;

public class AboutViewModel
{
    public string LicenseText => $@"Copyright (c) {DateTime.Now.Year} Mainframe98

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";

    public string NameAndVersion => Resources.PrismaWebServer + " " + PrismaInfo.Version;

    public ObservableCollection<PrismaInfo.UsedComponent> UsedComponents  => new(PrismaInfo.UsedComponents)
    {
        new("Ookii.Dialogs.Wpf", "BSD-3-Clause", "https://github.com/ookii-dialogs/ookii-dialogs-wpf", PrismaInfo.GetVersion(typeof(Ookii.Dialogs.Wpf.VistaFileDialog))),
        new("SharpVectors", "BSD-3-Clause", "https://github.com/ElinamLLC/SharpVectors", PrismaInfo.GetVersion(typeof(SharpVectors.Runtime.SvgObject))),
        new("Serilog.Skins.RichTextBox.Wpf", "Apache-2.0", "https://github.com/serilog-contrib/serilog-sinks-richtextbox", PrismaInfo.GetVersion(typeof(Serilog.Sinks.RichTextBox.Themes.RichTextBoxConsoleTheme))),
        new("Inno Setup", "BSD", "https://jrsoftware.org/isinfo.php", new Version(6, 2, 0))
    };
}