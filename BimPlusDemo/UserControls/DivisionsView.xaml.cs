using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.Notification;
using BimPlus.Sdk.Data.TenantDto;
using BimPlusDemo.Commands;
using Microsoft.Win32;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class DivisionsView : IDisposable
    {
        public ProgressWindow? Progress { get; set; } = null;
        public ICommand? UploadCommand { get; set; } = null;

        private IntegrationBase IntBase { get; set; }
        private List<DtoDivision> Models { get; set; }
        public MainWindow ParentWnd { get; set; }

        public DivisionsView(IntegrationBase intBase, List<DtoDivision> models, MainWindow parentWnd)
        {
            InitializeComponent();
            IntBase = intBase;
            Models = models;
            ParentWnd = parentWnd;
            DataContext = this;
            InitializeTreeView();
            IntBase.EventHandlerCore.SignalRImportProgress += EventHandlerCoreOnSignalRImportProgress;
        }


        private Guid ProjectId => IntBase.CurrentProject?.Id ?? Guid.Empty;

        private void InitializeTreeView()
        {
            foreach (var dtoDivision in Models)
            {
                DivisionsCtrl.Items.Add(TreeViewModel(dtoDivision));
            }
        }

        private TreeViewItem TreeViewModel(DtoDivision model)
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri("/BimPlusDemo;component/images/quader_16.png",
                    UriKind.RelativeOrAbsolute))
            });
            stackPanel.Children.Add(new TextBlock { Text = model.Name });
            var treeViewItem = new TreeViewItem
            {
                Header = stackPanel,
                Tag = model
            };
            treeViewItem.Items.Add(new TreeViewItem { Header = $"Type: {model.InputType}" });
            treeViewItem.Items.Add(new TreeViewItem { Header = $"Created by {model.CreatedByUser.Email}" });
            treeViewItem.Items.Add(new TreeViewItem { Header = $"last modified at {model.Changed}" });
            treeViewItem.Items.Add(new TreeViewItem { Header = $"Revisions: {model.Revisions?.Count() ?? 0}" });
            if (!string.IsNullOrEmpty(model.ImportFileName) && !string.IsNullOrEmpty(model.Url))
            {
                var button = new Button { Content = model.ImportFileName, Tag = model.Id, ToolTip = "Download model." };
                button.Click += (sender, args) =>
                {
                    var btn = sender as Button;
                    if (!(btn?.Tag is Guid modelId)) return;
                    var data = IntBase.ApiCore.Divisions.DownloadModelResource(modelId);
                    if (data != null)
                        Helper.Execute(data, model.ImportFileName);
                };
                treeViewItem.Items.Add(new TreeViewItem { Header = button });
            }
            return treeViewItem;

        }

        private void AddModel(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "Ifc files |*.ifc"
            };
            if (!dlg.ShowDialog().GetValueOrDefault())
                return;

            var file = new FileInfo(dlg.FileName);
            var divisionName = file.Name.Replace(file.Extension, "");

            var division = IntBase.ApiCore.Projects.GetDivisions(ProjectId).Find(x => x.Name == divisionName);
            if (division != null)
            {
                // update existing model
                var updateDlg = new UpdateModel(divisionName, file.Name);
                if (updateDlg.ShowDialog() == false)
                    return;

                if (updateDlg.CreateRevision)
                    IntBase.ApiCore.Divisions.CreateRevision(division.Id,
                        $"Revision_{division.Revisions?.Count() + 1}", "");
                if (updateDlg.ReplaceModel && division.TopologyDivisionId.HasValue)
                    IntBase.ApiCore.DtObjects.DeleteObject(division.TopologyDivisionId.Value);
            }
            else
            {
                // create new model/division
                division = IntBase.ApiCore.Divisions.CreateModel(ProjectId, new DtoDivision { Name = divisionName });
                if (division == null || division.Id == Guid.Empty)
                    return;
                DivisionsCtrl.Items.Add(TreeViewModel(division));
                // upload new model
            }

            if (Progress == null)
            {
                Progress = new ProgressWindow(ParentWnd);
                Progress.Closed += (o, args) =>
                {
                    Progress = null;
                    UploadCommand = null;
                    IntBase.EventHandlerCore.OnExportStarted(this,
                        new BimPlusEventArgs { Value = "ModelChanged" });
                };
            }

            if (UploadCommand == null)
                UploadCommand = new IfcUploadCommand(Progress);
            UploadCommand?.Execute(null);

            // Upload
            Task.Factory.StartNew(() =>
            {
                IntBase.ApiCore.Divisions.UploadIfcModel(ProjectId, division.Id, divisionName, dlg.FileName);
                UploadCommand?.Execute("Ifc-Export finished");
            });
        }

        private void RemoveModel(object sender, RoutedEventArgs e)
        {
            var selected = DivisionsCtrl.SelectedItem as TreeViewItem;
            if (!(selected?.Tag is DtoDivision model))
                return;
            if (MessageBox.Show($"Do you really like to delete the model {model.Name}?", "Delete Model",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (IntBase.ApiCore.Divisions.DeleteDtoDivision(model.Id))
                {
                    IntBase.EventHandlerCore.OnModelModified(this, new BimPlusEventArgs {Value = "ModelModified"});
                    DivisionsCtrl.Items.Remove(selected);
                }
            }
        }

        /// <summary>
        /// SignalR MessageHandler for showing ImportProgress. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        void EventHandlerCoreOnSignalRImportProgress(object sender, ImportProgressReport info)
        {
            if (Progress == null)
                return;

            if (info.State == "PROCESSING")
            {
                UploadCommand?.Execute(info.Percentage);
            }
            else
            {
                UploadCommand?.Execute($"ImportProcess '{info.State}'");
                IntBase.EventHandlerCore.OnExportStarted(this,
                    new BimPlusEventArgs { Value = "ModelChanged" });
            }
        }

        public void Dispose()
        {
            IntBase.EventHandlerCore.SignalRImportProgress -= EventHandlerCoreOnSignalRImportProgress;
        }
    }
}
