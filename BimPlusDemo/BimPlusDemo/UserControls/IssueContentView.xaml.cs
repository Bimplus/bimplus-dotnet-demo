using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlus.Sdk.Data.TenantDto;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for IssueContentView.xaml
    /// </summary>
    public partial class IssueContentView : UserControl
    {
        #region private member      

        private IntegrationBase _integrationBase;
        private MainWindow _main;

        private IssueListControl _listControl;
        private IssueDetailsControl _detailsControl;

        private Guid ProjectId => _integrationBase?.CurrentProject?.Id ?? Guid.Empty;
        //private TraceCodeTime _traceCodeTime;

        #endregion private member

        public IssueContentView(IntegrationBase integrationBase, MainWindow main)
        {
            InitializeComponent();
            LoadContent(integrationBase);
            _main = main;
        }


        #region public methods

        /// <summary>
        /// Load controls.
        /// </summary>
        /// <param name="integrationBase"></param>
        public void LoadContent(IntegrationBase integrationBase)
        {
            _integrationBase = integrationBase;

            _integrationBase.EventHandlerCore.IssueViewSelected += EventHandlerCore_IssueViewSelected;
            _integrationBase.EventHandlerCore.ProjectChanged += EventHandlerCore_ProjectChanged;

            _listControl = new IssueListControl(integrationBase);
            IssueList.Content = _listControl;

            _detailsControl = new IssueDetailsControl(integrationBase);
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
                //_detailsControl.LoadCompleted -= _detailsControl_LoadCompleted;
                _detailsControl.Dispose();
            }

            _integrationBase.EventHandlerCore.IssueViewSelected -= EventHandlerCore_IssueViewSelected;
            _integrationBase.EventHandlerCore.ProjectChanged -= EventHandlerCore_ProjectChanged;
        }

        #endregion public methods

        #region event handler

        public void EventHandlerCore_IssueViewSelected(object sender, BimPlusEventArgs e)
        {
            TaskLabel.Content = "IssueDetails";
            IssueList.Visibility = Visibility.Hidden;
            CreateIssue.Visibility = Visibility.Hidden;
            IssueDetails.Visibility = Visibility.Visible;
            BackToList.Visibility = Visibility.Visible;
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

        private void BackToList_OnClick(object sender, RoutedEventArgs e)
        {
            TaskLabel.Content = "Issues";
            IssueDetails.Visibility = Visibility.Hidden;
            IssueList.Visibility = Visibility.Visible;
            BackToList.Visibility = Visibility.Hidden;
            CreateIssue.Visibility = Visibility.Visible;
        }

        private void CreateIssue_OnClick(object sender, RoutedEventArgs e)
        {
            var issue = _integrationBase.ApiCore.Issues.PostProjectShortIssue(ProjectId,
                new DtoShortIssue { IssueName = "new Issue" });
            if (issue == null)
                return;
            //_integrationBase.ApiCore.Issues.AddSelectedObjects(issue.Id, _main.SelectedObjects);

            // TODO: Helper.WriteToPng returns black image from ContentControl.
            //var stream = Helper.WriteToPng(ContentControl, "issueThumbnail.png");
            MemoryStream ms = new MemoryStream();
            Properties.Resources.newIssue.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            using (BinaryReader br = new BinaryReader(ms))
            {
                byte[] b = br.ReadBytes((int)ms.Length);
                _integrationBase.ApiCore.Issues.PutIssueImage(issue.Id, b);
            }

            //Rectangle bounds = new Rectangle(){ Width = }
            //using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            //{
            //    using (Graphics g = Graphics.FromImage(bitmap))
            //    {
            //        g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            //    }
            //    bitmap.Save("C://test.jpg", ImageFormat.Jpeg);
            //}
            //if (Content is IssueContentView task)
                EventHandlerCore_IssueViewSelected(this, new BimPlusEventArgs { Id = issue.Id });
        }
    }
}
