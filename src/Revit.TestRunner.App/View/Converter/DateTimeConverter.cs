using System;
using System.Globalization;
using System.Windows.Data;

namespace Revit.TestRunner.App.View.Converter
{
    internal class DateTimeConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            var dateTime = (DateTime)value;

            return dateTime == DateTime.MinValue
                ? null
                : (object)dateTime.ToLocalTime();
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}