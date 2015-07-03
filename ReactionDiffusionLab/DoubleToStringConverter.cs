using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace ReactionDiffusionLab
{
    [ValueConversion( typeof( double ), typeof( string ) )]
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( value is double )
            {
                string val = String.Format( "{0:#,#0.00}", value );

                return val;
            }

            return null;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return null;
        }
    }
}

