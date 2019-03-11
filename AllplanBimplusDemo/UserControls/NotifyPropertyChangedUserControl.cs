using AllplanBimplusDemo.WinForms;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace AllplanBimplusDemo.UserControls
{
    public class NotifyPropertyChangedUserControl : UserControl, INotifyPropertyChanged
    {
        public NotifyPropertyChangedUserControl()
        {
            IsVisibleChanged += NotifyPropertyChangedUserControl_IsVisibleChanged;
            ProgressWindow = new ProgressWindow();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region properties

        private ProgressWindow _progressWindow;

        protected ProgressWindow ProgressWindow
        {
            get { return _progressWindow; }
            set
            {
                _progressWindow = value;
            }
        }

        protected bool ChangesSaved { get; set; }

        #endregion properties

        private void NotifyPropertyChangedUserControl_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool)
            {
                bool isVisible = (bool)e.NewValue;
                if (!isVisible && ProgressWindow != null)
                {
                    ProgressWindow.Hide();
                    ProgressWindow = null;
                    IsVisibleChanged -= NotifyPropertyChangedUserControl_IsVisibleChanged;
                }
            }
        }
    }
}
