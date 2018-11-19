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
                    FileStream fileStream = null;
                    if (existFile)
                        fileStream = new FileStream(fileName, FileMode.Append);
                    else
                        fileStream = new FileStream(fileName, FileMode.Create);

                    if (fileStream != null)
                        _streamWriter = new StreamWriter(fileStream);
                }
                catch (Exception)
                {
                    MessageBoxHelper.ShowInformation("The log file could not be opened.", this);
                }

                if (_streamWriter != null)
                {
                    DateTime dateTime = DateTime.UtcNow;
                    string output = string.Format("Start: {0}", dateTime);
                    _streamWriter.WriteLine(output);
                }
            }

            Guid clientId = LoginSettings.GetClientId();

            _integrationBase = new IntegrationBase(clientId, TestApplicationId, _streamWriter)
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

        #region private and internal member

        private StreamWriter _streamWriter;
        //private readonly Guid TestApplicationId = new Guid("5F43560D-9B0C-4F3C-85CB-B5721D098F7B");
        private readonly Guid TestApplicationId = new Guid("c25706f5-e296-fa1b-9459-a9a25d1d01ac"); // Excel

        private ProjectSelection _projectSelection = null;
        private Window _projectSelectionWindow = null;

        private IntegrationBase _integrationBase;

        #endregion private and internal member

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
            get { return _contentControlHasContent; }

            set { _contentControlHasContent = value; NotifyPropertyChanged(); }
        }

        private ApplicationSettings _applicationSettings;

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
            CsgObjects.IsEnabled = enable;
            CalatravaObjects.IsEnabled = enable;
            ConnectionObjects.IsEnabled = enable;
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
            Control control = ContentControl.Content as Control;
            if (control != null)
            {
                SetUserControlSize(ContentControl, control);
            }
        }

        #endregion events

        #region private functions

        private void DisposeOldBimPlusUserControl()
        {
            BimPlusUserControl bimPlusControl = ContentControl.Content as BimPlusUserControl;

            if (bimPlusControl != null)
            {
                bimPlusControl.WebViewRecreated -= webViewer_WebViewRecreated;

                bimPlusControl.Dispose();
                bimPlusControl = null;
                ContentControl.Content = null;
            }
            else if (ContentControl.Content is IssueContentControl)
            {
                IssueContentControl issueContentControl = ContentControl.Content as IssueContentControl;
                if (issueContentControl != null)
                    issueContentControl.UnloadContent();

                issueContentControl = null;
                ContentControl.Content = null;
            }
            else if (ContentControl.Content is BIMExplorerAndTasks)
            {
                BIMExplorerAndTasks bimExplorerAndTasks = ContentControl.Content as BIMExplorerAndTasks;
                if (bimExplorerAndTasks != null)
                    bimExplorerAndTasks.UnloadContent();

                bimExplorerAndTasks = null;
                ContentControl.Content = null;
            }
            else if (ContentControl.Content is StructureControl)
            {
                StructureControl structureControl = ContentControl.Content as StructureControl;
                structureControl.SaveChangedStructure();
                structureControl.DisconnectSignalR();

                structureControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is GetAttributesControl)
            {
                GetAttributesControl getAttributesControl = ContentControl.Content as GetAttributesControl;
                getAttributesControl = null;

                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is SetAttributesControl)
            {
                SetAttributesControl setAttributesControl = ContentControl.Content as SetAttributesControl;
                setAttributesControl.SaveChangedAttributes();
                setAttributesControl.UnloadContent();
                setAttributesControl = null;

                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is CsgObjectsControl)
            {
                CsgObjectsControl csgObjectsControl = ContentControl.Content as CsgObjectsControl;
                csgObjectsControl.Visibility = Visibility.Collapsed;
                csgObjectsControl.UnloadContent();

                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is CalatravaControl)
            {
                CalatravaControl calatravaControl = ContentControl.Content as CalatravaControl;
                calatravaControl.Visibility = Visibility.Collapsed;
                calatravaControl.UnloadContent();

                calatravaControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is ConnectionsUserControl)
            {
                ConnectionsUserControl connectionsUserControl = ContentControl.Content as ConnectionsUserControl;
                connectionsUserControl.Visibility = Visibility.Collapsed;
                connectionsUserControl.UnloadContent();

                connectionsUserControl = null;
                ContentControl.Content = null;
                ContentControl.InvalidateVisual();
            }
            else if (ContentControl.Content is UserControl)
            {
                UserControl userControl = ContentControl.Content as UserControl;
                userControl = null;
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

            if (ContentControl.Content is UserControl)
                ContentControlHasContent = Visibility.Visible;
            else
                ContentControlHasContent = Visibility.Collapsed;
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
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string headerText = menuItem.Header as string;
                if (!string.IsNullOrEmpty(headerText))
                {
                    headerText = headerText.Replace("_", "");
                    ViewText.Content = headerText;
                }
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

            _projectSelection = new ProjectSelection(_integrationBase);

            _projectSelectionWindow = new Window() { Title = "Project selection", WindowStartupLocation = WindowStartupLocation.CenterScreen };
            _projectSelectionWindow.Icon = Icon;

            _projectSelectionWindow.Width = 1200;
            _projectSelectionWindow.Height = 800;

            _projectSelectionWindow.Content = _projectSelection;
            _projectSelectionWindow.Closed += ProjectSelectionWindow_Closed;

            _projectSelectionWindow.ShowDialog();
        }

        private void ProjectSelectionWindow_Closed(object sender, EventArgs e)
        {
            // Dispose project selection control.
            _projectSelection.Dispose();

            _projectSelectionWindow.Closed -= ProjectSelectionWindow_Closed;
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
            WebViewer webViewer = sender as WebViewer;

            if (webViewer != null)
            {
                if (_integrationBase.CurrentProject != null)
                    webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);
            }
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

        #endregion menu events

        #region event handler

        private void EventHandlerCore_ProjectChanged(object sender, BimPlusEventArgs e)
        {
            // Close the project selection window.
            _projectSelectionWindow?.Close();

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
                if (e.Id == null || e.Id == Guid.Empty)
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_streamWriter != null)
            {
                DateTime dateTime = DateTime.UtcNow;
                string output = string.Format("End: {0}", dateTime);
                _streamWriter.WriteLine(output);
            }

            _applicationSettings.SaveSettings();

            _streamWriter?.Flush();
            _streamWriter?.Close();
            _integrationBase.SignalRCore?.Disconnect(true);
            DisposeOldBimPlusUserControl();
        }

        #endregion window events
    }
}
