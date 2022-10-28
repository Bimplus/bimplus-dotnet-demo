using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media.Imaging;
using BimPlus.Client.WebControls.WPF;
using BimPlusDemo.UserControls;
using Microsoft.Win32;

// ReSharper disable RedundantExtendsListEntry

namespace BimPlusDemo
{
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        /// <summary>
        /// ProjectSelection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProjectSelection_OnClick(object sender, RoutedEventArgs e)
        {
            var projectSelection = new ProjectSelection(_intBase, 800, 1200, "ProjectSelection", null);
            ProjectSelected = projectSelection.SelectBimPlusProject(Guid.Empty) != Guid.Empty;

            if (ProjectSelected == false || _intBase.CurrentProject == null) 
                return;
            ProjectName.Label = $"Project: {_intBase.CurrentProject.Name}";
            Team.Label = $"Team: {_intBase.CurrentProject.TeamName}";
            _webViewer.NavigateToControl(_intBase.CurrentProject.Id);

            DisposeContentControl();
            // read current project thumbnail
            try
            {
                var thumbnail = _intBase.ApiCore.Projects.GetThumbnail(ProjectId);
                if (thumbnail == null)
                    return;
                BitmapImage myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();
                myBitmapImage.StreamSource = new MemoryStream(thumbnail);
                myBitmapImage.DecodePixelWidth = 200;
                myBitmapImage.EndInit();
                ThumbnailIcon.LargeImageSource = myBitmapImage;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Select and Upload Project Thumbnail
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
                // upload to Bimplus
                _intBase.ApiCore.Projects.UploadThumbnail(ProjectId, dlg.FileName);
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
            var divisions = _intBase.ApiCore.Projects.GetDivisions(ProjectId);
            if (divisions == null)
            {
                MessageBox.Show("Project contains no models.");
                Models.IsEnabled = false;
                return;
            }

            var modelView = new DivisionsView(_intBase, divisions) { ParentWnd = this};
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
            var topology = _intBase.ApiCore.Projects.GetDocumentStructure(ProjectId);
            if (topology == null)
            {
                MessageBox.Show("No documents structure available");
                Documents.IsEnabled = false;
                return;
            }

            var documentView = new DocumentView(_intBase, topology);
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

            var members = _intBase.ApiCore.Projects.GetMembers(ProjectId);
            if (members == null || members.Count == 0)
            {
                MessageBox.Show("No ProjectMembers exist.");
                Users.IsEnabled = false;
                return;
            }

            var userView = new UsersView(_intBase, members);
            ContentControl.Content = userView;
            EnabledContent = "UsersView";
        }

        //private void AddDocuments_OnClick(object sender, RoutedEventArgs e)
        //{
        //    MessageBox.Show("Please select items in DocumentView \nfor uploading/downloading new/existing files");
        //}

        //private void AddMember_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var newMember = new InviteMember();
        //    if (newMember.ShowDialog() != true) 
        //        return;

        //    var serviceUrl = $"{_intBase.ServerName}/v2/users/exist?email={newMember.EMail.Text}";
        //    DtoUser member = null;
        //    try
        //    {
        //        member = GenericProxies.RestGet<DtoUser>(serviceUrl, _intBase.ClientConfiguration);
        //    }
        //    catch (WebException ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //        return;
        //    }

        //    try
        //    {

        //        if (member != null)
        //        {
        //            var userAdd = new DtoUserAddition
        //            {
        //                User = member,
        //                Role = new DtoRoleInfo(newMember.RoleId, newMember.Role.Text),
        //                UserGroups = new List<DtoGroupInfo>(1) { new DtoGroupInfo { Id = new Guid("b3908bd5-3173-4da9-8f56-3a950d4bba3f") } }
        //            };
        //            serviceUrl = $"{_intBase.ServerName}/v2/{_intBase.TeamSlug}/projects/{ProjectId}/members";
        //            GenericProxies.RestPost<DtoUserAddition, DtoUserAddition>(serviceUrl, userAdd, _intBase.ClientConfiguration);
        //        }
        //        else // if (result == HttpStatusCode.NotFound)
        //        {
        //            serviceUrl = $"{_intBase.ServerName}/v2/{_intBase.TeamSlug}/invitations";
        //            var invitation = GenericProxies.RestPost<DtoUserInvitation, DtoUserInvitation>(serviceUrl, new DtoUserInvitation
        //            {
        //                Email = newMember.EMail.Text,
        //                InvitationText = newMember.Message.Text,
        //                Projects = new List<DtoProjectRoleShort>(1)
        //                {
        //                    new DtoProjectRoleShort
        //                    {
        //                        ProjectId = ProjectId,
        //                        RoleName = newMember.Role.Text,
        //                        UserGroup = new DtoGroupInfo {Id = new Guid("b2efcb94-756e-4d51-bae4-16d662246b4a")}
        //                    }
        //                }
        //            });
        //        }
        //    }
        //    catch (WebException ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        /// <summary>
        /// Enable IssueContentView sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tasks_OnClick(object sender, RoutedEventArgs e)
        {
            DisposeContentControl();
            var tasks = new IssueContentView(_intBase, this);
            ContentControl.Content = tasks;
            EnabledContent = "Issues";
            
        }

        //private void CreateIssue_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var issue = _intBase.ApiCore.Issues.PostProjectShortIssue(ProjectId,
        //        new DtoShortIssue {IssueName = "new Issue"});
        //    if (issue == null)
        //        return;
        //    _intBase.ApiCore.Issues.AddSelectedObjects(issue.Id, _selectedObjects);

        //    // TODO: Helper.WriteToPng returns black image from ContentControl.
        //    //var stream = Helper.WriteToPng(ContentControl, "issueThumbnail.png");
        //    MemoryStream ms = new MemoryStream();
        //    Properties.Resources.newIssue.Save(ms, ImageFormat.Png);
        //    ms.Position = 0;
        //    using (BinaryReader br = new BinaryReader(ms))
        //    {
        //        byte[] b = br.ReadBytes((int) ms.Length);
        //        _intBase.ApiCore.Issues.PutIssueImage(issue.Id, b);
        //    }

        //    //Rectangle bounds = new Rectangle(){ Width = }
        //    //using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
        //    //{
        //    //    using (Graphics g = Graphics.FromImage(bitmap))
        //    //    {
        //    //        g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
        //    //    }
        //    //    bitmap.Save("C://test.jpg", ImageFormat.Jpeg);
        //    //}
        //    if (ContentControl.Content is IssueContentView task)
        //        task.EventHandlerCore_IssueViewSelected(task, new BimPlusEventArgs {Id = issue.Id});
        //}
    }
}