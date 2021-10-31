using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrismaGUI.ViewModelHelpingClasses
{
    public abstract class ClassWithPropertiesThatNotify : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifies that the <paramref name="propertyName"/> property was changed.
        /// </summary>
        /// <param name="propertyName">Name of the property. Omit to have the compiler provide this, pass <code>null</code> to refer to all.</param>
        protected void NotifyPropertyChanged([CallerMemberName]string? propertyName = "") => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }
}
