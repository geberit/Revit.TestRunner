using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Revit.TestRunner.App.View.Converter
{
    public class BoolToHiddenConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            bool isVisibleValue = true;
            bool val = (bool)value;

            if( parameter != null ) isVisibleValue = bool.Parse( parameter.ToString() );
            return val == isVisibleValue ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}
