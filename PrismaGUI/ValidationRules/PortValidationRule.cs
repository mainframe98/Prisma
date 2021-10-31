using System.Globalization;
using System.Windows.Controls;
using PrismaGUI.Properties;

namespace PrismaGUI.ValidationRules
{
    public class PortValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return new ValidationResult(ushort.TryParse(value.ToString(), out _), Resources.InvalidPortNumber);
        }
    }
}
