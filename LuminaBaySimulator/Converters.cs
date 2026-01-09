using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace LuminaBaySimulator
{
    /// <summary>
    /// Converte un valore Enum (es. GameViewMode.Map) in Visibility.Visible se corrisponde al parametro passato.
    /// Eredita da MarkupExtension per poter essere usato direttamente nel XAML come {local:EnumToVisConverter}.
    /// </summary>
    public class EnumToVisConverter : MarkupExtension ,IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();

            return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
