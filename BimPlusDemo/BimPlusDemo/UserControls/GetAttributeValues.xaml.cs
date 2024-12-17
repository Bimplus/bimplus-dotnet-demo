
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.Utilities.V2;
using BimPlusDemo.Annotations;
using Newtonsoft.Json.Linq;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace BimPlusDemo.UserControls
{

    /// <summary>
    /// Interaction logic for GetAttributeValues.xaml
    /// </summary>
    public partial class GetAttributeValues : IDisposable, INotifyPropertyChanged
    {
        private IntegrationBase IntBase => MainWindow.IntBase;
        private readonly List<PropertyInfo> _dtoAttributDefinitionProperties;

        public GetAttributeValues()
        {
            InitializeComponent();
            DataContext = this;
            IntBase.EventHandlerCore.ObjectSelected += EventHandlerCoreOnObjectSelected;
             
            Type type = typeof(DtoAttributDefinition);
            _dtoAttributDefinitionProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        }

        public DtObject? SelectedObject
        {
            get => _selectedObject;
            set { _selectedObject = value; OnPropertyChanged(); }
        }
        private DtObject? _selectedObject;

        /// <summary>
        ///  Property PsetCheckBoxIsChecked.
        /// </summary>
        public bool PsetCheckBoxIsChecked
        {
            get => _psetCheckBoxIsChecked;
            set
            {
                if (_psetCheckBoxIsChecked == value)
                    return;
                _psetCheckBoxIsChecked = value;
                if (_selectedObject != null)
                    UpdateSelectedObject(_selectedObject.Id);
                OnPropertyChanged();
            }
        }
        private bool _psetCheckBoxIsChecked = true;

        /// <summary>
        /// Property AttributeDefinitionIsChecked.
        /// </summary>
        public bool AttributeDefinitionIsChecked
        {
            get => _attributeDefinitionIsChecked;
            set
            {
                if (_attributeDefinitionIsChecked == value)
                    return;
                if (!AllowedFlags(value, InternalValuesCheckBoxIsChecked))
                    return;
                _attributeDefinitionIsChecked = value;
                if (_selectedObject != null)
                    UpdateSelectedObject(_selectedObject.Id);
                OnPropertyChanged();
            }
        }
        private bool _attributeDefinitionIsChecked = true;

        /// <summary>
        /// Property InternalValuesCheckBoxIsChecked.
        /// </summary>
        public bool InternalValuesCheckBoxIsChecked
        {
            get => _internalValuesCheckBoxIsChecked;
            set
            {
                if (_internalValuesCheckBoxIsChecked == value)
                    return;
                if (!AllowedFlags(value, AttributeDefinitionIsChecked))
                    return;
                _internalValuesCheckBoxIsChecked = value;
                if (_selectedObject != null)
                    UpdateSelectedObject(_selectedObject.Id);
                OnPropertyChanged();
            }
        }
        private bool _internalValuesCheckBoxIsChecked;

        /// <summary>
        /// Function HasAllowedFlags.
        /// </summary>
        /// <returns></returns>
        protected bool AllowedFlags(bool value1, bool value2)
        {
            //if (InternalValuesCheckBoxIsChecked && AttributeDefinitionIsChecked)
            if (value1 && value2)
            {
                string message = "The combination of these flags is not supported.";
                MessageBox.Show(message);
                return false;
            }
            return true;
        }

        private void EventHandlerCoreOnObjectSelected(object sender, BimPlusEventArgs? e)
        {
            if (e == null || e.Id == Guid.Empty)
            {
                SelectedObject = null;
                return;
            }

            if (SelectedObject?.Id == e.Id)
                return;

            UpdateSelectedObject(e.Id);
        }

        public void UpdateSelectedObject(Guid id)
        {
            ObjectRequestProperties flags = new ObjectRequestProperties();
            if (AttributeDefinitionIsChecked) flags |= ObjectRequestProperties.AttributDefinition;
            if (PsetCheckBoxIsChecked) flags |= ObjectRequestProperties.Pset;
            if (InternalValuesCheckBoxIsChecked) flags |= ObjectRequestProperties.InternalValues;
            SelectedObject = IntBase.ApiCore.DtObjects.GetObject(id, flags);
        }

        /// <summary>
        /// dispose eventHandler
        /// </summary>
        public void Dispose()
        {
            IntBase.EventHandlerCore.ObjectSelected -= EventHandlerCoreOnObjectSelected;
        }

        //private void EditProperties(object sender, RoutedEventArgs e)
        //{
        //    ;
        //}

        /// <summary>
        /// Expander
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            SetIsExpandedValue(true);
        }

        /// <summary>
        /// Collapse 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Collaps_Click(object sender, RoutedEventArgs e)
        {
            SetIsExpandedValue(false);
        }

        /// <summary>
        /// SetIsExpandedValue
        /// </summary>
        /// <param name="isExpanded"></param>
        private void SetIsExpandedValue(bool isExpanded)
        {
            if (AttributeTreeView.Items.Count > 0)
            {
                TreeViewItem? rootItem = AttributeTreeView.Items[0] as TreeViewItem;
                if (rootItem == null) return;
                foreach (object item in rootItem.Items)
                {
                    if (item is TreeViewItem treeViewItem)
                        treeViewItem.IsExpanded = isExpanded;
                }
            }
        }

        /// <summary>
        /// AttributeTreeView_OnDataContextChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeTreeView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttributeTreeView.Items.Clear();
            if (SelectedObject == null)
                return;
            TreeViewItem rootTreeViewItem = new TreeViewItem { Header = SelectedObject.GetType().Name };
            rootTreeViewItem.Header = SelectedObject.GetType().Name;

            AttributeTreeView.Items.Add(rootTreeViewItem);

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in SelectedObject.AttributeGroups)
            {
                var groupTreeViewItem = new TreeViewItem();

                if (SelectedObject.LocalizedAttributeGroups != null && SelectedObject.LocalizedAttributeGroups.ContainsKey(kvpGroup.Key))
                    groupTreeViewItem.Header = SelectedObject.LocalizedAttributeGroups[kvpGroup.Key];
                else
                    groupTreeViewItem.Header = kvpGroup.Key;

                rootTreeViewItem.Items.Add(groupTreeViewItem);

                foreach (KeyValuePair<string, object> kvpAttribute in kvpGroup.Value)
                {
                    var attributeTreeViewItem = new TreeViewItem { FontSize = 13 };

                    string? header = null;
                    string? value = "null";
                    string definitionValue = "";

                    DtoAttributDefinition? dtoAttributDefinition = null;

                    if (!AttributeDefinitionIsChecked)
                    {
                        value = kvpAttribute.Value.ToString();
                        header = $"{kvpAttribute.Key} : {value}";
                    }
                    else
                    {
                        if (kvpAttribute.Value is JObject jObject)
                        {
                            dtoAttributDefinition = jObject.ToObject<DtoAttributDefinition>();
                            if (dtoAttributDefinition is { EnumDefinition: null })
                            {
                                if (dtoAttributDefinition.Value != null)
                                    value = dtoAttributDefinition.Value.ToString();
                            }
                            else
                            {
                                value = ExtensionsClass.GetEnumDefinitionValue(dtoAttributDefinition) as string;
                            }
                            definitionValue = ExtensionsClass.DtoAttributDefinitionToString(dtoAttributDefinition, _dtoAttributDefinitionProperties);

                            header = $"{dtoAttributDefinition?.Name} : {value}";
                        }
                    }

                    attributeTreeViewItem.Header = header;

                    if (!string.IsNullOrEmpty(definitionValue))
                        attributeTreeViewItem.ToolTip = definitionValue;

                    if ((dtoAttributDefinition != null && dtoAttributDefinition.IsInternal != true) || dtoAttributDefinition == null)
                        groupTreeViewItem.Items.Add(attributeTreeViewItem);
                }
            }

            ExtensionsClass.ExpandTreeViewItem(rootTreeViewItem);
            AttributeTreeView.InvalidateVisual();

            if (ExtensionsClass.GetTypeObject(AttributeTreeView, typeof(ScrollViewer)) is ScrollViewer scrollViewer)
                scrollViewer.ScrollToHome();

            //HasTreeViewItems = rootTreeViewItem.Items.Count > 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

