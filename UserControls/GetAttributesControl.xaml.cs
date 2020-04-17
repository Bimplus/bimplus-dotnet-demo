using AllplanBimplusDemo.Classes;
using AllplanBimplusDemo.WinForms;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.IfcData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for GetAttributesControl.xaml
    /// </summary>
    public partial class GetAttributesControl : BaseAttributesUserControl
    {
        public GetAttributesControl()
        {
            InitializeComponent();
            DataContext = this;

            Type type = typeof(DtoAttributDefinition);
            _dtoAttributDefinitionProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        }

        #region private member      

        private IntegrationBase _integrationBase;
        private Window _parentWindow;
        private List<PropertyInfo> _dtoAttributDefinitionProperties;

        #endregion private member

        #region properties

        /// <summary>
        /// All object types from the models.
        /// </summary>
        public List<Type> ObjectTypes
        {
            get { return _objectTypes; }

            set { _objectTypes = value; NotifyPropertyChanged(); }
        }

        private List<Type> _objectTypes;

        /// <summary>
        /// All model names.
        /// </summary>
        public string ProjectModels
        {
            get { return _models; }

            set { _models = value; NotifyPropertyChanged(); }
        }

        private string _models;

        /// <summary>
        /// Selected object type.
        /// </summary>
        public Type SelectedItem
        {
            get { return _selectedItem; }

            set { _selectedItem = value; NotifyPropertyChanged(); }
        }

        private Type _selectedItem;


        /// <summary>
        /// The TreeView has child nodes.
        /// </summary>
        public bool HasTreeViewItems
        {
            get { return _hasTreeViewItems; }

            set { _hasTreeViewItems = value; NotifyPropertyChanged(); }
        }

        private bool _hasTreeViewItems = false;

        #endregion

        #region public methods

        /// <summary>
        /// Load start properties.
        /// </summary>
        /// <param name="integrationBase"></param>
        /// <param name="parent"></param>
        public void LoadContent(IntegrationBase integrationBase, Window parent)
        {
            _integrationBase = integrationBase;
            _parentWindow = parent;

            if (_integrationBase.CurrentProject != null)
            {
                DtoProject project = _integrationBase.ApiCore.Projects.GetProject(_integrationBase.CurrentProject.Id);

                ProjectModels = ExtensionsClass.GetDivisonNames(project, _integrationBase);
            }

            Dictionary<DbLayer, Dictionary<Type, List<DtObject>>> layerTypeObjectsDictionary = ExtensionsClass.ReadLayerTypeObjectsDictionary(_integrationBase);

            List<Type> types = ExtensionsClass.GetTypes(layerTypeObjectsDictionary, parent);

            if (types.Count > 0)
            {
                SelectedItem = types[0];
                HasObjects = true;
            }

            ObjectTypes = types;
        }

        #endregion public methods

        #region ui events

        /// <summary>
        /// Fill the TreeView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetObjects_Click(object sender, RoutedEventArgs e)
        {
            if (!HasAllowedFlags())
                return;

            AttributeTreeView.Items.Clear();

            List<DtObject> items = null;

            ProgressWindow.Text = "Get properties.";
            ProgressWindow.Show();

            try
            {
                items = _integrationBase.ApiCore.DtObjects.GetObjects(_integrationBase.CurrentProject.Id, SelectedItem,
                     AttributeDefinitionIsChecked, PsetCheckBoxIsChecked, InternalValuesCheckBoxIsChecked);
            }
            finally
            {
                ProgressWindow.Hide();
            }

            if (items != null && items.Count > 0)
            {
                DtObject dtObject = items.FirstOrDefault();
                TreeViewItem rootTreeViewItem = new TreeViewItem { Header = dtObject.GetType().Name };
                rootTreeViewItem.Header = dtObject.GetType().Name;

                AttributeTreeView.Items.Add(rootTreeViewItem);

                TreeViewItem groupTreeViewItem;
                TreeViewItem attributeTreeViewItem;

                foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in dtObject.AttributeGroups)
                {
                    groupTreeViewItem = new TreeViewItem();

                    if (dtObject.LocalizedAttributeGroups != null && dtObject.LocalizedAttributeGroups.ContainsKey(kvpGroup.Key))
                        groupTreeViewItem.Header = dtObject.LocalizedAttributeGroups[kvpGroup.Key];
                    else
                        groupTreeViewItem.Header = kvpGroup.Key;

                    rootTreeViewItem.Items.Add(groupTreeViewItem);

                    foreach (KeyValuePair<string, object> kvpAttribute in kvpGroup.Value)
                    {
                        // Can not set in TreeView.ItemTemplate.
                        attributeTreeViewItem = new TreeViewItem { FontSize = 13 };

                        string header = null;
                        string value = "";
                        string definitionValue = "";

                        DtoAttributDefinition dtoAttributDefinition = null;

                        if (!AttributeDefinitionIsChecked)
                        {
                            if (kvpAttribute.Value != null)
                                value = kvpAttribute.Value.ToString();
                            header = string.Format("{0} : {1}", kvpAttribute.Key, value);
                        }
                        else
                        {
                            if (kvpAttribute.Value != null)
                            {
                                JObject jObject = kvpAttribute.Value as JObject;
                                if (jObject != null)
                                {
                                    dtoAttributDefinition = jObject.ToObject<DtoAttributDefinition>();
                                    if (dtoAttributDefinition != null && dtoAttributDefinition.EnumDefinition == null)
                                    {
                                        if (dtoAttributDefinition.Value != null)
                                            value = dtoAttributDefinition.Value.ToString();
                                    }
                                    else
                                    {
                                        value = ExtensionsClass.GetEnumDefinitionValue(dtoAttributDefinition) as string;
                                    }

                                    definitionValue = ExtensionsClass.DtoAttributDefinitionToString(dtoAttributDefinition, _dtoAttributDefinitionProperties);

                                    header = string.Format("{0} : {1}", dtoAttributDefinition.Name, value);
                                }
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

                ScrollViewer scrollViewer = ExtensionsClass.GetTypeObject(AttributeTreeView, typeof(ScrollViewer)) as ScrollViewer;
                if (scrollViewer != null)
                    scrollViewer.ScrollToHome();

                HasTreeViewItems = rootTreeViewItem.Items.Count > 0;
            }
            else
                MessageBoxHelper.ShowInformation("No objects available.", _parentWindow);
        }

        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            SetIsExpandedValue(true);
        }

        private void Collaps_Click(object sender, RoutedEventArgs e)
        {
            SetIsExpandedValue(false);
        }

        private void SetIsExpandedValue(bool isExpanded)
        {
            if (AttributeTreeView.Items.Count > 0)
            {
                TreeViewItem rootItem = AttributeTreeView.Items[0] as TreeViewItem;
                foreach (object item in rootItem.Items)
                {
                    TreeViewItem treeViewItem = item as TreeViewItem;
                    if (treeViewItem != null)
                        treeViewItem.IsExpanded = isExpanded;
                }
            }
        }

        #endregion ui events

        #region private methods

        #endregion private methods

        #region UserControl events

        private void BaseAttributesUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ObjectTypesList.Focus();
        }

        #endregion UserControl events
    }
}
