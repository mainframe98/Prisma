using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace PrismaGUI.Converters
{
    internal class RegexStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo _)
        {
            return value is Regex regex ? regex.ToString() : DependencyProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo _)
        {
            string? pattern = value?.ToString();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return DependencyProperty.UnsetValue;
            }

            try
            {
                // Give 2.5 seconds as maximum to prevent blocking the UI forever.
                return new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(2500));
            }
            catch (ArgumentException)
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
