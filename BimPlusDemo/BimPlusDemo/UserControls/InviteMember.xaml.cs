using System;
using System.ComponentModel;
using System.Windows;
using BimPlus.Sdk.Data;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for InviteMember.xaml
    /// </summary>
    public partial class InviteMember
    {
        public enum ProjectRoles
        {
            [Description("a618d075-7e4a-4bde-9d58-d2979696fa96")]
            ProjectViewer,
            [Description("f11d32e2-30b7-4f81-8a74-2165ecc00cf6")]
            ProjectEditor,
            [Description("a298b28d-9711-4a76-9a7d-910cbf144ee5")]
            ProjectAdmin
        }


        public Guid RoleId
        {
            get
            {
                if (Role.SelectionBoxItem is ProjectRoles r)
                    return r.ToGuid();
                return Guid.Empty;
            }
        }

        public InviteMember()
        {
            InitializeComponent();
            Role.ItemsSource = (ProjectRoles[]) Enum.GetValues(typeof(ProjectRoles));
            DataContext = this;
        }

        private void Invite_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
