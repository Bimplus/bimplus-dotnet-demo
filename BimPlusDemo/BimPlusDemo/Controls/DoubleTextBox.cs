using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BimPlusDemo.Controls
{
    public class DoubleTextBox : CultureInfoTextBox
    {
        public DoubleTextBox()
        {
            SetNumberDecimalSeparator();

            TextAlignment = TextAlignment.Right;

            LostFocus += DoubleTextBox_LostFocus;
            GotFocus += DoubleTextBox_GotFocus;
        }

        ~DoubleTextBox()
        {
            LostFocus -= DoubleTextBox_LostFocus;
            GotFocus -= DoubleTextBox_GotFocus;
            Trace.WriteLine("destructor DoubleTextBox");
        }

        private string _numberDecimalSeparator = string.Empty;

        private void DoubleTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextAlignment = TextAlignment.Left;
            if (DoubleValue != null)
                Text = ToText(DoubleValue, false);
        }

        private void DoubleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextAlignment = TextAlignment.Right;
            if (!string.IsNullOrEmpty(Text))
            {
                if (!double.TryParse(Text, out var value))
                {
                    Background = new SolidColorBrush { Color = Colors.Red };
                    return;
                }
                else
                {
                    string valueString = value.ToString(CultureInfo.CurrentCulture);
                    Text = ToText(value, true);

                    if (double.TryParse(valueString, out var newValue))
                        DoubleValue = newValue;
                }
            }
            else
            {
                Text = null;
                DoubleValue = null;
            }

            Background = null;
        }

        #region properties

        public static readonly DependencyProperty DoubleProperty = DependencyProperty.Register("DoubleValue", typeof(double?), typeof(DoubleTextBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    new PropertyChangedCallback(
                        (s, a) =>
                        {
                            {
                                if (s is DoubleTextBox control) control.Text = control.ToText(a.NewValue as double?, true);
                            }
                        })));

        public double? DoubleValue
        {
            get
            {
                object value = GetValue(DoubleProperty);
                if (value != null && value is double)
                    return (double)value;
                else
                    return null;
            }
            set
            {
                SetValue(DoubleProperty, value);

                Text = ToText(value, true);
            }
        }

        #endregion properties

        #region private methods

        private string ToText(double? doubleValue, bool withGroupSeparator)
        {
            if (doubleValue == null)
                return string.Empty;

            double value = (double)doubleValue;
            string result = (withGroupSeparator)
                ? value.ToString("N10", CultureInfo)
                : value.ToString("F10", CultureInfo);


            int separatorIndex = result.IndexOf(_numberDecimalSeparator, StringComparison.Ordinal);

            if (separatorIndex > -1)
            {
                int lastNonZeroIndex = result.IndexOf("0", separatorIndex, StringComparison.Ordinal);

                result = result.Remove(lastNonZeroIndex > separatorIndex + 1 ? lastNonZeroIndex : separatorIndex);
            }

            return result;
        }

        private void SetNumberDecimalSeparator()
        {
            NumberFormatInfo numberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;
            _numberDecimalSeparator = numberFormatInfo.NumberDecimalSeparator;
        }

        #endregion private methods

        #region virtual methods

        protected override void CultureInfoChanged()
        {
            SetNumberDecimalSeparator();
            if (!IsFocused)
            {
                Text = ToText(DoubleValue, true);
            }
        }

        #endregion virtual methods
    }
}
