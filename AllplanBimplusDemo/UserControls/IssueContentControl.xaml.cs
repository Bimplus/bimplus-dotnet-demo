using AllplanBimplusDemo.Classes;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using System.Windows.Controls;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for IssueContentControl.xaml
    /// </summary>
    public partial class IssueContentControl : UserControl
    {
        public IssueContentControl()
        {
            InitializeComponent();
        }

        #region private member      

        private IntegrationBase _integrationBase;

        private IssueListControl _listControl;
        private IssueDetailsControl _detailsControl;

        private TraceCodeTime _traceCodeTime;

        #endregion private member

        #region public methods

        /// <summary>
        /// Load controls.
        /// </summary>
        /// <param name="integrationBase"></param>
        /// <param name="parent"></param>
        public void LoadContent(IntegrationBase integrationBase)
        {
            _traceCodeTime = new TraceCodeTime("LoadContent", "IssueContentControl");
            _integrationBase = integrationBase;

            _integrationBase.EventHandlerCore.IssueViewSelected += EventHandlerCore_IssueViewSelected;
            _integrationBase.EventHandlerCore.ProjectChanged += EventHandlerCore_ProjectChanged;

            _listControl = new IssueListControl(integrationBase);
            IssueList.Content = _listControl;

            _detailsControl = new IssueDetailsControl(integrationBase);
            _detailsControl.LoadCompleted += _detailsControl_LoadCompleted;
            IssueDetails.Content = _detailsControl;
        }

        /// <summary>
        /// Clean up the control.
        /// </summary>
        public void UnloadContent()
        {
            if (_listControl != null)
                _listControl.Dispose();

            if (_detailsControl != null)
            {
                _detailsControl.LoadCompleted -= _detailsControl_LoadCompleted;
                _detailsControl.Dispose();
            }

            _integrationBase.EventHandlerCore.IssueViewSelected -= EventHandlerCore_IssueViewSelected;
            _integrationBase.EventHandlerCore.ProjectChanged -= EventHandlerCore_ProjectChanged;
        }

        #endregion public methods

        #region event handler

        private void _detailsControl_LoadCompleted(object sender, System.EventArgs e)
        {
            _traceCodeTime.Dispose();
            _traceCodeTime = null;
        }

        private void EventHandlerCore_IssueViewSelected(object sender, BimPlusEventArgs e)
        {
            _detailsControl.NavigateToIssue(e.Id);
        }

        private void EventHandlerCore_ProjectChanged(object sender, BimPlusEventArgs e)
        {
            _listControl.NavigateToControl(e.Id);

            _detailsControl.Dispose();
            _detailsControl = null;

            _detailsControl = new IssueDetailsControl(_integrationBase);
            IssueDetails.Content = _detailsControl;
        }

        #endregion event handler
    }
}
