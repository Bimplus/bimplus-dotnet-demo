using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.TenantDto;
using Microsoft.Win32;

// ReSharper disable InvalidXmlDocComment

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for DocumentView.xaml
    /// </summary>
    public partial class DocumentView
    {
        private DtoTopologyStructure DocumentStructure { get; set; }
        private IntegrationBase IntBase { get; set; }

        public DocumentView(IntegrationBase intBase, DtoTopologyStructure structure)
        {
            InitializeComponent();
            DataContext = this;
            DocumentStructure = structure;
            IntBase = intBase;
            InitializeTreeView();
            //var att = IntBase.ApiCore.Attachments.GetAttachments(IntBase.CurrentProject.Id);
        }

        /// <summary>
        /// InitializeTreeView
        /// </summary>
        private void InitializeTreeView()
        {
            AddChildItems(Documents.Items, DocumentStructure.Children);
        }

        /// <summary>
        /// AddChildItems
        /// </summary>
        /// <param name="items"></param>
        /// <param name="structureItem"></param>
        private void AddChildItems(ItemCollection items, IEnumerable<DtoTopologyStructure> structureItem)
        {
            foreach (var item in structureItem)
            {
                StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                Image img = (item.Type == "Attachment") 
                 ? new Image{ Source = new BitmapImage(new Uri("/BimPlusDemo;component/images/document.png",UriKind.RelativeOrAbsolute))}
                 : new Image{ Source = new BitmapImage(new Uri("/BimPlusDemo;component/images/folder.png", UriKind.RelativeOrAbsolute))};
                stackPanel.Children.Add(img);
                stackPanel.Children.Add(new TextBlock { Text = item.Name });
                var treeViewItem = new TreeViewItem
                {
                    Header = stackPanel,
                    Tag = item
                };
                if (item.Children.Any())
                    AddChildItems(treeViewItem.Items, item.Children);
                items.Add(treeViewItem);
            }
        }

        private void Documents_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            /// TODO: Cannot be used, because it's not possible to receive the selected TreeViewItem.
        }

        /// <summary>
        /// OnSelectedItemChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Documents_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as TreeView;
            if (!(treeView?.SelectedItem is TreeViewItem node)) 
                return;

            if (!(node.Tag is DtoTopologyStructure structure)) 
                return;

            //MessageBox.Show($"Id={structure.Id}");
            if (structure.Type == "Attachment")
            {
                if (MessageBox.Show("Do you like to download this file?", "", MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                var attachment =
                    IntBase.ApiCore.Attachments.DownloadAttachment(
                        structure.Id);
                Helper.Execute(attachment, structure.Name);
            }
            else if (structure.Children.FirstOrDefault(x => x.Type == "DocumentFolder") != null)
            {
                // TODO: Create new Node.
            }
            else
            {
                if (MessageBox.Show("Do you like to upload a new file to this folder?", "",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                OpenFileDialog dlg = new OpenFileDialog
                {
                    DefaultExt = "*.*",
                    //Filter = "Ifc files |*.ifc"
                };
                if (!dlg.ShowDialog().GetValueOrDefault())
                    return;
                var result = IntBase.ApiCore.Attachments.UploadAttachment(IntBase.CurrentProject.Id, dlg.FileName,
                    File.ReadAllBytes(dlg.FileName), null, structure.Name);
                if (result != null)
                {
                    Documents.Items.Clear();
                    DocumentStructure = IntBase.ApiCore.Structures.GetTopology(DocumentStructure.Id);
                    InitializeTreeView();
                }
            }
        }
    }
}
