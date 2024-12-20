using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Media.Imaging;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlusDemo.UserControls;
using Path = System.IO.Path;
// ReSharper disable RedundantExtendsListEntry

namespace BimPlusDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        #region BimPlus API Integration
        /// Application ID for the BimPlus API
        private static readonly Guid TestApplicationId = new Guid("7B8E7315-7F69-40DB-B340-03782B8BCB12");

        /// IntegrationBase object for the BimPlus API connection.
        public static readonly IntegrationBase IntBase = new IntegrationBase(TestApplicationId, GetStreamWriter(), clientName:"BimPlusDemo", clientVersion:"1.0")
        {
            UseSignalRCore = true,
            SignalRAppCode = $"BimPlusClient-{TestApplicationId}.SignalRCore",
            SignalRLogFileName = $"BimPlusDemo-{TestApplicationId}",
            SignalREnableUIMessages = true
        };

         /// WebViewer object for the BimPlus API connection.
        private readonly WebViewer _webViewer;

        /// List of selected objects in the WebViewer.
        //private static readonly List<Guid> _selectedObjects = new List<Guid>();
        public static List<Guid> SelectedObjects = new List<Guid>();

        private bool _projectSelected;

        /// enable/disable the ribbon buttons
        public bool ProjectSelected
        {
            get => _projectSelected;
            set => SetField(ref _projectSelected, value);
        }

        private BitmapImage _projectImage = new BitmapImage(new Uri("pack://application:,,,/Images/defaultImage.png"));
        public BitmapImage Thumbnail
        {
            get => _projectImage;
            set => SetField(ref _projectImage, value);
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(IntBase.ClientConnection.AuthorizationAccessToken);
        public Guid ProjectId => IntBase.CurrentProject?.Id ?? Guid.Empty;

        /// <summary>
        /// update webViewer control.
        /// </summary>
        private void NavigateToControl()
        {
            //ButtonsEnabled = false;
            _webViewer.NavigateToControl(IntBase.CurrentProject.Id);
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            if (IntBase.ConnectWithRememberMe())
            {
                Login.SmallImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/Logout.png"));
                Login.Label = IntBase.UserName;
            }

            _webViewer = new WebViewer(IntBase);
            _webViewer.WebViewRecreated += (sender, _) =>
            {
                if (sender is not WebViewer webViewer) return;
                if (IntBase.CurrentProject != null)
                    webViewer.NavigateToControl(IntBase.CurrentProject.Id);
            };

            IntBase.EventHandlerCore.DataLoaded += (_, _) =>
            {
                //ProgressWindow.Hide();
                //ButtonsEnabled = true;
            };

            // Event handler for the export started event.
            IntBase.EventHandlerCore.ExportStarted += (_, _) =>
            {
                NavigateToControl();
            };

            // ModelModified event needs an update of WebViewer control.
            IntBase.EventHandlerCore.ModelModified += (_, args) =>
            {
                if (args?.Value == "ModelModified")
                    NavigateToControl();
                else if (args?.Value == "SelectObject")
                {
                    _webViewer.HighlightObjectByID(args.Id, throwOnError: false, isolate: true, blockProperties: false);
                    if (args.Selected.GetValueOrDefault(false))
                        _webViewer.ObjectZoomToFit(args.Id);
                }
            };

            // receive selected object from WebViewer.
            IntBase.EventHandlerCore.ObjectSelected += (sender, e) =>
            {
                if (sender is not WebViewer || e == null || e.Id == Guid.Empty)
                    return;
                if (SelectedObjects.Contains(e.Id))
                    return;

                if (e.Multiselect == false)
                    SelectedObjects.Clear();

                SelectedObjects.Add(e.Id);
            };

            // receive selected objects from WebViewer.
            IntBase.EventHandlerCore.ObjectsSelected += (_, e) =>
            {
                if (e?.Ids == null)
                    return;
                SelectedObjects.Clear();
                SelectedObjects.AddRange(e.Ids);
            };

            Viewer.Content = _webViewer;
        }

        private string _enabledContent = string.Empty;
        // ReSharper disable ExplicitCallerInfoArgument
        public string EnabledContent
        {
            get => _enabledContent;
            set
            {
                _enabledContent = value;
                SetField(ref _enabledContent, value);
            }
        }

        private void DisposeContentControl()
        {
            if (ContentControl.Content is UserControl userControl)
            {
                userControl.IsEnabled = false;
            }

            if (ContentControl.Content is IssueContentView issueContentView)
            {
                issueContentView.UnloadContent();
            }
            ContentControl.Content = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #region logging
        static StreamWriter? GetStreamWriter()
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

    }
}