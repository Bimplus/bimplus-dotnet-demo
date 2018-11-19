using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AllplanBimplusDemo.Windows
{
    /// <summary>
    /// Interaction logic for TextInputWindow.xaml
    /// </summary>
    public partial class TextInputWindow : Window, INotifyPropertyChanged
    {
        public TextInputWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region properties

        private string _stringValue;

        public string StringValue
        {
            get { return _stringValue; }

            set { _stringValue = value; NotifyPropertyChanged(); }
        }

        #endregion properties

        #region events

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox.Focus();
        }

        #endregion events

        public static string GetTextInput(string title)
        {
            string result = "";

            TextInputWindow window = new TextInputWindow { Title = title };

            bool? ok = window.ShowDialog();
            if (ok == true)
                result = window.StringValue;

            return result;
        }

    }
}
