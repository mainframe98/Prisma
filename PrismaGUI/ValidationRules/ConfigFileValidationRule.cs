using System.Globalization;
using System.IO;
using System.Windows.Controls;
using PrismaGUI.Properties;

namespace PrismaGUI.ValidationRules
{
    public class ConfigFileValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value.ToString();

            return new ValidationResult(
                string.IsNullOrWhiteSpace(input) || Path.IsPathRooted(input) && File.Exists(input),
                Resources.ProvidePathToFile
            );
        }
    }
}
