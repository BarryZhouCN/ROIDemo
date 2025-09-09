using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace WPFAdorner.Converters
{
    public class PointToStringConverter:MarkupExtension,IValueConverter
    {
        private PointToStringConverter _pointToStringConverter;
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _pointToStringConverter = _pointToStringConverter ?? new PointToStringConverter();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point result = new Point();
            if (value is Point point)
            {
                result = point;
            }
            return $"坐标：{result.X} , {result.Y}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
