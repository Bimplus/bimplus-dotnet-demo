using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace AllplanBimplusDemo.Controls
{
    public class IntegerTextBox : CultureInfoTextBox
    {
        public IntegerTextBox()
        {
            TextAlignment = TextAlignment.Right;

            LostFocus += IntegerTextBox_LostFocus;
            GotFocus += IntegerTextBox_GotFocus;
        }

        ~IntegerTextBox()
        {
            LostFocus -= IntegerTextBox_LostFocus;
            GotFocus -= IntegerTextBox_GotFocus;
            Trace.WriteLine("destructor IntegerTextBox");
        }

        private void IntegerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextAlignment = TextAlignment.Right;
            if (!string.IsNullOrEmpty(Text))
            {
                Int32 value;
                if (!Int32.TryParse(Text, out value))
                {
                    Background = new SolidColorBrush { Color = Colors.Red };
                    return;
                }
                else
                {
                    string valueString = value.ToString();
                    Text = ToText(value, true);

                    int newValue;
                    if (int.TryParse(valueString, out newValue))
                        IntValue = newValue;
                }
            }
            else
            {
                Text = null;
                IntValue = null;
            }

            Background = null;
        }

        private void IntegerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextAlignment = TextAlignment.Left;
            if (IntValue != null)
                Text = ToText(IntValue, false);
        }

        #region properties

        public static readonly DependencyProperty IntProperty = DependencyProperty.Register("IntValue", typeof(int?), typeof(IntegerTextBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    new PropertyChangedCallback(
                        (s, a) =>
                        {
                            IntegerTextBox control = s as IntegerTextBox;
                            if (a != null)
                                control.Text = control.ToText(a.NewValue as int?, true);
                        })));


        public int? IntValue
        {
            get
            {
                object value = GetValue(IntProperty);
                if (value != null && value is int)
                    return (int)value;
                else
                    return null;
            }
            set
            {
                SetValue(IntProperty, value);

                Text = ToText(value, true);
            }
        }

        #endregion properties

        #region private methods

        private string ToText(int? intValue, bool withGroupSeparator)
        {
            string result = null;

            if (intValue == null)
                return result;
            else
            {
                double value = (double)intValue;
                if (withGroupSeparator)
                    result = value.ToString("N0", CultureInfo);
                else
                    result = value.ToString("F0", CultureInfo);
            }

            return result;
        }

        #endregion private methods

        #region virtual methods

        protected override void CultureInfoChanged()
        {
            if (!IsFocused)
            {
                Text = ToText(IntValue, true);
            }
        }

        #endregion virtual methods

    }
}
