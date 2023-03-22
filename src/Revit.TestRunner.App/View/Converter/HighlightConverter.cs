using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Revit.TestRunner.App.View.Converter
{
    internal class HighlightConverter : IMultiValueConverter
    {
        public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
        {
            string input = values[0] as string;
            string highlight = values[1] as string;
            TextBlock result = new TextBlock();
            result.Style = values[2] as Style;

            if( !string.IsNullOrEmpty( input ) && !string.IsNullOrEmpty( highlight ) ) {
                var inputLower = input.ToLower();
                var highlightLower = highlight.ToLower();

                int startIndex = inputLower.IndexOf( highlightLower, StringComparison.Ordinal );
                int endIndex = startIndex + highlightLower.Length - 1;

                if( startIndex >= 0 ) {
                    result.Inlines.Add( input[..startIndex] );
                    result.Inlines.Add( new Run( input.Substring( startIndex, highlightLower.Length ) ) { Background = (SolidColorBrush)new BrushConverter().ConvertFrom( "#B5BECD" ) } );
                    result.Inlines.Add( input[(endIndex + 1)..] );
                }
            }

            if( result.Inlines.Count == 0 ) {
                result.Text = input;
            }

            return result;
        }

        public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}