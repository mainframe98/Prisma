using PrismaGUI.Properties;

namespace PrismaGUI.Localization
{
    /// <summary>
    /// This class and <see cref="Localize"/> are based on code from https://www.wpftutorial.net/LocalizeMarkupExtension.html.
    /// </summary>
    public class LocalizationData
    {
        private readonly string _key;

        public LocalizationData(string key)
        {
            this._key = key;
        }

        public object Value => Resources.ResourceManager.GetString(this._key) ?? $"(No translation has been defined for \"{this._key}\")";
    }
}
