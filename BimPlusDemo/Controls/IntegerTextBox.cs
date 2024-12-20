using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace BimPlusDemo.Controls
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
                if (!int.TryParse(Text, out var value))
                {
                    Background = new SolidColorBrush { Color = Colors.Red };
                    return;
                }

                string valueString = value.ToString();
                Text = ToText(value, true);

                if (int.TryParse(valueString, out var newValue))
                    IntValue = newValue;
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
                            {
                                if (s is IntegerTextBox control) control.Text = control.ToText(a.NewValue as int?, true);
                            }
                        })));


        public int? IntValue
        {
            get
            {
                object value = GetValue(IntProperty);
                if (value is int iv)
                    return iv;
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
            if (intValue == null)
                return string.Empty;
            double value = (double)intValue;
            var result = value.ToString(withGroupSeparator ? "N0" : "F0", CultureInfo);
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
