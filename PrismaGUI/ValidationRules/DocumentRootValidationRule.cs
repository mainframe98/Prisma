using System.Globalization;
using System.IO;
using System.Windows.Controls;
using PrismaGUI.Properties;

namespace PrismaGUI.ValidationRules
{
    public class DirectoryValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return new ValidationResult(
                Path.IsPathRooted(value.ToString()) && Directory.Exists(value.ToString()),
                Resources.ProvideValidDirectory
            );
        }
    }
}
