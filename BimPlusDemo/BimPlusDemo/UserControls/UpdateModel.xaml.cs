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

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for UpdateModel.xaml
    /// </summary>
    public partial class UpdateModel : Window, INotifyPropertyChanged
    {
        private bool _replaceModel;
        public bool ReplaceModel
        {
            get => _replaceModel;
            set
            {
                _replaceModel = value;
                OnPropertyChanged();
            }
        }


        public bool Synchronize
        {
            get => !_replaceModel;
            set
            {
                _replaceModel = !value;
                OnPropertyChanged();
            }
        }
        private bool _createRevision;
        public bool CreateRevision
        {
            get => _createRevision;
            set
            {
                _createRevision = value;
                OnPropertyChanged();
            }
        }

        public UpdateModel(string modelName, string fileName)
        {
            InitializeComponent();
            DataContext = this;
            Synchronize = true;
            CreateRevision = false;
            Model.Content = modelName;
            FileName.Content = fileName;
        }

        private void Upload_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
