using System;
using System.Globalization;
using System.Windows.Data;

namespace Revit.TestRunner.App.View.Converter
{
    internal class TimeSpanConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            var dateTime = (TimeSpan)value;

            return dateTime == TimeSpan.Zero
                ? null
                 : (object)dateTime   ;

        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}