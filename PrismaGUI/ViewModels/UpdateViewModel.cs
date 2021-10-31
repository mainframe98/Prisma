using System;
using PrismaGUI.Properties;

namespace PrismaGUI.ViewModels
{
    public class UpdateViewModel
    {
        // Set a dummy value for the initialization in the view, which shouldn't throw an exception.
        internal Version? NewVersion { get; set; } = new(0, 0, 0, 0);

        public string NewVersionText => string.Format(Resources.NewVersionHeader, NewVersion?.ToString() ?? throw new ArgumentNullException(nameof(Updater.CachedNewVersion)));
    }
}
