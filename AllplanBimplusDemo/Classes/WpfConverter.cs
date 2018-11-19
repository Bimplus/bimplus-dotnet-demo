using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AllplanBimplusDemo.Classes
{
    #region InverseBooleanConverter

    /// <summary>
    /// Inverts a Boolean value
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    #region BimplusColorToWpfColorConverter

    [ValueConversion(typeof(uint?), typeof(/*SolidColorBrush*/Color))]
    public class BimplusColorToWpfColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint? uIntValue = (uint?)value;

            if (uIntValue != null)
            {
                Color color = ConvertUintToMediaColor(uIntValue);
                SolidColorBrush brush = new SolidColorBrush(color);

                return brush;
            }

            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static Color ConvertUintToMediaColor(uint? value)
        {
            Color color = Colors.Black;

            uint? uIntValue = value;
            if (uIntValue != null)
            {
                uint intValue = (uint)uIntValue;
                byte a = (byte)(intValue / 16777216);
                byte r = (byte)(intValue / 65536);
                byte g = (byte)(intValue / 256);
                byte b = (byte)(intValue);

                // Some alpha values are wrong.
                // Do not change the alpha value, otherwise IntegrationBase.ApiCore.Structures.PostStructure will not work.
                //if (a == 0)
                //    a = 255;

                color = Color.FromArgb(a, r, g, b);
            }

            return color;
        }
    }

    #endregion BimplusColorToWpfColorConverter
}
