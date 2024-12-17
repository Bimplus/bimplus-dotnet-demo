using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace BimPlusDemo.Controls
{
    public class CultureInfoTextBox : TextBox
    {
        public CultureInfoTextBox()
        {
            CultureInfo = ParentCultureInfo;
            VerticalAlignment = VerticalAlignment.Center;
            Loaded += CultureInfoTextBox_Loaded;
            Unloaded += CultureInfoTextBox_Unloaded;
            TextChanged += TextChangedEventHandler;
        }

        ~CultureInfoTextBox()
        {
            TextChanged -= TextChangedEventHandler;
            Trace.WriteLine("destructor CultureInfoTextBox");
        }

        private void CultureInfoTextBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void CultureInfoTextBox_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Locale)
            {
                // Refresh CurrentCulture
                CultureInfo.CurrentCulture.ClearCachedData();

                string cultureName = CultureInfo.CurrentCulture.DisplayName;
                if (CultureInfo == null || cultureName != CultureInfo.DisplayName)
                {
                    CultureInfo = CultureInfo.CurrentCulture;
                }
            }
        }

        #region properties

        public static CultureInfo ParentCultureInfo { get; set; }

        private CultureInfo _cultureInfo;

        public CultureInfo CultureInfo
        {
            get => _cultureInfo;

            set
            {
                if (Equals(_cultureInfo, value)) 
                    return;
                _cultureInfo = value;
                CultureInfoChanged();
            }
        }

        #endregion properties

        protected virtual void CultureInfoChanged()
        {
        }

        // TextChangedEventHandler delegate method.
        private void TextChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            ;
        } // end textChangedEventHandler
    }
}
