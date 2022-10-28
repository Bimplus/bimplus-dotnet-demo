using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BimPlus.Client.Integration;
using BimPlus.LightCaseClient;
using BimPlus.Sdk.Data.Authentication;
using BimPlus.Sdk.Data.UserAdministration;
using BimPlusDemo.Annotations;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for UsersView.xaml
    /// </summary>
    public partial class UsersView : INotifyPropertyChanged
    {
        private IntegrationBase IntBase { get; set; }

        public UsersView(IntegrationBase intBase, List<DtoProjectMember> members)
        {
            InitializeComponent();
            DataContext = this;
            AddMembers(UsersCtrl.Items, members);
            IntBase = intBase;
        }

        private void AddMembers(ItemCollection items, List<DtoProjectMember> members)
        {
            foreach (var member in members)
            {
                items.Add(TreeViewMember(member));
            }
        }

        private TreeViewItem TreeViewMember(DtoProjectMember member)
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri("/BimPlusDemo;component/images/member.png",
                    UriKind.RelativeOrAbsolute))
            });
            stackPanel.Children.Add(new TextBlock { Text = member.User.DisplayName });
            var treeViewItem = new TreeViewItem
            {
                Header = stackPanel,
                Tag = member
            };
            treeViewItem.Items.Add(new TreeViewItem { Header = $"Email: {member.User.Email}" });
            treeViewItem.Items.Add(new TreeViewItem { Header = $"Company: {member.User.Company}" });
            treeViewItem.Items.Add(new TreeViewItem { Header = $"Role: {member.Role.Name}" });
            return treeViewItem;
        }

        private void UsersCtrl_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }

        /// <summary>
        /// RemoveMember
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveMember(object sender, RoutedEventArgs e)
        {
            var selected = UsersCtrl.SelectedItem as TreeViewItem;
            if (!(selected?.Tag is DtoProjectMember member)) 
                return;
            if (member.Role.Name == "ProjectAdmin" || member.Role.Name == "Account_Owner" || member.Role.Name == "Team_Admin")
            {
                MessageBox.Show($"User {member.User.DisplayName} is {member.Role.Name}", "Not allowed");
                return;
            }
            if (MessageBox.Show($"Do you like to remove User {member.User.Email} from this Project?", "Remove User",
                    MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            try
            {
                var serviceUrl = $"{IntBase.ServerName}/v2/{IntBase.TeamSlug}/projects/{IntBase.CurrentProject.Id}/members";
                GenericProxies.RestDelete<HttpStatusCode, DtoUserAddition>(serviceUrl, Convert(member),
                    IntBase.ClientConfiguration);
                UsersCtrl.Items.Remove(selected);
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// AddMember
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddMember(object sender, RoutedEventArgs e)
        {
            Guid projectId = IntBase.CurrentProject.Id;

            var newMember = new InviteMember();
            if (newMember.ShowDialog() != true)
                return;

            var serviceUrl = $"{IntBase.ServerName}/v2/users/exist?email={newMember.EMail.Text}";
            DtoUser user = null;
            try
            {
                user = GenericProxies.RestGet<DtoUser>(serviceUrl, IntBase.ClientConfiguration);
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
                //return;
            }

            try
            {
                if (user != null)
                {
                    var userAdd = new DtoUserAddition
                    {
                        User = user,
                        Role = new DtoRoleInfo(newMember.RoleId, newMember.Role.Text),
                        UserGroups = new List<DtoGroupInfo>(1) { new DtoGroupInfo { Id = new Guid("b3908bd5-3173-4da9-8f56-3a950d4bba3f") } }
                    };
                    serviceUrl = $"{IntBase.ServerName}/v2/{IntBase.TeamSlug}/projects/{projectId}/members";
                    var prjUser = GenericProxies.RestPost<DtoUserAddition, DtoUserAddition>(serviceUrl, userAdd, IntBase.ClientConfiguration);
                    if (prjUser != null)
                    {
                        UsersCtrl.Items.Add(TreeViewMember(Convert(prjUser)));
                        //OnPropertyChanged("UsersCtrl");
                    }
                }
                else // if (result == HttpStatusCode.NotFound)
                {
                    serviceUrl = $"{IntBase.ServerName}/v2/{IntBase.TeamSlug}/invitations";
                    var invitation = GenericProxies.RestPost<DtoUserInvitation, DtoUserInvitation>(serviceUrl, new DtoUserInvitation
                    {
                        Email = newMember.EMail.Text,
                        InvitationText = newMember.Message.Text,
                        Projects = new List<DtoProjectRoleShort>(1)
                        {
                            new DtoProjectRoleShort
                            {
                                ProjectId = projectId,
                                RoleName = newMember.Role.Text,
                                UserGroup = new DtoGroupInfo {Id = new Guid("b2efcb94-756e-4d51-bae4-16d662246b4a")}
                            }
                        }
                    });
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private DtoProjectMember Convert(DtoUserAddition member)
        {
            return new DtoProjectMember
            {
                User = member.User,
                Role = member.Role
            };
        }
        private DtoUserAddition Convert(DtoProjectMember member)
        {
            return new DtoUserAddition
            {
                User = member.User,
                Role = member.Role
            };
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
