using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace PrismaGUI.Localization
{
    /// <summary>
    /// This class and <see cref="LocalizationData"/> are based on code from https://www.wpftutorial.net/LocalizeMarkupExtension.html.
    /// </summary>
    public class Localize : MarkupExtension
    {
        [ConstructorArgument("key")]
        public string Key { get; set; }

        public Localize(string key)
        {
            this.Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding("Value")
            {
                Source = new LocalizationData(this.Key)
            }.ProvideValue(serviceProvider);
        }
    }
}
