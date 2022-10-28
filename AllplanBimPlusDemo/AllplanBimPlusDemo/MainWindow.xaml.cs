using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Ribbon;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlusDemo.UserControls;
// ReSharper disable RedundantExtendsListEntry

namespace BimPlusDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        //private static readonly ILog Logger = LogManager.GetLogger(typeof(MainWindow));

        private static readonly Guid TestApplicationId = new Guid("7B8E7315-7F69-40DB-B340-03782B8BCB12");
        private readonly IntegrationBase _intBase;
        private readonly WebViewer _webViewer;
        private readonly List<Guid> _selectedObjects;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            _selectedObjects = new List<Guid>();
            _intBase = new IntegrationBase(TestApplicationId, GetStreamWriter() )
            {
                UseSignalRCore = true,
                SignalRAppCode = $"BimPlusClient-{TestApplicationId}.SignalRCore",
                SignalRLogFileName = $"BimPlusDemo-{TestApplicationId}",
                SignalREnableUIMessages = true
            };

            if (_intBase.ConnectWithRememberMeAndClientId())
            {
                Login.SmallImageSource =
                    new BitmapImage(new Uri(@"/BimPlusDemo;component/Images/logout.png", UriKind.Relative));
                Login.Label = _intBase.UserName;
                ButtonsEnabled = true;
            }
            else
            {
                ButtonsEnabled = false;
            }

            _webViewer = new WebViewer(_intBase);
            _webViewer.WebViewRecreated += (sender, args) =>
            {
                if (!(sender is WebViewer webViewer))
                    return;
                if (_intBase.CurrentProject != null)
                    webViewer.NavigateToControl(_intBase.CurrentProject.Id);
            };

            _intBase.EventHandlerCore.DataLoaded += (sender, args) =>
            {
                //ProgressWindow.Hide();
                ButtonsEnabled = true;
            };

            _intBase.EventHandlerCore.ExportStarted += (sender, args) =>
            {
                NavigateToControl();
            };

            _intBase.EventHandlerCore.ModelModified += (sender, args) =>
            {
                if (args?.Value == "ModelModified")
                    NavigateToControl();
                else if (args?.Value == "SelectObject")
                {
                    _webViewer.HighlightObjectByID(args.Id, throwOnError:false, isolate:true, blockProperties:false);
                    if (args.Selected.GetValueOrDefault(false))
                        _webViewer.ObjectZoomToFit(args.Id);
                }
            };



            _intBase.EventHandlerCore.ObjectSelected += EventHandlerCoreOnObjectSelected;
            _intBase.EventHandlerCore.ObjectsSelected  += EventHandlerCoreOnObjectsSelected;            // set content control 
            //_intBase.EventHandlerCore.SignalRImportProgress += EventHandlerCoreOnSignalRImportProgress;

            Viewer.Content = _webViewer;

            // assign defaultImage
            ThumbnailIcon.LargeImageSource = new BitmapImage(new Uri("/BimPlusDemo;component/images/defaultImage.png",
                UriKind.RelativeOrAbsolute));
        }

        #region logging
        StreamWriter GetStreamWriter()
        {
            string loggingPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}//BimPlusDemo";
            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            if (Directory.Exists(loggingPath))
            {
                string fileName = Path.Combine(loggingPath, "BimPlusDemo.Log");

                var fileStream = File.Exists(fileName)
                    ? new FileStream(fileName, FileMode.Append)
                    : new FileStream(fileName, FileMode.Create);

                return new StreamWriter(fileStream);
            }
            return null;
        }

        #endregion

#region GlobalEventHandler
private void EventHandlerCoreOnObjectsSelected(object sender, BimPlusEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void EventHandlerCoreOnObjectSelected(object sender, BimPlusEventArgs e)
        {
            if (e == null || e.Id == Guid.Empty)
                return;
            if (_selectedObjects.Contains(e.Id))
                return;

            if (e.Multiselect == false)
                _selectedObjects.Clear();

            _selectedObjects.Add(e.Id);
        }

        #endregion

        public bool IsLoggedIn => _intBase.ClientConfiguration.AuthorizationAccessToken != Guid.Empty;

        public Guid ProjectId => _intBase?.CurrentProject.Id ?? Guid.Empty;

        #region EventsHandler

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _buttonsEnabled;
        public bool ButtonsEnabled
        {
            get => _buttonsEnabled;

            set
            {
                if (_buttonsEnabled == value) return;
                _buttonsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private bool _projectSelected;

        public bool ProjectSelected
        {
            get => _projectSelected;
            set
            {
                _projectSelected = value;
                NotifyPropertyChanged();
            }
        }

        private Image _imageOpenClose;

        public Image Thumbnail
        {
            get => _imageOpenClose;
            set
            {
                _imageOpenClose = value;
                NotifyPropertyChanged();
            }
        }

        private string _enabledContent;
        // ReSharper disable ExplicitCallerInfoArgument
        public string EnabledContent
        {
            get => _enabledContent;
            set
            {
                _enabledContent = value;
                NotifyPropertyChanged("UsersSelected");
                NotifyPropertyChanged("IssuesSelected");
                NotifyPropertyChanged("DocumentsSelected");
                NotifyPropertyChanged("ModelsSelected");
                NotifyPropertyChanged("AttributesSelected");
            }
        }

        public bool UsersSelected => EnabledContent == "UsersView";
        public bool IssuesSelected => EnabledContent == "Issues";
        public bool DocumentsSelected => EnabledContent == "Documents";
        public bool ModelsSelected => EnabledContent == "Models";
        public bool AttributesSelected => EnabledContent == "Attributes";

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //Debug.WriteLine(propertyName);
        }

        #endregion EventsHandler


        private void Login_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is RibbonButton login)) return;

            if (IsLoggedIn)
            {
                if (MessageBox.Show("Do you like to logout and login with a different account?", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                if (_intBase.Disconnect() == HttpStatusCode.OK)
                {
                    DisposeContentControl();
                    _intBase.CurrentProject = null;
                    ButtonsEnabled = false;
                    ProjectSelected = false;
                    _webViewer.Reload();
                }
            }

            _intBase.ConnectWithLoginDialog();
            if (IsLoggedIn)
            {
                login.Label = _intBase.UserName;
                var uri = new Uri(@"/BimPlusDemo;component/Images/logout.png", UriKind.Relative);
                login.SmallImageSource = new BitmapImage(uri);
            }
            else
            {
                login.Label = "Login";
                var uri = new Uri(@"/BimPlusDemo;component/Images/login.png", UriKind.Relative);
                login.SmallImageSource = new BitmapImage(uri);
            }

            ButtonsEnabled = IsLoggedIn;
        }


        private void ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!(Viewer.Content is Control control))
                return;
            control.Width = Viewer.ActualWidth;
            control.Height = Viewer.ActualHeight;
        }

        private void NavigateToControl()
        {
            ButtonsEnabled = false;
            _webViewer.NavigateToControl(_intBase.CurrentProject.Id);
        }

        private void DisposeContentControl()
        {
            if (ContentControl.Content is DocumentView documentView)
            {
                //documentView.Dispose()
                ContentControl.Content = null;
                documentView.IsEnabled = false;
            }
            else if (ContentControl.Content is UsersView users)
            {
                ContentControl.Content = null;
                users.IsEnabled = false;
            }
            else if (ContentControl.Content is IssueContentView tasks)
            {
                tasks.UnloadContent();
                ContentControl.Content = null;
                tasks.IsEnabled = false;
            }
            else if (ContentControl.Content is GeometryView geometry)
            {
                ContentControl.Content = null;
                geometry.IsEnabled = false;
            }
            else if (ContentControl.Content is DivisionsView models)
            {
                models.Dispose();
                ContentControl.Content = null;
                models.IsEnabled = false;
            }
            else if (ContentControl.Content is GetAttributeValues attributeValues)
            {
                attributeValues.Dispose();
                ContentControl.Content = null;
                attributeValues.IsEnabled = false;
            }
            else if (ContentControl.Content is BaseQuantitiesView qto)
            {
                ContentControl.Content = null;
                qto.IsEnabled = false;
            }

            EnabledContent = "";
        }

        public List<Guid> SelectedObjects => _selectedObjects;

    }
}
