using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.Utilities.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for BIMExplorerAndTasks.xaml
    /// </summary>
    public partial class BIMExplorerAndTasks : NotifyPropertyChangedUserControl

    {
        public BIMExplorerAndTasks()
        {
            InitializeComponent();

            DataContext = this;

            SelectedObjects = new List<DtObject>();
        }

        #region private member      

        private IntegrationBase _integrationBase;

        private WebViewer _webViewer;
        private IssueListControl _listControl;
        private IssueDetailsControl _detailsControl;

        #endregion private member

        #region properties

        private List<DtObject> _selectedObjects = null;

        /// <summary>
        /// List of selected DtObjects.  
        /// </summary>
        public List<DtObject> SelectedObjects
        {
            get { return _selectedObjects; }

            set
            {
                _selectedObjects = value;
                HasSelectedObjects = (_selectedObjects != null && _selectedObjects.Count > 0);
                Debug.WriteLine(string.Format("{0} : {1}", "SelectedObjects", value.Count));
                NotifyPropertyChanged();
            }
        }

        private bool _hasSelectedObjects = false;

        /// <summary>
        /// Has selected objects from UI selection.
        /// </summary>
        public bool HasSelectedObjects
        {
            get { return _hasSelectedObjects; }

            set { _hasSelectedObjects = value; NotifyPropertyChanged(); }
        }

        private bool _createIssueEnabled = false;

        /// <summary>
        /// Can an issue be created.
        /// </summary>
        public bool CreateIssueEnabled
        {
            get { return _createIssueEnabled; }

            // The binding does not work.
            set { _createIssueEnabled = value; CreateTask.IsEnabled = value; NotifyPropertyChanged(); }
        }

        internal DtoIssue CurrentIssue { get; set; }

        #endregion properties

        #region public methodes

        /// <summary>
        /// Load start properties and controls.
        /// </summary>
        /// <param name="integrationBase"></param>
        /// <param name="parent"></param>
        public void LoadContent(IntegrationBase integrationBase)
        {
            _integrationBase = integrationBase;

            _integrationBase.EventHandlerCore.IssueViewSelected += EventHandlerCore_IssueViewSelected;
            _integrationBase.EventHandlerCore.IssueSelected += EventHandlerCore_IssueSelected;
            _integrationBase.EventHandlerCore.ObjectSelected += EventHandlerCore_ObjectSelected;
            _integrationBase.EventHandlerCore.ProjectChanged += EventHandlerCore_ProjectChanged;
            _integrationBase.EventHandlerCore.DataLoaded += EventHandlerCore_DataLoaded;

            _webViewer = new WebViewer(integrationBase);

            _webViewer.NavigateToControl(integrationBase.CurrentProject.Id);
            _webViewer.LoadCompleted += webViewer_LoadCompleted;

            BimExplorer.Content = _webViewer;

            _listControl = new IssueListControl(integrationBase);
            _listControl.LoadCompleted += listControl_LoadCompleted;
            IssueList.Content = _listControl;

            _detailsControl = new IssueDetailsControl(integrationBase);
            IssueDetails.Content = _detailsControl;
        }

        /// <summary>
        /// Clean up the control.
        /// </summary>
        public void UnloadContent()
        {
            _webViewer.LoadCompleted -= webViewer_LoadCompleted;
            _listControl.LoadCompleted -= listControl_LoadCompleted;

            if (_webViewer != null)
                _webViewer.Dispose();

            if (_listControl != null)
                _listControl.Dispose();

            if (_detailsControl != null)
                _detailsControl.Dispose();

            _integrationBase.EventHandlerCore.IssueViewSelected -= EventHandlerCore_IssueViewSelected;
            _integrationBase.EventHandlerCore.IssueSelected -= EventHandlerCore_IssueSelected;
            _integrationBase.EventHandlerCore.ObjectSelected -= EventHandlerCore_ObjectSelected;
            _integrationBase.EventHandlerCore.ProjectChanged -= EventHandlerCore_ProjectChanged;
            _integrationBase.EventHandlerCore.DataLoaded -= EventHandlerCore_DataLoaded;

            CreateIssueEnabled = false;
            HasSelectedObjects = false;
        }

        #endregion public methodes

        #region events

        private void EventHandlerCore_IssueViewSelected(object sender, BimPlusEventArgs e)
        {
            _detailsControl.NavigateToIssue(e.Id);

            OnIssueSelected(e);
        }

        private void EventHandlerCore_IssueSelected(object sender, BimPlusEventArgs e)
        {
            OnIssueSelected(e);
        }

        private void EventHandlerCore_ObjectSelected(object sender, BimPlusEventArgs e)
        {
            Debug.WriteLine(string.Format("Id:{0} Selected:{1} Multiselect:{2}", e.Id, e.Selected, e.Multiselect));

            if (e.Id != Guid.Empty || e.Selected != true)
            {
                DtObject dtObject = _integrationBase.ApiCore.DtObjects.GetObject(e.Id, ObjectRequestProperties.AttributDefinition | ObjectRequestProperties.Pset);

                if (dtObject != null)
                {
                    if (e.Multiselect == true)
                    {
                        if (SelectedObjects.FirstOrDefault(o => o.Id == e.Id) == null)
                            SelectedObjects.Add(dtObject);
                    }
                    else
                    {
                        SelectedObjects.Clear();
                        if (e.Selected == true)
                            SelectedObjects.Add(dtObject);
                    }
                }

                if (e.Selected == false)
                {
                    DtObject exist = SelectedObjects.FirstOrDefault(o => o.Id == e.Id);
                    if (exist != null)
                        SelectedObjects.Remove(exist);
                    else
                        SelectedObjects.Clear();
                }
                else if (e.Selected == null && e.Id != Guid.Empty)
                {
                    if (dtObject != null)
                    {
                        if (SelectedObjects.FirstOrDefault(o => o.Id == e.Id) == null)
                            SelectedObjects.Add(dtObject);
                    }
                    else
                        SelectedObjects.Clear();
                }

                List<DtObject> newList = new List<DtObject>();
                newList.AddRange(SelectedObjects);
                SelectedObjects = newList;
                Trace.WriteLine(string.Format("_selectedObjects.Count:{0}", SelectedObjects.Count()));
            }
        }

        private void OnIssueSelected(BimPlusEventArgs e)
        {
            DtoIssue task = _integrationBase.ApiCore.Issues.GetDtoIssue(e.Id);
            CurrentIssue = task;

            if (_listControl != null)
                _listControl.SetCurrentIssue(e.Id.ToString());

            if (task.Id != Guid.Empty)
            {
                List<Guid> guids = new List<Guid>();

                string scene = task.Scene;

                if (scene != null && scene != "null")
                {
                    DtoScene dtoScene = JsonConvert.DeserializeObject(scene, typeof(DtoScene)) as DtoScene;
                    if (dtoScene != null && dtoScene.Objects != null)
                    {
                        if (dtoScene.Objects.Selected != null)
                            guids.AddRange(dtoScene.Objects.Selected);
                        if (dtoScene.Objects.HighlightedSelected != null)
                            guids.AddRange(dtoScene.Objects.HighlightedSelected);

                        guids = guids.ToLookup(g => g).Select(l => l.Key).ToList();

                        if (guids != null && guids.Count > 0)
                        {
                            _webViewer.HighlightObjectsByID(guids);

                            BimPlusEventArgs args = new BimPlusEventArgs { Id = guids[0] };
                            _integrationBase.EventHandlerCore.OnObjectSelected(args);
                        }
                    }
                }
            }
        }

        private void EventHandlerCore_ProjectChanged(object sender, BimPlusEventArgs e)
        {
            UnloadContent();

            LoadContent(_integrationBase);
        }

        private void AddObjectsButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentIssue != null && CurrentIssue.Id != Guid.Empty && SelectedObjects.Count > 0)
            {
                List<Guid> objectGuids = SelectedObjects.Select(o => o.Id).ToList();

                HttpStatusCode status = _integrationBase.ApiCore.Issues.AddSelectedObjects(CurrentIssue.Id, objectGuids);
            }
        }

        private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Test.
            //List<DtoIssue> issues = _integrationBase.ApiCore.Issues.GetProjectDtoIssues(_integrationBase.CurrentProject.Id).OrderBy(i => i.ShortId).ToList();
            //DtoIssue lastIssue = issues.LastOrDefault();
            //if (lastIssue != null)
            //    _integrationBase.ApiCore.Issues.DeleteIssue(lastIssue.Id);

            DtoIssue newIssue = new DtoIssue { Name = "New task" };

            newIssue = _integrationBase.ApiCore.Projects.PostDtoIssue(_integrationBase.CurrentProject.Id, newIssue);
            Debug.WriteLine(string.Format("ShortId{0}", newIssue.ShortId));

            if (newIssue != null && newIssue.Id != Guid.Empty)
            {
                BimPlusEventArgs args = new BimPlusEventArgs { Id = newIssue.Id };
                _integrationBase.EventHandlerCore.OnCreateIssue(args);
                _integrationBase.EventHandlerCore.OnIssueSelected(args);

                _detailsControl.NavigateToIssue(newIssue.Id);
            }
        }

        #region content loaded

        bool _listControlLoaded = false;

        private void listControl_LoadCompleted(object sender, EventArgs e)
        {
            _listControlLoaded = true;
            CheckContentLoaded();
        }

        bool _detailsControlLoaded = false;

        private void EventHandlerCore_DataLoaded(object sender, BimPlusEventArgs e)
        {
            _detailsControlLoaded = true;
            CheckContentLoaded();
        }

        bool _webViewerLoaded = false;

        private void webViewer_LoadCompleted(object sender, EventArgs e)
        {
            _webViewerLoaded = true;
            CheckContentLoaded();
        }

        private void CheckContentLoaded()
        {
            if (_listControlLoaded && _detailsControlLoaded && _webViewerLoaded)
                CreateIssueEnabled = true;
        }

        #endregion content loaded

        private void IssueDetails_Unloaded(object sender, RoutedEventArgs e)
        {
            UnloadContent();
        }

        #endregion events
    }
}
