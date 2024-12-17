using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BimPlusDemo.Controls
{
    public class DateTimeTextBox : CultureInfoTextBox
    {
        public DateTimeTextBox()
        {
            LostFocus += DateTextBox_LostFocus;
        }

        ~DateTimeTextBox()
        {
            LostFocus -= DateTextBox_LostFocus;
            Trace.WriteLine("destructor DateTimeTextBox");
        }

        private void DateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(CultureInfo.CurrentCulture.DisplayName);
            if (!string.IsNullOrEmpty(Text))
            {
                if (!DateTime.TryParse(Text, out var dateTime))
                {
                    Background = new SolidColorBrush { Color = Colors.Red };
                    return;
                }
                else
                {
                    Text = ToText(dateTime);
                    DateTimeValue = dateTime;
                }
            }
            Background = null;
        }

        #region properties

        public static readonly DependencyProperty DateTimeProperty = DependencyProperty.Register("DateTimeValue", typeof(DateTime?), typeof(DateTimeTextBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    new PropertyChangedCallback(
                        (s, a) =>
                        {
                            {
                                if (s is DateTimeTextBox control) control.Text = control.ToText(a.NewValue as DateTime?);
                            }
                        })));

        public DateTime? DateTimeValue
        {
            get
            {
                object value = GetValue(DateTimeProperty);
                if (value is DateTime time)
                    return time;
                else
                    return null;
            }
            set
            {
                SetValue(DateTimeProperty, value);

                Text = ToText(value);
            }
        }

        #endregion properties

        #region private methods

        private string ToText(DateTime? dateTimeValue)
        {
            if (dateTimeValue != null)
            {
                DateTime dateTime = (DateTime)dateTimeValue;

                CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                string shortDateFormat = cultureInfo.DateTimeFormat.ShortDatePattern;

                return dateTime.ToString(shortDateFormat);
            }
            return String.Empty;
        }

        #endregion private methods

        #region virtual methods

        protected override void CultureInfoChanged()
        {
            if (!IsFocused)
                Text = ToText(DateTimeValue);
        }

        #endregion virtual methods
    }
}
