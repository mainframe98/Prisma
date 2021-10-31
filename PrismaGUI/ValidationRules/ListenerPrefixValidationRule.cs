using System.Globalization;
using System.Windows.Controls;
using PrismaGUI.Properties;

namespace PrismaGUI.ValidationRules
{
    public class ListenerPrefixValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value.ToString();
            if (
                string.IsNullOrWhiteSpace(input) ||
                !input.EndsWith('/') ||
                !input.StartsWith("http://") &&
                !input.StartsWith("https://")
            )
            {
                return new ValidationResult(false, Resources.InvalidPrefix);
            }
            else
            {
                return ValidationResult.ValidResult;
            }
        }
    }
}
