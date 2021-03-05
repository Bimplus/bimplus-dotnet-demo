using AllplanBimplusDemo.Classes;
using AllplanBimplusDemo.Controls;
using AllplanBimplusDemo.UserControls;
using AllplanBimplusDemo.Windows;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Client.Integration.Login;
using BimPlus.Client.WebControls.WPF;
using BimPlus.Sdk.Data.TenantDto;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace AllplanBimplusDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region private and internal member

        private readonly StreamWriter _streamWriter;
        private readonly Guid _testApplicationId = new Guid("5F43560D-9B0C-4F3C-85CB-B5721D098F7B");

        private readonly IntegrationBase _integrationBase;

        #endregion private and internal member

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            _contentControlHasContent = Visibility.Collapsed;

            CultureInfoTextBox.ParentCultureInfo = CultureInfo.CurrentCulture;

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            // Logging
            string loggingPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nemetschek\bim+";

            if (!Directory.Exists(loggingPath))
            {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(loggingPath);
            }

            if (Directory.Exists(loggingPath))
            {
                string fileName = Path.Combine(loggingPath, "AllplanBimplusDemo.Log");
                bool existFile = File.Exists(fileName);

                try
                {
                    var fileStream = existFile 
                        ? new FileStream(fileName, FileMode.Append) 
                        : new FileStream(fileName, FileMode.Create);

                    _streamWriter = new StreamWriter(fileStream) {AutoFlush = true};
                }
                catch (Exception)
                {
                    MessageBoxHelper.ShowInformation("The log file could not be opened.", this);
                }

                if (_streamWriter != null)
                {
                    DateTime dateTime = DateTime.UtcNow;
                    string output = $"Start: {dateTime}";
                    _streamWriter.WriteLine(output);
                }
            }

            // because of UseSignalRCore it's necessary to add reference to Microsoft.AspNet.SignalR.Client.
            _integrationBase = new IntegrationBase(_testApplicationId, _streamWriter)
            {
                UseSignalRCore = true,
                SignalRAppCode = "AllplanBimplusDemo",
                SignalRLogFileName = "AllplanBimplusDemo"
            };

            // Connect with remember me.
            bool ok = _integrationBase.ConnectWithRememberMeAndClientId();

            if (ok)
            {
                AfterLogin();
            }

            // Set UILanguage.
            BimPlusUserControl.SetWebBrowserRuntimeUILanguage(new CultureInfo("en-GB"));
            //BimPlusUserControl.SetWebBrowserRuntimeUILanguage(new CultureInfo("de-DE"));

            // "de-DE" and "en-GB" are supported for the login.
            _integrationBase.TranslationCultureInfo = CultureInfo.CreateSpecificCulture("en-GB");
            //_integrationBase.TranslationCultureInfo = CultureInfo.CreateSpecificCulture("de-DE");

            BimPlusUserControl.EnableCache = true;

            _applicationSettings = new ApplicationSettings();
            _applicationSettings.SetLogger(_integrationBase.Logger);
            _applicationSettings.LoadSettings();
        }

        #region SystemEvents

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Locale)
            {
                // Refresh CurrentCulture
                CultureInfo.CurrentCulture.ClearCachedData();

                string cultureName = CultureInfo.CurrentCulture.DisplayName;

                if (cultureName != _cultureName)
                {
                    _cultureName = cultureName;

                    CultureInfoTextBox.ParentCultureInfo = CultureInfo.CurrentCulture;
                }
            }
        }

        private string _cultureName = "";

        #endregion SystemEvents

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //Debug.WriteLine(propertyName);
        }

        #endregion INotifyPropertyChanged

        #region properties

        private Visibility _contentControlHasContent;

        public Visibility ContentControlHasContent
        {
            get => _contentControlHasContent;

            set { _contentControlHasContent = value; NotifyPropertyChanged(); }
        }

        private readonly ApplicationSettings _applicationSettings;

        #endregion properties

        #region enable/disable

        private void EnableLoggedInControls(bool enable)
        {
            LoginMenuItem.IsEnabled = !enable;
            LogoutMenuItem.IsEnabled = enable;
            SelectProjectItem.IsEnabled = enable;
        }

        private void EnableProjectControls(bool enable)
        {
            BIMExplorerMenuItem.IsEnabled = enable;
            IssueContentControlMenuItem.IsEnabled = enable;
            BIMExplorerAndTasksMenuItem.IsEnabled = enable;

            UploadThumbnailMenuItem.IsEnabled = enable;
            UploadAttachmentMenuItem.IsEnabled = enable;
            UploadIfcFileMenuItem.IsEnabled = enable;

            UseCacheMenuItem.IsEnabled = enable;

            GetAttributesMenuItem.IsEnabled = enable;
            SetAttributesMenuItem.IsEnabled = enable;

            StructureMenuItem.IsEnabled = enable;
            GeometryData.IsEnabled = enable;
            CalatravaObjects.IsEnabled = enable;
            ConnectionObjects.IsEnabled = enable;
            CatalogData.IsEnabled = enable;
        }

        private void AfterLogin()
        {
            UserName.Content = _integrationBase.UserName;

            EnableLoggedInControls(true);

            _integrationBase.EventHandlerCore.ProjectChanged += EventHandlerCore_ProjectChanged;
            _integrationBase.EventHandlerCore.Unauthorized += EventHandlerCore_Unauthorized;
        }

        private void ShowDisabledControls()
        {
            _integrationBase.CurrentTeam = null;
            _integrationBase.CurrentProject = null;

            UserName.Content = null;
            ProjectName.Content = null;

            EnableLoggedInControls(false);
            EnableProjectControls(false);
        }

        #endregion enable/disable

        #region menu events

        #region general menu functions

        #region events

        private void ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ContentControl.Content is Control control)
            {
                SetUserControlSize(ContentControl, control);
            }
        }

        #endregion events

        #region private functions

        private void DisposeOldBimPlusUserControl()
        {
            if (ContentControl.Content is BimPlusUserControl bimPlusControl)
            {
                bimPlusControl.WebViewRecreated -= webViewer_WebViewRecreated;

                bimPlusControl.Dispose();
                //bimPlusControl = null;
                ContentControl.Content = null;
            }
            else if (ContentControl.Content is IssueContentControl)
            {
                if (ContentControl.Content is IssueContentControl issueContentControl)
                    issueContentControl.UnloadContent();

                //issueContentControl = null;
                ContentControl.Content = null;
            }
            else if (ContentControl.Content is BIMExplorerAndTasks)
            {
                if (ContentControl.Content is BIMExplorerAndTasks bimExplorerAndTasks)
                    bimExplorerAndTasks.UnloadContent();

                //bimExplorerAndTasks = null;
                ContentControl.Content = null;
            }
            else if (ContentControl.Content is StructureControl structureControl)
            {
                structureControl.SaveChangedStructure();
                structureControl.DisconnectSignalR();

                //structureControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is GetAttributesControl getAttributesControl)
            {
                //getAttributesControl = null;

                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is SetAttributesControl setAttributesControl)
            {
                setAttributesControl.SaveChangedAttributes();
                setAttributesControl.UnloadContent();
                //setAttributesControl = null;

                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is CsgObjectsControl csgObjectsControl)
            {
                csgObjectsControl.Visibility = Visibility.Collapsed;
                csgObjectsControl.UnloadContent();

                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is CalatravaControl calatravaControl)
            {
                calatravaControl.Visibility = Visibility.Collapsed;
                calatravaControl.UnloadContent();

                //calatravaControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is ConnectionsUserControl connectionsUserControl)
            {
                connectionsUserControl.Visibility = Visibility.Collapsed;
                connectionsUserControl.UnloadContent();

                //connectionsUserControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is UserControl userControl)
            {
                //userControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }

            if (ContentControl.Content == null)
            {
                ViewText.Content = "";
            }
            CheckViewExist();
        }

        private void CheckViewExist()
        {
            CloseView.IsEnabled = ContentControl.Content is UserControl;

            ContentControlHasContent = ContentControl.Content is UserControl 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private void SetUserControlSize(ContentControl contentControl, Control control)
        {
            double actualWidth = contentControl.ActualWidth;
            double actualHeight = contentControl.ActualHeight;

            control.Width = actualWidth;
            control.Height = actualHeight;
        }

        private void SetViewText(object sender)
        {
            if (!(sender is MenuItem menuItem)) 
                return;
            string headerText = menuItem.Header as string;
            if (!string.IsNullOrEmpty(headerText))
            {
                headerText = headerText.Replace("_", "");
                ViewText.Content = headerText;
            }
        }

        #endregion private functions

        #endregion general menu functions

        #region login/logout

        private void LoginMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Login with login dialog.
            bool connected = false;

            Guid accessToken = _integrationBase.ApiCore.AccessToken();

            if (accessToken != Guid.Empty)
                connected = _integrationBase.ConnectWithAccessToken(accessToken);

            if (connected || _integrationBase.ConnectWithLoginDialog())
            {
                if (_integrationBase.ApiCore.AccessToken() != Guid.Empty)
                {
                    AfterLogin();
                }

                ShowProjectSelection();
            }
        }

        private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Save changed values.
            DisposeOldBimPlusUserControl();

            // Logout.
            HttpStatusCode status = _integrationBase.Disconnect();
            if (status == HttpStatusCode.OK)
            {
                _integrationBase.EventHandlerCore.ProjectChanged -= EventHandlerCore_ProjectChanged;
                _integrationBase.EventHandlerCore.Unauthorized -= EventHandlerCore_Unauthorized;
                ShowDisabledControls();
                DisposeOldBimPlusUserControl();
                CheckViewExist();
            }
        }

        private void SelectProject_Click(object sender, RoutedEventArgs e)
        {
            ShowProjectSelection();
        }

        private void ShowProjectSelection()
        {
            DisposeOldBimPlusUserControl();

            var projectSelection = new ProjectSelection(_integrationBase);
            if (projectSelection.SelectBimPlusProject(Guid.Empty) == Guid.Empty)
                MessageBoxHelper.ShowInformation("No project selected!");
        }

       #endregion login/logout

        #region views

        private void BIMExplorer_Click(object sender, RoutedEventArgs e)
        {
            // Show Bim Explorer.
            DisposeOldBimPlusUserControl();

            if (!IsConnected())
                return;

            SetViewText(sender);

            WebViewer webViewer = new WebViewer(_integrationBase);

            if (_integrationBase.CurrentProject != null)
                webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);

            ContentControl.Content = webViewer;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);

            webViewer.WebViewRecreated += webViewer_WebViewRecreated;
            CheckViewExist();
        }

        private void webViewer_WebViewRecreated(object sender, WebViewRecreatedArgs args)
        {
            if (!(sender is WebViewer webViewer)) 
                return;
            if (_integrationBase.CurrentProject != null)
                webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);
        }

        private void IssueContentControl_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();

            if (!IsConnected())
                return;

            SetViewText(sender);

            IssueContentControl issueContentControl = new IssueContentControl();
            issueContentControl.LoadContent(_integrationBase);

            ContentControl.Content = issueContentControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        private void BIMExplorerAndTasks_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();

            if (!IsConnected())
                return;

            SetViewText(sender);

            BIMExplorerAndTasks userControl = new BIMExplorerAndTasks();
            userControl.LoadContent(_integrationBase);

            ContentControl.Content = userControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        private void CloseView_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();
        }

        private bool IsConnected()
        {
            if (_integrationBase == null || _integrationBase.CurrentProject == null || _integrationBase.CurrentProject.Id == Guid.Empty)
            {
                MessageBoxHelper.ShowInformation("No project selected.", this);
                return false;
            }
            else
                return true;
        }

        #endregion views

        #region uploads

        private void UploadThumbnail_Click(object sender, RoutedEventArgs e)
        {
            // Upload picture files.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".jpg",
                Filter = "Picture files |*.jpg;*.png"
            };

            bool? doIt = openFileDialog.ShowDialog();

            if (doIt == true)
            {
                long? size = _integrationBase.ApiCore.Projects.UploadThumbnail(_integrationBase.CurrentProject.Id, openFileDialog.FileName);
                if (size != null && (long)size > 0)
                {
                    MessageBoxHelper.ShowInformation(string.Format("{0} bytes were uploaded.", (long)size), this);
                }
            }
        }

        private void UploadAttachment_Click(object sender, RoutedEventArgs e)
        {
            // Upload attachment.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = "*.*",
                Filter = "All files |*.*"
            };

            bool? doIt = openFileDialog.ShowDialog();

            if (doIt == true)
            {
                try
                {
                    byte[] byteArray = File.ReadAllBytes(openFileDialog.FileName);


                    DtoAttachment dtoAttachment = _integrationBase.ApiCore.Attachments.UploadAttachment(_integrationBase.CurrentProject.Id, openFileDialog.FileName, byteArray);

                    if (dtoAttachment != null)
                        MessageBoxHelper.ShowInformation(string.Format("Attachment {0} has been uploaded.", dtoAttachment.FileName), this);
                }
                catch (Exception)
                {
                }
            }
        }

        private void UploadIfcFile_Click(object sender, RoutedEventArgs e)
        {
            // Upload ifc file.
            IfcUpload window = new IfcUpload();
            window.LoadContent(_integrationBase);

            window.ShowDialog();
        }

        #endregion uploads

        #region miscellaneous

        #region use cache

        private class FirstClass
        {
            public string ProjectName { get; set; }
            public SecondClass SecondClass { get; set; }
        }

        private class SecondClass
        {
            public string User { get; set; }
            public string Team { get; set; }
        }

        private void UseCache_Click(object sender, RoutedEventArgs e)
        {
            // Store classes in the cache
            if (_integrationBase != null && _integrationBase.CurrentTeam != null && _integrationBase.DtoUser != null)
            {
                Guid projectId = _integrationBase.CurrentProject.Id;
                string cacheName = "FirstClassCache";

                SecondClass secondClass = new SecondClass
                {
                    User = _integrationBase.DtoUser.FullName,
                    Team = _integrationBase.CurrentTeam.Name
                };

                FirstClass firstClass = new FirstClass
                {
                    ProjectName = _integrationBase.CurrentProject.Name,
                    SecondClass = secondClass
                };

                HttpStatusCode result = _integrationBase.ApiCore.Objects.PostCache(projectId, cacheName, firstClass);
                if (result == HttpStatusCode.OK || result == HttpStatusCode.Created)
                {
                    FirstClass cachedClass = _integrationBase.ApiCore.Objects.GetCache<FirstClass>(projectId, cacheName);
                    if (cachedClass != null)
                        MessageBoxHelper.ShowInformation("Class 'FirstClass' has been correctly restored from the cache.", this);
                    else
                        MessageBoxHelper.ShowInformation("Class 'FirstClass' could not correctly deserialized by Json.Deserializer.", this);
                }
            }
        }

        #endregion use cache

        #endregion miscellaneous

        #region attributes

        private void GetAttributes_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();

            SetViewText(sender);

            GetAttributesControl getAttributesControl = new GetAttributesControl();

            getAttributesControl.LoadContent(_integrationBase, this);

            ContentControl.Content = getAttributesControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        private void SetAttributes_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();

            SetViewText(sender);

            SetAttributesControl setAttributesControl = new SetAttributesControl();
            setAttributesControl.LoadContent(_integrationBase, this);

            ContentControl.Content = setAttributesControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        #endregion attributes

        #region structures

        private void Structure_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();

            SetViewText(sender);

            StructureControl structureControl = new StructureControl();
            structureControl.LoadContent(_integrationBase, this, _applicationSettings);

            ContentControl.Content = structureControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        #endregion structures

        #region CsgModel

        private void CsgObjects_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();

            SetViewText(sender);

            CsgObjectsControl csgObjectsControl = new CsgObjectsControl();
            csgObjectsControl.LoadContent(_integrationBase, this);

            ContentControl.Content = csgObjectsControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        #endregion CsgModel

        #region Calatrava

        private void CalatravaObjects_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();
            SetViewText(sender);

            CalatravaControl calatravaControl = new CalatravaControl();
            calatravaControl.LoadContent(_integrationBase, this);

            ContentControl.Content = calatravaControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        #endregion Calatrava

        #region Connections

        private void Connections_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();
            SetViewText(sender);

            ConnectionsUserControl connectionsUserControl = new ConnectionsUserControl();
            connectionsUserControl.LoadContent(_integrationBase, this);

            ContentControl.Content = connectionsUserControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        #endregion Connections

        #region Catalog data

        private void CatalogData_Click(object sender, RoutedEventArgs e)
        {
            DisposeOldBimPlusUserControl();
            SetViewText(sender);

            CatalogsControl catalogsControl = new CatalogsControl();
            catalogsControl.LoadContent(_integrationBase, this);


            ContentControl.Content = catalogsControl;

            SetUserControlSize(ContentControl, ContentControl.Content as Control);
            CheckViewExist();
        }

        #endregion Catalog data

        #endregion menu events

        #region event handler

        private void EventHandlerCore_ProjectChanged(object sender, BimPlusEventArgs e)
        {
            if (e.Id != Guid.Empty && _integrationBase.CurrentProject != null)
            {
                DtoProject project = _integrationBase.ApiCore.Projects.GetDtoProject(e.Id);
                EnableProjectControls(true);
                ProjectName.Content = _integrationBase.CurrentProject.Name;
            }
            else
            {
                EnableProjectControls(false);
                ProjectName.Content = "";
            }

            DisposeOldBimPlusUserControl();
        }

        private void EventHandlerCore_Unauthorized(object sender, BimPlusEventArgs e)
        {
            Action action = new Action(() =>
            {
                if (e.Id == Guid.Empty)
                    ProjectName.Content = "";

                _integrationBase.DtoUser = null;
                UserName.Content = null;

                MessageBox.Show("The access token is no longer valid.\r\n\r\nPlease log in again.");
                Debug.WriteLine("EventHandlerCore_Unauthorized");

                LogoutMenuItem_Click(this, new RoutedEventArgs());
                //EnableLoggedInControls(false);
                //EnableProjectControls(false);
            });

            if (ProjectCaption.CheckAccess())
                action();
            else
            {
                ProjectCaption.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, action);
            }
        }

        #endregion event handler

        #region window events

        #endregion window events

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_streamWriter != null)
            {
                DateTime dateTime = DateTime.UtcNow;
                string output = $"End: {dateTime}";
                _streamWriter.WriteLine(output);
            }

            _applicationSettings.SaveSettings();

            _integrationBase.SignalRCore?.Disconnect(true);
            DisposeOldBimPlusUserControl();
        }
    }
}
