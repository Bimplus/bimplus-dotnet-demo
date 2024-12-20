using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BimPlus.Client;
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

        private IssueListControl? _listControl;
        private IssueDetailsControl? _detailsControl;

        private Guid ProjectId => MainWindow.IntBase.CurrentProject?.Id ?? Guid.Empty;

        #endregion private member

        public IssueContentView()
        {
            InitializeComponent();
            LoadContent();
        }


        #region public methods

        /// <summary>
        /// Load controls.
        /// </summary>
        public void LoadContent()
        {

            MainWindow.IntBase.EventHandlerCore.IssueViewSelected += EventHandlerCore_IssueViewSelected;
            MainWindow.IntBase.EventHandlerCore.ProjectChanged += EventHandlerCore_ProjectChanged;

            _listControl = new IssueListControl(MainWindow.IntBase);
            IssueList.Content = _listControl;

            _detailsControl = new IssueDetailsControl(MainWindow.IntBase);
            IssueDetails.Content = _detailsControl;
        }

        /// <summary>
        /// Clean up the control.
        /// </summary>
        public void UnloadContent()
        {
            _listControl?.Dispose();
            _detailsControl?.Dispose();

            MainWindow.IntBase.EventHandlerCore.IssueViewSelected -= EventHandlerCore_IssueViewSelected;
            MainWindow.IntBase.EventHandlerCore.ProjectChanged -= EventHandlerCore_ProjectChanged;
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
            _detailsControl?.NavigateToIssue(e.Id);
        }

        private void EventHandlerCore_ProjectChanged(object sender, BimPlusEventArgs e)
        {
            _listControl?.NavigateToControl(e.Id);

            _detailsControl?.Dispose();
            _detailsControl = null;

            _detailsControl = new IssueDetailsControl(MainWindow.IntBase);
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
            var issue = MainWindow.IntBase.ApiCore.Issues.PostProjectShortIssue(ProjectId,
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
                MainWindow.IntBase.ApiCore.Issues.PutIssueImage(issue.Id, b);
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
