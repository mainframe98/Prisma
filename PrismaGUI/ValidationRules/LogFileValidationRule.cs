using System;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using PrismaGUI.Properties;

namespace PrismaGUI.ValidationRules
{
    public class LogFileValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value.ToString();

            return new ValidationResult(
                string.IsNullOrWhiteSpace(input) || this.IsValidFilePath(input) && Path.IsPathRooted(input),
                Resources.ProvideValidLogFile
            );
        }

        private bool IsValidFilePath(string path)
        {
            try
            {
                new FileInfo(path);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
