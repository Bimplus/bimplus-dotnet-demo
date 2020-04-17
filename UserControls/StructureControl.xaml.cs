using AllplanBimplusDemo.Classes;
using AllplanBimplusDemo.Windows;
using AllplanBimplusDemo.WinForms;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.Data.Notification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using BimPlus.Sdk.Data.DbCore.Structure;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for StructureControl.xaml
    /// </summary>
    public partial class StructureControl : NotifyPropertyChangedUserControl
    {
        public StructureControl()
        {
            InitializeComponent();
            DataContext = this;

            _orgStructureNamesDictionary = new Dictionary<DtoTopologyStructure, string>();
        }

        #region private member

        private IntegrationBase _integrationBase;
        private Window _parentWindow;
        private ApplicationSettings _applicationSettings;

        // Structure names.
        private Dictionary<DtoTopologyStructure, string> _orgStructureNamesDictionary;

        // Original topology structure.
        private DtoTopologyStructure _orginalTopology;

        private TreeViewItem _rootTreeViewItem;

        #endregion private member

        #region public methods

        /// <summary>
        /// Load structure content.
        /// </summary>
        /// <param name="integrationBase"></param>
        /// <param name="parent"></param>
        public void LoadContent(IntegrationBase integrationBase, Window parent, ApplicationSettings applicationSettings)
        {
            _integrationBase = integrationBase;
            _parentWindow = parent;
            _applicationSettings = applicationSettings;

            if (_integrationBase.CurrentProject != null)
            {
                DtoProject project = _integrationBase.ApiCore.Projects.GetProject(_integrationBase.CurrentProject.Id);
                ProjectModels = ExtensionsClass.GetDivisonNames(project, _integrationBase);

                using (new TraceCodeTime("LoadContent", "Load structures"))
                {
                    List<DtoStructure> structureList = _integrationBase.ApiCore.Structures.GetStructuresByProjectId(_integrationBase.CurrentProject.Id).OrderBy(s => s.Name).ToList();
                    Structures = structureList;
                }

                if (Structures != null)
                {
                    HasObjects = Structures.Count > 0;
                    if (HasObjects)
                        SelectedItem = Structures[0];
                }

                _integrationBase.EventHandlerCore.SignalRProjectItemChanged += EventHandlerCore_SignalRProjectItemChanged;

                ListenToBimplus.IsChecked = _applicationSettings?.Structures_ListenToBimplus;
            }
        }

        #endregion public methods

        #region properties

        private string _models;

        public string ProjectModels
        {
            get { return _models; }

            set { _models = value; NotifyPropertyChanged(); }
        }

        private List<DtoStructure> _structures;

        public List<DtoStructure> Structures
        {
            get { return _structures; }

            set { _structures = value; NotifyPropertyChanged(); }
        }

        private DtoStructure _selectedItem;

        public DtoStructure SelectedItem
        {
            get { return _selectedItem; }

            set
            {
                if (value != _selectedItem)
                {
                    SaveChangedStructure();
                    ClearTreeView();

                    _selectedItem = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _hasObjects = false;

        public bool HasObjects
        {
            get { return _hasObjects; }

            set { _hasObjects = value; NotifyPropertyChanged(); }
        }

        private bool _canModifyTreeViewItem;

        public bool CanModifyTreeViewItem
        {
            get { return _canModifyTreeViewItem; }

            set { _canModifyTreeViewItem = value; NotifyPropertyChanged(); }
        }

        // Working topology structure.
        private DtoTopologyStructure _selectedTopology;

        public DtoTopologyStructure SelectedTopology
        {
            get { return _selectedTopology; }
            set { _selectedTopology = value; }
        }

        #endregion properties

        #region events

        #region SignalR events

        private void EventHandlerCore_SignalRProjectItemChanged(object sender, ProjectHubItemChangedInfo info)
        {
            if (ListenToBimplusIsChecked() == true && info.ProjectId == _integrationBase.CurrentProject?.Id && info.TeamId == _integrationBase.CurrentTeam?.Id)
            {
                DtoStructure rootStructure = null;
                if (info.RelatedObjectId == null)
                {
                    rootStructure = _integrationBase.ApiCore.Structures.GetStructure(info.ObjectId);
                }

                if (info.ChangeType == "STRUCTURE_UPDATED")
                {
                    StructureUpdated(info);
                }
                else if (info.ChangeType == "STRUCTURE_CREATED")
                {
                    SetCursor(Cursors.Wait);
                    try
                    {
                        if (rootStructure != null)
                        {
                            DtoStructure selectedItem = rootStructure;
                            List<DtoStructure> structures = new List<DtoStructure>();
                            structures.AddRange(Structures);
                            structures.Add(rootStructure);
                            structures = structures.OrderBy(s => s.Name).ToList();
                            Structures = structures;

                            Action action = new Action(() =>
                            {
                                SelectedItem = selectedItem;
                            });

                            if (StructuresListBox.CheckAccess())
                                action();
                            else
                            {
                                StructuresListBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
                            }
                        }
                        else
                            StructureCreated(info);
                    }
                    finally
                    {
                        SetCursor(null);
                    }
                }
                else if (info.ChangeType == "STRUCTURE_DELETED")
                {
                    if (info.RelatedObjectId == null)
                    {
                        Action action = new Action(() =>
                        {
                            DtoStructure toDelete = Structures.FirstOrDefault(s => s.Id == info.ObjectId);
                            if (toDelete != null && SelectedItem != null)
                            {
                                if (toDelete.Id == SelectedItem.Id)
                                {
                                    MessageBoxHelper.ShowInformation(string.Format("The structure '{0}' has been deleted from the Bimplus.", SelectedItem.Name));
                                }
                                List<DtoStructure> structures = new List<DtoStructure>();
                                structures.AddRange(Structures);
                                int index = structures.IndexOf(toDelete);
                                structures.Remove(toDelete);

                                Structures = structures;

                                if (Structures.Count > 0)
                                {
                                    if (index > 0)
                                        index--;
                                    SelectedItem = Structures[index];
                                }
                                else
                                    SelectedItem = null;
                            }
                            else
                            {
                                if (SelectedItem != null)
                                {
                                    if (StructureTreeView.Items.Count > 0)
                                    {
                                        TreeViewItem tvi = StructureTreeView.Items[0] as TreeViewItem;
                                        if (tvi != null)
                                        {
                                            TreeViewItem found = FindTreeViewItem(tvi, info.ObjectId);
                                            if (found != null)
                                            {
                                                TreeViewItem parent = found.Parent as TreeViewItem;
                                                parent.Items.Remove(found);
                                                parent.IsSelected = true;

                                                NotificationLabel.Foreground = Brushes.Red;
                                                NotificationLabel.Content = string.Format("Structure '{0}' deleted.", (found.Header as DtoTopologyStructure).Name);
                                            }
                                        }
                                    }
                                }
                            }
                        });

                        if (StructuresListBox.CheckAccess())
                            action();
                        else
                        {
                            StructuresListBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
                        }
                    }
                    else
                        StructureDeleted(info);
                }
            }
        }

        private TreeViewItem FindTreeViewItem(TreeViewItem treeViewItem, Guid structureId)
        {
            TreeViewItem found = null;
            if (treeViewItem.Items.Count > 0)
            {
                for (int i = 0; i < treeViewItem.Items.Count; i++)
                {
                    TreeViewItem child = treeViewItem.Items[i] as TreeViewItem;
                    if (child.Header is DtoTopologyStructure)
                    {
                        DtoTopologyStructure structure = child.Header as DtoTopologyStructure;
                        if (structure.Id == structureId)
                        {
                            found = child;
                            return found;
                        }
                    }

                    if (child.Items.Count > 0)
                    {
                        found = FindTreeViewItem(child, structureId);
                        if (found != null)
                            return found;
                    }
                }
            }

            return found;
        }

        private void StructureUpdated(ProjectHubItemChangedInfo info)
        {
            if (StructureTreeView != null && StructureTreeView.Items != null)
            {
                TreeViewItem rootItem = StructureTreeView.Items[0] as TreeViewItem;

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    TreeViewItem treeViewItem = FindStructureFromId(rootItem, info.ObjectId);
                    if (treeViewItem != null)
                    {
                        CancelEdit(treeViewItem, true);

                        if (info.RelatedObjectId == null) // Properties changed.
                        {
                            DtoTopologyStructure changedStructure = _integrationBase.ApiCore.Structures.GetTopology(_selectedTopology.Id, true);

                            DtoTopologyStructure actualTopology = FindStructureFromId(changedStructure, info.ObjectId);

                            if (actualTopology != null)
                            {
                                treeViewItem.DataContext = actualTopology;

                                DataTemplate template = Resources["HeaderTextBlockTemplate"] as DataTemplate;
                                if (template != null)
                                {
                                    treeViewItem.HeaderTemplate = template;
                                    treeViewItem.Header = actualTopology;
                                    treeViewItem.InvalidateVisual();
                                }

                                NotificationLabel.Foreground = Brushes.Blue;
                                NotificationLabel.Content = string.Format("Structure '{0}' changed.", actualTopology.Name);
                            }
                        }
                        else
                        {
                            // Structure moved.
                            DtoTopologyStructure changedStructure = _integrationBase.ApiCore.Structures.GetTopology(_selectedTopology.Id, true);
                            DtoTopologyStructure actualTopology = FindStructureFromId(changedStructure, info.ObjectId);

                            if (actualTopology != null)
                            {
                                // Get changed structure.
                                GetStructure_Click(this, new RoutedEventArgs());

                                NotificationLabel.Foreground = Brushes.Blue;
                                NotificationLabel.Content = string.Format("Structure '{0}' changed.", actualTopology.Name);

                                return;
                            }
                        }
                    }
                }), DispatcherPriority.Send);
            }
        }

        private static void WriteChildren(DtoTopologyStructure topology)
        {
            if (topology != null)
            {
                Debug.WriteLine(string.Format("Write children of '{0}'", topology.Name));
                Debug.WriteLine(string.Format("Name: {0}, Number: {1}", topology.Name, topology.Number));
                if (topology.Children != null)
                {
                    foreach (DtoTopologyStructure item in topology.Children)
                    {
                        Debug.WriteLine(string.Format("Name: {0}, Number: {1}", item.Name, item.Number));
                    }
                }
            }
        }

        private void StructureCreated(ProjectHubItemChangedInfo info)
        {
            if (StructureTreeView != null && StructureTreeView.Items != null)
            {
                // Do nothing, if the created object is the root node.
                if (info.ObjectId == SelectedTopology.Id)
                    return;

                TreeViewItem rootItem = StructureTreeView.Items[0] as TreeViewItem;

                if (rootItem != null)
                {
                    TreeViewItem selectedItem = null;
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        selectedItem = StructureTreeView.SelectedItem as TreeViewItem;

                        if (selectedItem != null)
                        {
                            CancelEdit(selectedItem, true);
                            DtoTopologyStructure currentTopology = null;

                            using (new TraceCodeTime("GetTopology", "StructureCreated"))
                            {
                                try
                                {
                                    currentTopology = _integrationBase.ApiCore.Structures.GetTopology(SelectedItem.Id, false);
                                }
                                catch (Exception ex)
                                {
                                    MessageBoxHelper.ShowInformation(ex.Message, _parentWindow);
                                }
                            }

                            if (currentTopology != null)
                            {
                                Guid? relatedObjectId = info.RelatedObjectId;

                                if (relatedObjectId != null && relatedObjectId != Guid.Empty)
                                {
                                    DtoTopologyStructure parentStructure = FindStructureFromId(currentTopology, (Guid)relatedObjectId);
                                    DtoTopologyStructure newStructure = FindStructureFromId(currentTopology, info.ObjectId);

                                    if (newStructure != null && parentStructure != null)
                                    {
                                        TreeViewItem parentTreeViewItem = FindStructureFromId(rootItem, parentStructure.Id);

                                        TreeViewItem childItem = CreateChildItem(newStructure);
                                        parentTreeViewItem.Items.Add(childItem);

                                        childItem.Focus();

                                        NotificationLabel.Foreground = Brushes.LimeGreen;
                                        NotificationLabel.Content = string.Format("Structure '{0}' created.", newStructure.Name);
                                    }
                                }
                            }
                        }

                    }), DispatcherPriority.Send);
                }
            }
        }

        private void StructureDeleted(ProjectHubItemChangedInfo info)
        {
            if (StructureTreeView != null && StructureTreeView.Items != null)
            {
                // No items to delete.
                if (StructureTreeView.Items == null || StructureTreeView.Items.Count == 0)
                    return;

                TreeViewItem rootItem = StructureTreeView.Items[0] as TreeViewItem;
                if (rootItem != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        TreeViewItem treeViewItemToDelete = FindStructureFromId(rootItem, info.ObjectId);
                        if (treeViewItemToDelete != null)
                        {
                            DtoTopologyStructure structureToDelete = treeViewItemToDelete.DataContext as DtoTopologyStructure;
                            string structureName = structureToDelete.Name;
                            treeViewItemToDelete.Focus();

                            DeleteStructure(true);

                            NotificationLabel.Foreground = Brushes.Red;
                            NotificationLabel.Content = string.Format("Structure '{0}' deleted.", structureName);
                        }
                    }), DispatcherPriority.Send);
                }
            }
        }

        private bool ListenToBimplusIsChecked()
        {
            bool result = false;

            if (Application.Current.Dispatcher.CheckAccess())
                return ListenToBimplus.IsChecked == true;
            else
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    result = ListenToBimplus.IsChecked == true;
                }), DispatcherPriority.Send);
            }

            return result;
        }

        #endregion SignalR events

        private void GetStructure_Click(object sender, RoutedEventArgs e)
        {
            SaveChangedStructure();

            ClearNotification();

            ProgressWindow.Text = "Load structure.";
            ProgressWindow.Show();

            try
            {
                using (new TraceCodeTime("GetTopology", "Load structure"))
                {
                    try
                    {
                        SelectedTopology = _integrationBase.ApiCore.Structures.GetTopology(SelectedItem.Id, true);
                        _orginalTopology = _integrationBase.ApiCore.Structures.GetTopology(SelectedItem.Id, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowInformation(ex.Message, _parentWindow);
                    }
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            if (SelectedTopology != null && _orginalTopology != null)
                FillTreeView(SelectedTopology);
            else
                MessageBoxHelper.ShowInformation("The structure could not be loaded.", _parentWindow);
        }

        private void CreateStructure_Click(object sender, RoutedEventArgs e)
        {
            SaveChangedStructure();

            string name = TextInputWindow.GetTextInput("Enter name");

            if (!string.IsNullOrEmpty(name))
            {
                DtoStructure structure = new DtoStructure { Name = name };

                structure = _integrationBase.ApiCore.Structures.PostStructure(_integrationBase.CurrentProject.Id, structure);

                if (structure != null)
                {
                    List<DtoStructure> structures = new List<DtoStructure>();
                    structures.AddRange(Structures);
                    structures.Add(structure);

                    structures = structures.OrderBy(s => s.Name).ToList();

                    Structures = new List<DtoStructure>(structures);

                    SelectedItem = structure;
                    if (SelectedItem != null)
                    {
                        HasObjects = true;
                        SelectedTopology = _integrationBase.ApiCore.Structures.GetTopology(SelectedItem.Id, true);
                        _orginalTopology = _integrationBase.ApiCore.Structures.GetTopology(SelectedItem.Id, true);

                        FillTreeView(SelectedTopology);
                    }
                }
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            ShowViewItemEditor();
        }

        private void CancelEditNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                CancelEdit(treeViewItem, true);
                bool isFocused = StructureTreeView.Focus();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveStructureName();
        }

        private void NewStructure_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                CancelEdit(treeViewItem, false);
                DtoTopologyStructure structure = treeViewItem.DataContext as DtoTopologyStructure;

                TreeViewItem parentTreeViewItem = treeViewItem.Parent as TreeViewItem;
                if (parentTreeViewItem == null)
                    parentTreeViewItem = treeViewItem;

                if (parentTreeViewItem != null)
                {
                    DtoTopologyStructure parentStructure = parentTreeViewItem.DataContext as DtoTopologyStructure;
                    if (parentStructure != null)
                    {
                        List<DtoTopologyStructure> children = parentStructure.Children as List<DtoTopologyStructure>;
                        if (children != null)
                        {
                            DtoTopologyStructure newStructure = new DtoTopologyStructure { Name = "New structure" };
                            // Type is a required field.
                            newStructure.Type = parentStructure.Type;

                            TreeViewItem newTreeViewItem = CreateChildItem(newStructure);

                            int index = parentTreeViewItem.Items.IndexOf(treeViewItem);

                            if (index < children.Count - 1)
                            {
                                index++;
                                children.Insert(index, newStructure);
                                parentTreeViewItem.Items.Insert(index, newTreeViewItem);
                            }
                            else
                            {
                                children.Add(newStructure);
                                parentTreeViewItem.Items.Add(newTreeViewItem);
                            }

                            for (int i = 0; i < children.Count; i++)
                            {
                                DtoTopologyStructure childTopology = children[i];
                                childTopology.Number = i + 1;
                                childTopology.Type = parentStructure.Type;
                            }

                            newTreeViewItem.IsSelected = true;
                            ShowViewItemEditor();
                        }
                    }
                }
            }
        }

        private void NewSubStructureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                CancelEdit(treeViewItem, false);
                DtoTopologyStructure structure = treeViewItem.DataContext as DtoTopologyStructure;

                if (structure != null)
                {
                    if (structure.Children == null)
                        structure.Children = new List<DtoTopologyStructure>();

                    DtoTopologyStructure newStructure = new DtoTopologyStructure { Name = "New structure", Parent = structure.Id };

                    // Type is a required field.
                    newStructure.Type = structure.Type;

                    TreeViewItem newTreeViewItem = CreateChildItem(newStructure);

                    List<DtoTopologyStructure> children = structure.Children as List<DtoTopologyStructure>;
                    if (children != null)
                    {
                        children.Insert(0, newStructure);
                        treeViewItem.Items.Insert(0, newTreeViewItem);

                        for (int i = 0; i < children.Count; i++)
                        {
                            DtoTopologyStructure childTopology = children[i];
                            childTopology.Number = i + 1;
                            childTopology.Type = structure.Type;
                        }

                        treeViewItem.ExpandSubtree();
                        newTreeViewItem.IsSelected = true;
                        ShowViewItemEditor();
                    }
                }
            }
        }

        private void DeleteStructureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteStructure(false);
        }

        private void DeleteStructure(bool removeDeletedStructure)
        {
            TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                // Delete old DtoTopologyStructure.
                TreeViewItem parentTreeViewItem = treeViewItem.Parent as TreeViewItem;

                DtoTopologyStructure structure = treeViewItem.DataContext as DtoTopologyStructure;

                DtoTopologyStructure parentStructure = parentTreeViewItem.Header as DtoTopologyStructure;
                if (parentStructure != null)
                {
                    if (_orgStructureNamesDictionary.ContainsKey(structure))
                        _orgStructureNamesDictionary.Remove(structure);

                    DtoTopologyStructure removedStructure = structure;

                    if (removedStructure != null && removedStructure.Parent != null && removedStructure.Parent != Guid.Empty)
                    {
                        List<DtoTopologyStructure> childList = parentStructure.Children as List<DtoTopologyStructure>;
                        bool removed = childList.Remove(structure);
                    }

                    if (removeDeletedStructure)
                    {
                        removedStructure = FindStructureFromId(_orginalTopology, structure.Id);
                        if (removedStructure != null && removedStructure.Parent != null && removedStructure.Parent != Guid.Empty)
                        {
                            parentStructure = FindStructureFromId(_orginalTopology, (Guid)removedStructure.Parent);

                            if (parentStructure != null)
                            {
                                List<DtoTopologyStructure> childList = parentStructure.Children as List<DtoTopologyStructure>;
                                DtoTopologyStructure toDelete = childList.FirstOrDefault(c => c.Id == structure.Id);
                                if (toDelete != null)
                                {
                                    bool removed = childList.Remove(toDelete);
                                }
                            }
                        }
                    }
                }

                if (parentTreeViewItem != null)
                {
                    int index = parentTreeViewItem.Items.IndexOf(treeViewItem);
                    if (index > 0)
                    {
                        TreeViewItem predecessor = parentTreeViewItem.Items[index - 1] as TreeViewItem;
                        if (predecessor != null)
                            predecessor.IsSelected = true;
                    }
                    else if (parentTreeViewItem.Items.Count > 1)
                    {
                        TreeViewItem successor = parentTreeViewItem.Items[1] as TreeViewItem;
                        if (successor != null)
                            successor.IsSelected = true;
                    }
                    else
                        parentTreeViewItem.IsSelected = true;

                    parentTreeViewItem.Items.Remove(treeViewItem);
                }
            }
        }

        private void StructureTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            CanModifyTreeViewItem = (e.NewValue as TreeViewItem) != _rootTreeViewItem;

            TreeViewItem oldItem = e.OldValue as TreeViewItem;

            if (oldItem != null)
            {
                CancelEdit(oldItem, true);
            }
        }

        private void EditNameMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu != null)
                {
                    foreach (object item in contextMenu.Items)
                    {
                        menuItem = item as MenuItem;
                        if (menuItem != null)
                        {
                            if (menuItem.Name == "EditNameMenuItem" || menuItem.Name == "SaveNameMenuItem"
                                || menuItem.Name == "DeleteStructureMenuItem" || menuItem.Name == "NewStructureMenuItem"
                                || menuItem.Name == "CancelEditNameMenuItem")
                                menuItem.IsEnabled = CanModifyTreeViewItem;
                        }
                    }
                    menuItem.IsEnabled = CanModifyTreeViewItem;
                }
            }
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SaveStructureName();
            }
        }

        private void DeleteSelectedStructureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                string message = string.Format("Are you sure you want to delete the structure '{0}'?", SelectedItem.Name);
                bool yes = MessageBoxHelper.ShowQuestion(message, MessageBoxResult.No, _parentWindow);

                if (yes)
                {
                    int index = Structures.IndexOf(SelectedItem);

                    if (SelectedTopology != null)
                    {
                        SaveChangedStructure();
                    }

                    bool deleted = _integrationBase.ApiCore.Structures.DeleteStructure(SelectedItem.Id);
                    bool removed = Structures.Remove(SelectedItem);
                    if (removed)
                    {
                        if (index > 0)
                        {
                            index--;
                        }
                        if (Structures.Count > index)
                        {
                            SelectedItem = Structures[index];
                            HasObjects = true;
                        }
                        else
                        {
                            SelectedItem = null;
                            HasObjects = false;
                        }
                    }
                }
            }
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Focus();
                textBox.SelectAll();
                textBox.SelectionStart = textBox.SelectionLength;
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool)
            {
                bool isVisible = (bool)e.NewValue;
                if (!isVisible)
                {
                    if (_applicationSettings != null)
                        _applicationSettings.Structures_ListenToBimplus = ListenToBimplus.IsChecked == true;

                    SaveChangedStructure();
                    InvalidateVisual();
                }
            }
        }

        #endregion events

        #region internal methods

        internal bool SaveChangedStructure()
        {
            TreeViewItem selectedItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                CancelEdit(selectedItem, true);
            }

            bool result = false;

            if (SelectedTopology == null)
                return false;

            List<DtoStructure> newStructures = new List<DtoStructure>();

            bool hasChangedItems = HasChangedItems();
            if (hasChangedItems)
            {
                string message = "Do you want to save the changes?";
                if (MessageBoxHelper.ShowQuestion(message, MessageBoxResult.Yes, _parentWindow))
                {
                    Guid oldId = Guid.Empty;
                    Guid.TryParse(SelectedTopology.Id.ToString(), out oldId);

                    string structureString = ClearGuidsFromTopology(SelectedTopology, true);

                    // PostStructure creates a new structure!
                    DtoStructure structure = _integrationBase.ApiCore.Structures.PostStructure(_integrationBase.CurrentProject.Id, SelectedTopology);

                    if (structure != null)
                    {
                        _orginalTopology = _integrationBase.ApiCore.Structures.GetTopology(structure.Id, true);

                        bool deleted = _integrationBase.ApiCore.Structures.DeleteStructure(oldId);
                        if (deleted)
                        {
                            int index = Structures.IndexOf(SelectedItem);
                            if (index >= 0)
                            {
                                newStructures.AddRange(Structures);
                                newStructures[index] = structure;

                                SelectedItem = structure;
                            }
                        }
                        else
                        {
                            message = "The old structure could not be deleted.";
                            MessageBoxHelper.ShowInformation(message, _parentWindow);
                        }
                    }

                    result = true;
                }
                else
                    result = true;
            }

            SelectedTopology = null;
            _orginalTopology = null;

            if (newStructures.Count > 0)
                Structures = new List<DtoStructure>(newStructures);

            return result;
        }

        internal void DisconnectSignalR()
        {
            _integrationBase.EventHandlerCore.SignalRProjectItemChanged -= EventHandlerCore_SignalRProjectItemChanged;
        }

        #endregion internal methods

        #region private methods

        private void FillTreeView(DtoTopologyStructure topology)
        {
            ClearTreeView();

            _orgStructureNamesDictionary.Add(topology, topology.Name);

            _rootTreeViewItem = new TreeViewItem { Header = topology };
            _rootTreeViewItem.DataContext = topology;

            if (Resources.Contains("HeaderTextBlockTemplate"))
            {
                DataTemplate dataTemplate = Resources["HeaderTextBlockTemplate"] as DataTemplate;
                _rootTreeViewItem.HeaderTemplate = dataTemplate;
            }

            StructureTreeView.Items.Add(_rootTreeViewItem);

            FillChilds(topology, _rootTreeViewItem);

            _rootTreeViewItem.IsSelected = true;

            ScrollViewer scrollViewer = ExtensionsClass.GetTypeObject(StructureTreeView, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
                scrollViewer.ScrollToHome();

            ExtensionsClass.ExpandTreeViewItem(_rootTreeViewItem);
            StructureTreeView.Focus();
        }

        private void ClearTreeView()
        {
            StructureTreeView.Items.Clear();
            _orgStructureNamesDictionary.Clear();
            NotificationLabel.Content = "";
        }

        private bool IsStructureBasedType(DtoTopologyStructure topology)
        {
            bool result = false;

            string typeString = topology.Type;
            if (topology != null && !string.IsNullOrEmpty(typeString))
            {
                if (typeString == typeof(Apartment).Name || typeString == typeof(Activity).Name || typeString == typeof(Energy).Name
                    || typeString == typeof(ScheduleManual).Name || typeString == typeof(Worksheet).Name || typeString == typeof(Schedule).Name)
                    result = true;
            }

            return result;
        }

        private void FillChilds(DtoTopologyStructure topology, TreeViewItem rootTreeViewItem)
        {
            if (topology.Children != null)
            {
                foreach (DtoTopologyStructure structure in topology.Children)
                {
                    if (!IsStructureBasedType(structure))
                    {
                        if (structure.Color != null)
                        {
                            Debug.WriteLine(BimplusColorToWpfColorConverter.ConvertUintToMediaColor(structure.Color).ToString());
                        }
                    }

                    TreeViewItem childItem = CreateChildItem(structure);
                    rootTreeViewItem.Items.Add(childItem);

                    FillChilds(structure, childItem);
                }
            }
        }

        private TreeViewItem CreateChildItem(DtoTopologyStructure structure)
        {
            _orgStructureNamesDictionary.Add(structure, structure.Name);

            TreeViewItem childItem = new TreeViewItem { Header = structure };
            childItem.DataContext = structure;

            if (Resources.Contains("HeaderTextBlockTemplate"))
            {
                DataTemplate dataTemplate = Resources["HeaderTextBlockTemplate"] as DataTemplate;
                childItem.HeaderTemplate = dataTemplate;
            }

            return childItem;
        }

        private void CancelEdit(TreeViewItem treeViewItem, bool cancel)
        {
            if (Resources.Contains("HeaderTextBlockTemplate"))
            {
                DtoTopologyStructure structure = treeViewItem.DataContext as DtoTopologyStructure;
                if (cancel)
                {
                    if (structure != null && _orgStructureNamesDictionary.ContainsKey(structure))
                    {
                        structure.Name = _orgStructureNamesDictionary[structure];
                    }
                }
                else
                {
                    if (structure != null && _orgStructureNamesDictionary.ContainsKey(structure))
                    {
                        _orgStructureNamesDictionary[structure] = structure.Name;

                        // That doesn't work. It will return only the "Others" group.
                        //if (structure.Type != null && !IsStructureBasedType(structure) && structure.Attributes == null)
                        //{
                        //    structure = _integrationBase.ApiCore.Structures.GetTopology(structure.Id, true);
                        //}
                    }
                }

                DataTemplate template = Resources["HeaderTextBlockTemplate"] as DataTemplate;
                if (template != null)
                {
                    treeViewItem.HeaderTemplate = template;
                    treeViewItem.Header = structure;
                    treeViewItem.InvalidateVisual();
                }
            }
        }

        private void SaveStructureName()
        {
            TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                CancelEdit(treeViewItem, false);
                bool isFocused = StructureTreeView.Focus();
            }
        }

        private void ShowViewItemEditor()
        {
            TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;
            if (treeViewItem != null)
            {
                DtoTopologyStructure structure = treeViewItem.DataContext as DtoTopologyStructure;

                if (Resources.Contains("HeaderTextBoxTemplate"))
                {
                    DataTemplate template = Resources["HeaderTextBoxTemplate"] as DataTemplate;
                    if (template != null)
                    {
                        treeViewItem.HeaderTemplate = template;
                        treeViewItem.Header = structure;
                    }
                }
            }
        }

        /// <summary>
        /// Set Id and Parent to Guid.Empty for PostStructure().
        /// </summary>
        /// <param name="topology"></param>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        /// <summary>
        private string ClearGuidsFromTopology(DtoTopologyStructure topology, bool rootNode = false)
        {
            if (IsStructureBasedType(topology))
                topology.Id = Guid.Empty;

            StringBuilder sb = new StringBuilder();

            if (rootNode)
                topology.Parent = null;
            else
                topology.Parent = Guid.Empty;

            if (topology.Children != null)
            {
                List<DtoTopologyStructure> children = topology.Children as List<DtoTopologyStructure>;
                if (children != null && children.Count == 0)
                    topology.Children = null;
            }

            sb.AppendLine(string.Format("Name: {0}", topology.Name));
            sb.AppendLine(string.Format("Id: {0}", topology.Id));
            sb.AppendLine(string.Format("Parent: {0}", topology.Parent));
            sb.AppendLine(string.Format("Color: {0}", topology.Color));
            sb.AppendLine(string.Format("Type: {0}", topology.Type));
            sb.AppendLine(string.Format("Children: {0}", topology.Children?.Count()));
            sb.AppendLine(string.Format("Attributes: {0}", topology.Attributes?.Count));
            sb.AppendLine();

            if (topology.Children != null)
            {
                foreach (DtoTopologyStructure child in topology.Children)
                {
                    string childValue = ClearGuidsFromTopology(child);
                    sb.Append(childValue);
                }
            }

            return sb.ToString();
        }

        private bool HasChangedItems()
        {
            bool result = false;

            if (_orginalTopology == null && SelectedTopology == null)
                return false;

            // _selectedTopology.Id == Guid.Empty => deleted structure
            if (SelectedTopology != null && SelectedTopology.Id == Guid.Empty)
                return false;

            int orgCount = 0;
            if (_orginalTopology != null)
                StructureCount(_orginalTopology, ref orgCount);

            int selectedCount = 0;
            if (SelectedTopology != null)
                StructureCount(SelectedTopology, ref selectedCount);

            if (orgCount != selectedCount)
                return true;

            result = CompareTopologies(_orginalTopology, SelectedTopology);

            return result;
        }

        private void StructureCount(DtoTopologyStructure topology, ref int count)
        {
            if (topology.Children != null)
            {
                foreach (DtoTopologyStructure child in topology.Children)
                {
                    count++;
                    StructureCount(child, ref count);
                }
            }
        }

        private bool CompareTopologies(DtoTopologyStructure orgTopology, DtoTopologyStructure selectedTopology)
        {
            bool result = false;

            if (orgTopology == null)
                return false;

            if (orgTopology.Name != selectedTopology.Name)
                return true;

            if (orgTopology.Color != selectedTopology.Color)
                return true;

            if (orgTopology.Children == null && selectedTopology.Children != null)
                return true;

            if (orgTopology.Children != null && selectedTopology.Children == null)
                return true;

            if (orgTopology.Children == null && selectedTopology.Children == null)
                return false;

            if (orgTopology.Children.Count() != selectedTopology.Children.Count())
                return true;

            List<DtoTopologyStructure> orgList = orgTopology.Children.ToList();
            List<DtoTopologyStructure> selectedList = selectedTopology.Children.ToList();

            for (int i = 0; i < orgList.Count; i++)
            {
                result = CompareTopologies(orgList[i], selectedList[i]);
                if (result)
                    return result;
            }

            return result;
        }

        private TreeViewItem FindStructureFromId(TreeViewItem root, Guid guid)
        {
            TreeViewItem treeViewItem = null;

            if (root != null)
            {
                DtoTopologyStructure structure = root.DataContext as DtoTopologyStructure;
                if (structure != null && structure.Id == guid)
                {
                    return root;
                }

                foreach (TreeViewItem item in root.Items)
                {
                    treeViewItem = FindStructureFromId(item, guid);
                    if (treeViewItem != null)
                        break;
                }
            }

            return treeViewItem;
        }

        private DtoTopologyStructure FindStructureFromId(DtoTopologyStructure root, Guid guid)
        {
            DtoTopologyStructure structure = null;

            if (root != null && root.Id == guid)
            {
                return root;
            }

            foreach (DtoTopologyStructure child in root?.Children)
            {
                structure = FindStructureFromId(child, guid);
                if (structure != null)
                    break;
            }

            return structure;
        }

        private void ClearNotification()
        {
            NotificationLabel.Content = " ";
        }

        private void SetCursor(Cursor cursor)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                Cursor = cursor;
            }
            else Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Cursor = cursor;
            }), DispatcherPriority.Send);
        }

        #endregion private methods

        #region UserControl events

        private void StructureTreeView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TreeView treeView = sender as TreeView;
            if (treeView != null & treeView.SelectedItem != null)
            {
                if (e.Key == Key.F2)
                {
                    ShowViewItemEditor();
                }
                else if (e.Key == Key.Escape)
                {
                    TreeViewItem treeViewItem = StructureTreeView.SelectedItem as TreeViewItem;

                    CancelEdit(treeViewItem, true);

                    treeViewItem.IsSelected = true;
                    bool isFocused = StructureTreeView.Focus();
                }
            }
        }

        private void NotifyPropertyChangedUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            StructuresListBox.Focus();
        }

        private void ListenToBimplus_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if (checkBox.IsChecked != true)
                    NotificationLabel.Content = "";
            }
        }

        #endregion UserControl events
    }
}