using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media.Imaging;
using BimPlus.Client.WebControls.WPF;
using BimPlusDemo.UserControls;
using Microsoft.Win32;

namespace BimPlusDemo
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : RibbonWindow
    {
        /// <summary>
        /// Login/Logout button click event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Login_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not RibbonButton login)
                return;
            if (IsLoggedIn)
            {
                if (MessageBox.Show("Do you like to logout and login with a different account ? ", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                if (IntBase.Disconnect() == HttpStatusCode.OK)
                {
                    //DisposeContentControl();
                    IntBase.CurrentProject = null;
                    ProjectSelected = false;
                    _webViewer.Reload();
                }
            }

            if (await IntBase.ConnectAsync(this) == HttpStatusCode.OK)
            {
                login.Label = IntBase.UserName;
                login.SmallImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/Logout.png"));
            }
            else
            {
                login.Label = "Login";
                login.SmallImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/Login.png"));
            }
            OnPropertyChanged(nameof(IsLoggedIn));
        }

        /// <summary>
        /// Create a new Thumbnail Icon for the project.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThumbnailIcon_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "Picture files |*.jpg;*.png"
            };
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                ThumbnailIcon.LargeImageSource = new BitmapImage(new Uri(dlg.FileName));
                // upload to BimPlus
                IntBase.ApiCore.Projects.UploadThumbnail(ProjectId, dlg.FileName);
            }
        }

        private void ProjectSelection_OnClick(object sender, RoutedEventArgs e)
        {
            var projectSelection = new ProjectSelection(IntBase, 800, 1200, "ProjectSelection", null);
            ProjectSelected = projectSelection.SelectBimPlusProject(Guid.Empty) != Guid.Empty;

            if (ProjectSelected == false ||IntBase.CurrentProject == null)
                return;
            ProjectName.Label = $"Project: {IntBase.CurrentProject.Name}";
            Team.Label = $"Team: {IntBase.CurrentProject.TeamName}";
            _webViewer.NavigateToControl(IntBase.CurrentProject.Id);

            //DisposeContentControl();
            // read current project thumbnail
            try
            {
                var thumbnail = IntBase.ApiCore.Projects.GetThumbnail(ProjectId);
                if (thumbnail == null)
                    return;
                BitmapImage myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();
                myBitmapImage.StreamSource = new MemoryStream(thumbnail);
                myBitmapImage.DecodePixelWidth = 200;
                myBitmapImage.EndInit();
                Thumbnail = myBitmapImage;

                //var elements = IntBase.ApiCore.Projects.GetProjectElementTypes(IntBase.CurrentProject.Id);
                //var steelplates = IntBase.ApiCore.DtObjects.GetObjects<SteelPlate>(IntBase.CurrentProject.Id);
            }


            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Enable DivisionView sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Divisions_OnClick(object sender, RoutedEventArgs e)
        {
            if (ContentControl.Content is DivisionsView)
                return;

            DisposeContentControl();
            Models.IsEnabled = true;
            var divisions = IntBase.ApiCore.Projects.GetDivisions(ProjectId);
            if (divisions == null)
            {
                MessageBox.Show("Project contains no models.");
                Models.IsEnabled = false;
                return;
            }

            var modelView = new DivisionsView(IntBase, divisions, this);
            ContentControl.Content = modelView;
            EnabledContent = "Models";
        }

        /// <summary>
        /// Enable DocumentView sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowDocuments_OnClick(object sender, RoutedEventArgs e)
        {
            if (ContentControl.Content is DocumentView)
                return;

            DisposeContentControl();
            Documents.IsEnabled = true;
            // read DocumentFolder topology
            var topology = IntBase.ApiCore.Projects.GetDocumentStructure(ProjectId);
            if (topology == null)
            {
                MessageBox.Show("No documents structure available");
                Documents.IsEnabled = false;
                return;
            }

            var documentView = new DocumentView(topology);
            ContentControl.Content = documentView;
            EnabledContent = "Documents";
        }

        /// <summary>
        /// Enable UsersView sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Users_OnClick(object sender, RoutedEventArgs e)
        {
            if (ContentControl.Content is UsersView)
                return;
            DisposeContentControl();

            var members = IntBase.ApiCore.Projects.GetMembers(ProjectId);
            if (members == null || members.Count == 0)
            {
                MessageBox.Show("No ProjectMembers exist.");
                Users.IsEnabled = false;
                return;
            }

            var userView = new UsersView(IntBase, members);
            ContentControl.Content = userView;
            EnabledContent = "UsersView";
        }

        /// <summary>
        /// Enable IssueContentView sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tasks_OnClick(object sender, RoutedEventArgs e)
        {
            DisposeContentControl();
            var tasks = new IssueContentView();
            ContentControl.Content = tasks;
            EnabledContent = "Issues";

        }
    }
}
