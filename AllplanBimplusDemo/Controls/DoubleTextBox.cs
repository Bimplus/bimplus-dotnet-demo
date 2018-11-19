using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace AllplanBimplusDemo.Controls
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

        private string _numberDecimalSeparator;

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
                double value;
                if (!double.TryParse(Text, out value))
                {
                    Background = new SolidColorBrush { Color = Colors.Red };
                    return;
                }
                else
                {
                    string valueString = value.ToString(CultureInfo.CurrentCulture);
                    Text = ToText(value, true);

                    double newValue;
                    if (double.TryParse(valueString, out newValue))
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
                            DoubleTextBox control = s as DoubleTextBox;
                            if (a != null)
                                control.Text = control.ToText(a.NewValue as double?, true);
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
            string result = null;

            if (doubleValue == null)
                return result;
            else
            {
                double value = (double)doubleValue;
                if (withGroupSeparator)
                    result = value.ToString("N10", CultureInfo);
                else
                    result = value.ToString("F10", CultureInfo);
            }

            int separatorIndex = result.IndexOf(_numberDecimalSeparator);

            if (separatorIndex > -1)
            {
                int lastNonZeroIndex = result.IndexOf("0", separatorIndex);

                if (lastNonZeroIndex > separatorIndex + 1)
                    result = result.Remove(lastNonZeroIndex);
                else
                    result = result.Remove(separatorIndex);
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
