using AllplanBimplusDemo.Classes;
using AllplanBimplusDemo.Controls;
using AllplanBimplusDemo.WinForms;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.IfcData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for SetAttributesControl.xaml
    /// </summary>
    public partial class SetAttributesControl : BaseAttributesUserControl
    {
        public SetAttributesControl()
        {
            InitializeComponent();
            DataContext = this;

            Type type = typeof(DtoAttributDefinition);
            _dtoAttributDefinitionProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            _focusableControls = new List<Control>();
        }

        #region private member

        private List<PropertyInfo> _dtoAttributDefinitionProperties;

        private IntegrationBase _integrationBase;
        private Window _parentWindow;
        private List<DtObject> _objects;
        private Dictionary<DtObject, Dictionary<string, DtoAttributesGroup>> _orgObjectsValues;

        private List<TreeViewItem> _treeViewItems = new List<TreeViewItem>();

        private Control _lastFocusedControl;

        private int _dataTemplatesCount;
        // List of TreeViewItems with Controls
        private List<TreeViewItem> _notStringHeaders;
        private List<Control> _focusableControls;
        private int _notStringHeadersCount;

        #endregion private member

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

        public void UnloadContent()
        {
            foreach (Control control in _focusableControls)
            {
                control.GotFocus -= Control_GotFocus;
            }
        }

        /// <summary>
        /// Save all changed attributes.
        /// </summary>
        public void SaveChangedAttributes()
        {
            if (ChangesSaved)
                return;

            DtObject dtObject = null;
            if (_selectedObject != null)
            {
                dtObject = _objects.FirstOrDefault(i => i.Id == _selectedObject.Id);
                if (dtObject != null)
                {
                    if (_lastFocusedControl != null)
                    {
                        // Get last value.
                        object dataContext = _lastFocusedControl.DataContext;
                        DtoAttributDefinition dtoAttributDefinition = dataContext as DtoAttributDefinition;

                        Type type = _lastFocusedControl.GetType();

                        switch (type.Name)
                        {
                            case "TextBox":
                                TextBox textBox = _lastFocusedControl as TextBox;
                                if (dtoAttributDefinition != null)
                                {
                                    if (string.IsNullOrEmpty(textBox.Text))
                                        dtoAttributDefinition.Value = null;
                                    else
                                        dtoAttributDefinition.Value = textBox.Text;
                                }
                                break;
                            case "DateTimeTextBox":
                                DateTimeTextBox dateTimeTextBox = _lastFocusedControl as DateTimeTextBox;
                                if (dataContext is DateTimeWrapper)
                                {
                                    DateTimeWrapper dateTimeWrapper = dataContext as DateTimeWrapper;
                                    dateTimeWrapper.DateTime = dateTimeTextBox.DateTimeValue;
                                }
                                break;
                            case "DoubleTextBox":
                                DoubleTextBox doubleTextBox = _lastFocusedControl as DoubleTextBox;
                                if (dataContext is DoubleWrapper)
                                {
                                    DoubleWrapper doubleWrapper = dataContext as DoubleWrapper;
                                    doubleWrapper.Double = doubleTextBox.DoubleValue;
                                }
                                break;
                            case "IntegerTextBox":
                                IntegerTextBox integerTextBox = _lastFocusedControl as IntegerTextBox;
                                if (dataContext is IntWrapper)
                                {
                                    IntWrapper intWrapper = dataContext as IntWrapper;
                                    intWrapper.Int = integerTextBox.IntValue;
                                }
                                break;
                            case "CheckBox":
                                CheckBox checkBox = _lastFocusedControl as CheckBox;
                                if (dtoAttributDefinition != null)
                                    dtoAttributDefinition.Value = checkBox.IsChecked;
                                break;
                            case "ComboBox":
                                ComboBox comboBox = _lastFocusedControl as ComboBox;
                                DtoAttributDefinitionWrapper wrapper = dataContext as DtoAttributDefinitionWrapper;
                                if (wrapper != null)
                                    wrapper.Value = comboBox.SelectedItem;
                                break;
                            default:
                                break;
                        }
                    }

                    GetChangedAttibutes(dtObject);
                }
            }
        }

        #endregion public methods

        #region properties

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
        /// All object types from the models.
        /// </summary>
        public List<Type> ObjectTypes
        {
            get { return _objectTypes; }

            set { _objectTypes = value; NotifyPropertyChanged(); }
        }

        private List<Type> _objectTypes;

        /// <summary>
        /// Selected object type.
        /// </summary>
        public Type SelectedItem
        {
            get { return _selectedItem; }

            set
            {
                if (_selectedItem != value)
                {
                    DtObject dtObject = null;
                    if (_selectedObject != null)
                    {
                        dtObject = _objects.FirstOrDefault(i => i.Id == _selectedObject.Id);
                        if (dtObject != null)
                        {
                            SaveChangedAttributes();
                        }
                    }

                    // Clear DataGrid and TreeView.
                    UnloadContent();
                    if (ObjectsDataGrid.ItemsSource != null)
                    {
                        DataGridDataList = new List<DataGridData>();
                    }

                    AttributeTreeView.Items.Clear();
                    _treeViewItems.Clear();

                    _selectedItem = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Type _selectedItem;

        /// <summary>
        /// List of DataGrid objects. 
        /// </summary>
        public List<DataGridData> DataGridDataList
        {
            get { return _dataGridDataList; }

            set { _dataGridDataList = value; NotifyPropertyChanged(); }
        }

        private List<DataGridData> _dataGridDataList;

        /// <summary>
        /// The selected object from type SelectedItem.
        /// </summary>
        public DataGridData SelectedObject
        {
            get { return _selectedObject; }

            set
            {
                if (value != _selectedObject)
                {
                    DtObject dtObject = null;
                    if (_selectedObject != null)
                    {
                        dtObject = _objects.FirstOrDefault(i => i.Id == _selectedObject.Id);
                        if (dtObject != null)
                        {
                            GetChangedAttibutes(dtObject);
                            ChangesSaved = false;
                        }
                    }

                    _selectedObject = value;

                    if (_selectedObject != null)
                    {
                        dtObject = _objects.FirstOrDefault(i => i.Id == _selectedObject.Id);

                        FillTreeView(dtObject);
                    }

                    NotifyPropertyChanged();
                }
            }
        }

        private DataGridData _selectedObject;

        #endregion properties

        #region private methods

        /// <summary>
        /// Get objects from type 'type'.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>The selected object type.</returns>
        private List<DtObject> GetObjectData(Type type)
        {
            List<DtObject> items = new List<DtObject>();
            if (!HasAllowedFlags())
                return items;

            ProgressWindow.Text = string.Format("Load objects {0}", SelectedItem.Name);
            ProgressWindow.Show();
            try
            {
                try
                {
                    using (new TraceCodeTime(string.Format("Load objects {0}", type.Name), "Import types"))
                    {
                        Guid guid = _integrationBase.CurrentProject.Id;
                        items = _integrationBase.ApiCore.DtObjects
                            .GetObjects(guid, type, AttributeDefinitionIsChecked, PsetCheckBoxIsChecked, InternalValuesCheckBoxIsChecked);
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            return items;
        }

        private Dictionary<DtObject, Dictionary<string, DtoAttributesGroup>> GetOrgAttributeValues(List<DtObject> objects)
        {
            Dictionary<DtObject, Dictionary<string, DtoAttributesGroup>> result = new Dictionary<DtObject, Dictionary<string, DtoAttributesGroup>>();

            foreach (DtObject dtObject in objects)
            {
                Dictionary<string, DtoAttributesGroup> attributeGroups = dtObject.AttributeGroups as Dictionary<string, DtoAttributesGroup>;

                if (attributeGroups != null)
                {
                    foreach (KeyValuePair<string, DtoAttributesGroup> groupKvp in attributeGroups)
                    {
                        DtoAttributesGroup attributeGroup = groupKvp.Value;

                        bool hasDefinition = true;
                        List<KeyValuePair<string, object>> kvpList = attributeGroup.ToList();

                        for (int i = 0; i < kvpList.Count; i++)
                        {
                            KeyValuePair<string, object> kvp = kvpList[i];

                            DtoAttributDefinition definition = kvp.Value as DtoAttributDefinition;

                            if (definition == null)
                            {
                                hasDefinition = false;

                                JObject jObject = kvp.Value as JObject;
                                if (jObject != null)
                                {
                                    definition = GetAttributDefinitionFromJObject(jObject);

                                    if (definition != null)
                                    {
                                        if (definition.DataType == typeof(Int32))
                                        {
                                            if (definition.Value != null && definition.Value is long)
                                                definition.Value = Convert.ToInt32(definition.Value);
                                        }

                                        KeyValuePair<string, object> newKvp = new KeyValuePair<string, object>(kvp.Key, definition);
                                        kvpList.Remove(kvp);
                                        kvpList.Insert(i, newKvp);
                                    }
                                }
                            }
                        }

                        // Create DtoAttributesGroup with DtoAttributDefinition.
                        if (!hasDefinition)
                        {
                            attributeGroup.Clear();
                            foreach (KeyValuePair<string, object> kvp in kvpList)
                            {
                                attributeGroup.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }

                Dictionary<string, DtoAttributesGroup> attributes = CloneAttributeGroups(attributeGroups);

                result.Add(dtObject, attributes);
            }

            return result;
        }

        private static Dictionary<string, DtoAttributesGroup> CloneAttributeGroups(Dictionary<string, DtoAttributesGroup> attributeGroups)
        {
            Dictionary<string, DtoAttributesGroup> clonedGroups = new Dictionary<string, DtoAttributesGroup>();

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in attributeGroups)
            {
                DtoAttributesGroup newGroup = new DtoAttributesGroup();

                foreach (KeyValuePair<string, object> kvp in kvpGroup.Value)
                {
                    DtoAttributDefinition orgDef = kvp.Value as DtoAttributDefinition;
                    if (orgDef != null)
                    {
                        DtoAttributDefinition newDefinition = orgDef.Clone() as DtoAttributDefinition;

                        if (newDefinition.DataType == typeof(Int32))
                        {
                            if (newDefinition.Value != null && newDefinition.Value is long)
                                newDefinition.Value = Convert.ToInt32(newDefinition.Value);
                        }

                        newGroup.Add(kvp.Key, newDefinition);
                    }
                    else
                    {
                        object value = kvp.Value;
                    }
                }

                clonedGroups.Add(kvpGroup.Key, newGroup);
            }

            return clonedGroups;
        }

        private static DtoAttributDefinition GetAttributDefinitionFromJObject(JObject jObject)
        {
            DtoAttributDefinition definition = null;
            try
            {
                definition = jObject.ToObject<DtoAttributDefinition>();
                if (definition.Value != null)
                {
                    if (definition.DataType == typeof(double))
                    {
                        if (definition.Value is string)
                        {
                            double doubleValue;
                            if (double.TryParse(definition.Value as string, out doubleValue))
                            {
                                definition.Value = doubleValue;
                            }
                        }
                    }

                    if (definition.DataType == typeof(Int32))
                    {
                        if (definition.Value is long)
                            definition.Value = Convert.ToInt32(definition.Value);
                    }

                    if (definition.DataType == typeof(bool) && definition.EnumDefinition != null)
                    {
                        if (definition.Value is bool)
                        {
                            if ((bool)definition.Value == true)
                                definition.Value = 1;
                            else
                                definition.Value = 0;
                        }
                        if (definition.Value is long)
                        {
                            definition.Value = Convert.ToInt32(definition.Value);
                        }
                        else if (definition.Value is Int32)
                        {
                            definition.Value = (int)definition.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message);
            }

            return definition;
        }

        private void FillTreeView(DtObject dtObject)
        {
            UnloadContent();

            AttributeTreeView.Items.Clear();
            _treeViewItems.Clear();

            TreeViewItem rootTreeViewItem = new TreeViewItem { Header = dtObject.GetType().Name };

            AttributeTreeView.Items.Add(rootTreeViewItem);

            _treeViewItems.Add(rootTreeViewItem);

            TreeViewItem groupTreeViewItem;
            TreeViewItem attributeTreeViewItem;

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in dtObject.AttributeGroups)
            {
                groupTreeViewItem = new TreeViewItem();
                if (dtObject.LocalizedAttributeGroups != null && dtObject.LocalizedAttributeGroups.ContainsKey(kvpGroup.Key))
                    groupTreeViewItem.Header = dtObject.LocalizedAttributeGroups[kvpGroup.Key];
                else
                    groupTreeViewItem.Header = kvpGroup.Key;

                groupTreeViewItem.Background = null;
                rootTreeViewItem.Items.Add(groupTreeViewItem);

                _treeViewItems.Add(groupTreeViewItem);

                foreach (KeyValuePair<string, object> kvpAttribute in kvpGroup.Value)
                {
                    // Can not set in TreeView.ItemTemplate.
                    attributeTreeViewItem = new TreeViewItem();

                    object value = null;
                    string definitionValue = null;

                    DtoAttributDefinition dtoAttributeDefinition = null;
                    DataTemplate dataTemplate = null;
                    DtoAttributDefinitionWrapper dtoAttributDefinitionWrapper = null;
                    DoubleWrapper doubleWrapper = null;
                    IntWrapper intWrapper = null;
                    DateTimeWrapper dataTimeWrapper = null;

                    if (kvpAttribute.Value != null)
                    {
                        dtoAttributeDefinition = kvpAttribute.Value as DtoAttributDefinition;
                        if (dtoAttributeDefinition != null && dtoAttributeDefinition.Value != null)
                        {
                            value = dtoAttributeDefinition.Value;
                        }

                        if ((dtoAttributeDefinition.DataType == typeof(string) || dtoAttributeDefinition.DataType == typeof(Guid)) && dtoAttributeDefinition.EnumDefinition == null)
                        {
                            if (Resources.Contains("HeaderTextBoxTemplate"))
                            {
                                dataTemplate = Resources["HeaderTextBoxTemplate"] as DataTemplate;
                            }
                        }
                        else if (dtoAttributeDefinition.DataType == typeof(DateTime))
                        {
                            dataTimeWrapper = new DateTimeWrapper(attributeTreeViewItem, dtoAttributeDefinition);
                            if (Resources.Contains("DateTimeTextBoxTemplate"))
                            {
                                dataTemplate = Resources["DateTimeTextBoxTemplate"] as DataTemplate;
                            }
                        }
                        else if (dtoAttributeDefinition.DataType == typeof(double))
                        {
                            doubleWrapper = new DoubleWrapper(attributeTreeViewItem, dtoAttributeDefinition);
                            if (Resources.Contains("DoubleTextBoxTemplate"))
                            {
                                dataTemplate = Resources["DoubleTextBoxTemplate"] as DataTemplate;
                            }
                        }
                        else if (dtoAttributeDefinition.DataType == typeof(Int32) && dtoAttributeDefinition.EnumDefinition == null)
                        {
                            intWrapper = new IntWrapper(attributeTreeViewItem, dtoAttributeDefinition);
                            if (Resources.Contains("Int32TextBoxTemplate"))
                            {
                                dataTemplate = Resources["Int32TextBoxTemplate"] as DataTemplate;
                            }
                        }
                        else if (dtoAttributeDefinition.DataType == typeof(bool) && dtoAttributeDefinition.EnumDefinition == null)
                        {
                            if (Resources.Contains("BooleanTemplate"))
                            {
                                dataTemplate = Resources["BooleanTemplate"] as DataTemplate;
                            }
                        }
                        else if (dtoAttributeDefinition.EnumDefinition != null)
                        {
                            dtoAttributDefinitionWrapper = new DtoAttributDefinitionWrapper(attributeTreeViewItem, dtoAttributeDefinition);
                            if (Resources.Contains("EnumComboBoxTemplate"))
                            {
                                dataTemplate = Resources["EnumComboBoxTemplate"] as DataTemplate;
                            }
                        }
                        else
                        {
#if DEBUG
                            Trace.WriteLine("FillTreeView - Unexpected branch");
#endif
                        }

                        attributeTreeViewItem.DataContext = dtoAttributeDefinition;

                        // Read the DtoAttributDefinition properties.
                        definitionValue = ExtensionsClass.DtoAttributDefinitionToString(dtoAttributeDefinition, _dtoAttributDefinitionProperties);
                    }

                    if (dtoAttributDefinitionWrapper != null)
                        attributeTreeViewItem.Header = dtoAttributDefinitionWrapper;
                    else if (doubleWrapper != null)
                        attributeTreeViewItem.Header = doubleWrapper;
                    else if (intWrapper != null)
                        attributeTreeViewItem.Header = intWrapper;
                    else if (dataTimeWrapper != null)
                        attributeTreeViewItem.Header = dataTimeWrapper;
                    else
                        attributeTreeViewItem.Header = dtoAttributeDefinition;

                    attributeTreeViewItem.HeaderTemplate = dataTemplate;

                    if (!string.IsNullOrEmpty(definitionValue))
                        attributeTreeViewItem.ToolTip = definitionValue;

                    if ((dtoAttributeDefinition != null && dtoAttributeDefinition.IsInternal != true) || dtoAttributeDefinition == null)
                    {
                        groupTreeViewItem.Items.Add(attributeTreeViewItem);
                        _treeViewItems.Add(attributeTreeViewItem);
                    }
                }
            }

            ExtensionsClass.ExpandTreeViewItem(rootTreeViewItem);
            AttributeTreeView.InvalidateVisual();

            ScrollViewer scrollViewer = ExtensionsClass.GetTypeObject(AttributeTreeView, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
                scrollViewer.ScrollToHome();

            AttributeTreeView.Focus();

            // Preparation of the sum of the loaded DataTemplates.
            _dataTemplatesCount = 0;
            _notStringHeaders = _treeViewItems.Where(tvi => (tvi.Header != null && tvi.Header.GetType() != typeof(string))).ToList();
            _notStringHeadersCount = _notStringHeaders.Count;
        }

        /// <summary>
        /// Finds the modified attributes and stores it.
        /// </summary>
        /// <param name="dtObject"></param>
        private void GetChangedAttibutes(DtObject dtObject)
        {
            Dictionary<string, DtoAttributesGroup> orgAttributes = null;
            if (_orgObjectsValues.ContainsKey(dtObject))
            {
                orgAttributes = _orgObjectsValues[dtObject];
            }

            Dictionary<DtoAttributDefinition, object> changedAttributes = new Dictionary<DtoAttributDefinition, object>();

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in dtObject.AttributeGroups)
            {
                DtoAttributesGroup orgGroup = null;
                if (orgAttributes != null)
                {
                    (orgAttributes as Dictionary<string, DtoAttributesGroup>).TryGetValue(kvpGroup.Key, out orgGroup);
                }

                foreach (KeyValuePair<string, object> kvpAttribute in kvpGroup.Value)
                {
                    object orgValue = null;

                    DtoAttributDefinition orgDefinition = null;

                    if (orgGroup != null)
                    {
                        orgGroup.TryGetValue(kvpAttribute.Key, out orgValue);
                        orgDefinition = orgValue as DtoAttributDefinition;
                    }

                    DtoAttributDefinition actDefinition = kvpAttribute.Value as DtoAttributDefinition;

                    if (orgDefinition != null && actDefinition != null)
                    {
                        if (orgDefinition.Value != null && orgDefinition.Value is long)
                            orgDefinition.Value = Convert.ToInt32(orgDefinition.Value);

                        bool isEqual = IsEqualValue(actDefinition, orgDefinition);
                        if (!isEqual)
                        {
                            if (!changedAttributes.ContainsKey(actDefinition))
                                changedAttributes.Add(actDefinition, actDefinition.Value);
                            else
                            {
                                changedAttributes.Remove(actDefinition);
                                changedAttributes.Add(actDefinition, actDefinition.Value);
                            }
                        }
                    }
                    else
                    {
#if DEBUG
                        Trace.WriteLine("GetChangedAttibutes - Unexpected branch");
#endif
                    }
                }
            }

            int updateCount = 0;

            if (changedAttributes != null && changedAttributes.Count > 0)
            {
                bool updateErrorMessage = false;

                ProgressWindow.Text = "Save changed attibutes.";
                ProgressWindow.Show();
                try
                {
                    using (new TraceCodeTime("SetAttributeValues", "GetChangedAttibutes"))
                    {
                        foreach (KeyValuePair<DtoAttributDefinition, object> kvp in changedAttributes)
                        {
                            Dictionary<Guid, object> values = new Dictionary<Guid, object>
                            {
                                { dtObject.Id, kvp.Value }
                            };

                            HttpStatusCode updateResult = _integrationBase.ApiCore.DtObjects
                                .UpdateAttributeValues(_integrationBase.CurrentProject.Id, kvp.Key.Id, values);

                            if (updateResult == HttpStatusCode.OK)
                                updateCount++;
                            else
                                updateErrorMessage = true;
                        }

                        if (updateCount == changedAttributes.Count)
                        {
                            if (_orgObjectsValues.ContainsKey(dtObject))
                            {
                                Dictionary<string, DtoAttributesGroup> attributeGroups = dtObject.AttributeGroups as Dictionary<string, DtoAttributesGroup>;
                                _orgObjectsValues[dtObject] = CloneAttributeGroups(attributeGroups);
                            }
                            if (changedAttributes != null)
                                changedAttributes.Clear();
                        }
                    }
                }
                finally
                {
                    ProgressWindow.Hide();
                }

                if (updateErrorMessage)
                    MessageBoxHelper.ShowInformation("Not all attributes could be changed.", _parentWindow);
            }

            ChangesSaved = true;
        }

        /// <summary>
        /// Has the value of an attribute changed.
        /// </summary>
        /// <param name="actDefinition"></param>
        /// <param name="orgDefinition"></param>
        /// <returns>Returns true if the value has changed.</returns>
        private bool IsEqualValue(DtoAttributDefinition actDefinition, DtoAttributDefinition orgDefinition)
        {
            if (actDefinition.IsChangeable == false)
                return true;

            bool isEqual = false;

            if (actDefinition.Value == null && orgDefinition.Value == null)
                return true;

            if (actDefinition.Value == null && orgDefinition.Value != null || actDefinition.Value != null && orgDefinition.Value == null)
                return false;

            Type type = actDefinition.DataType;

            if (type == typeof(Int32))
            {
                isEqual = (int)actDefinition.Value == (int)orgDefinition.Value;
            }
            else if (type == typeof(bool))
            {
                if (actDefinition.EnumDefinition != null)
                    isEqual = (int)actDefinition.Value == (int)orgDefinition.Value;
                else
                    isEqual = (bool)actDefinition.Value == (bool)orgDefinition.Value; ;
            }
            else if (type == typeof(double))
            {
                isEqual = (double)actDefinition.Value == (double)orgDefinition.Value;
            }
            else if (type == typeof(Guid))
            {
                Type valueTyp = actDefinition.Value.GetType();
                if (valueTyp == typeof(Guid))
                {
                    isEqual = ((Guid)actDefinition.Value).Equals((Guid)orgDefinition.Value);
                }
                else
                {
                    isEqual = actDefinition.Value.ToString() == orgDefinition.Value.ToString();
                }
            }
            else if (type == typeof(DateTime))
            {
                isEqual = ((DateTime)actDefinition.Value).Equals((DateTime)orgDefinition.Value);
            }
            else
                return actDefinition.Value == orgDefinition.Value;

            return isEqual;
        }

        #endregion private methods

        #region events

        private void GetObjects_Click(object sender, RoutedEventArgs e)
        {
            SaveChangedAttributes();

            if (SelectedItem != null)
            {
                _objects = GetObjectData(SelectedItem);
                if (_objects == null | _objects.Count == 0)
                    return;

                _orgObjectsValues = GetOrgAttributeValues(_objects);

                List<DataGridData> dataGridData = new List<DataGridData>();

                foreach (DtObject dtObject in _objects)
                {
                    DataGridData data = new DataGridData() { Id = dtObject.Id };

                    string name = dtObject.GetDtObjectName();
                    data.Name = name;

                    if (dtObject.AttributeGroups != null)
                    {
                        foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in dtObject.AttributeGroups)
                        {
                            DtoAttributesGroup group = kvpGroup.Value;

                            foreach (KeyValuePair<string, object> kvp in group)
                            {
                                DtoAttributDefinition definition = kvp.Value as DtoAttributDefinition;
                                if (definition != null)
                                {
                                    if (kvp.Key.ToUpper() == "MODEL")
                                    {
                                        data.Model = definition.Value as string;
                                        continue;
                                    }
                                    else if (kvp.Key.ToUpper() == "LAYER")
                                    {
                                        data.Layer = definition.Value as string;
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    else
                        data.Name = "Unkown";

                    dataGridData.Add(data);
                }

                DataGridDataList = dataGridData;

                if (dataGridData != null && dataGridData.Count > 0)
                    ObjectsDataGrid.SelectedIndex = 0;
            }
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

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool && (bool)e.NewValue == false)
                SaveChangedAttributes();    
        }

        #endregion events

        #region inner classes

        /// <summary>
        /// Properties for the DataGrid objects.
        /// </summary>
        public class DataGridData
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Model { get; set; }
            public string Layer { get; set; }
        }

        internal abstract class ValueWrappper : INotifyPropertyChanged
        {
            private ValueWrappper()
            {
            }

            public ValueWrappper(TreeViewItem treeViewItem)
            {
                TreeViewItem = treeViewItem;
            }

            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged;

            protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion INotifyPropertyChanged

            public TreeViewItem TreeViewItem { get; set; }

            public DtoAttributDefinition DtoAttributDefinition;

            private string _unit;

            public string Unit
            {
                get { return _unit; }

                set { _unit = value; NotifyPropertyChanged(); }
            }
        }

        internal class DtoAttributDefinitionWrapper : ValueWrappper
        {

            public DtoAttributDefinitionWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) : base (treeViewItem)
            {
                DtoAttributDefinition = dtoAttributDefinition;

                Name = DtoAttributDefinition.Name;
                IsChangeable = DtoAttributDefinition.IsChangeable;

                JObject jObject = DtoAttributDefinition.EnumDefinition as JObject;
                if (jObject != null)
                {
                    _jObjectAttributes = JsonConvert.DeserializeObject(jObject.ToString(), typeof(Dictionary<object, string>)) as Dictionary<object, string>;
                    if (_jObjectAttributes != null)
                    {
                        Items = _jObjectAttributes.Values.ToList();
                        if (DtoAttributDefinition.Value != null)
                        {
                            object value = DtoAttributDefinition.Value;
                            Type type = value.GetType();

                            if (type == typeof(Int32) || type == typeof(Int64))
                            {
                                value = value.ToString();

                                if (_jObjectAttributes.ContainsKey(value))
                                    Value = _jObjectAttributes[value];
                            }
                            else if (type == typeof(bool))
                            {
                                bool boolValue = (bool)value;

                                Value = _jObjectAttributes.Values.FirstOrDefault(v => boolValue.ToString().ToUpper() == v.ToUpper());
                            }
                            else if (type == typeof(string))
                            {
                                if (DtoAttributDefinition.DataType == typeof(string))
                                    Value = value;
                                else if (DtoAttributDefinition.DataType == typeof(Guid))
                                {
                                    string stringValue = value as string;
                                    Guid guid;
                                    if (Guid.TryParse(stringValue, out guid))
                                    {
                                        if (_jObjectAttributes.ContainsKey(stringValue))
                                        {
                                            Value = _jObjectAttributes[stringValue];
                                        }
                                    }
                                }
                            }
                            else if (type == typeof(Guid))
                            {
                                Value = value;
                            }
                            else
                            {
#if DEBUG
                                Trace.WriteLine("DtoAttributDefinitionWrapper - Unexpected branch");
#endif
                            }
                        }
                    }
                }
                else
                {
                    JArray jArray = DtoAttributDefinition.EnumDefinition as JArray;
                    if (jArray != null)
                    {
                        _jArrayAttributes = JsonConvert.DeserializeObject(jArray.ToString(), typeof(List<string>)) as List<string>;
                        Items = _jArrayAttributes;
                        Value = DtoAttributDefinition.Value;
                    }
                }

                Unit = dtoAttributDefinition.Unit;
            }

            #region private member

            private Dictionary<object, string> _jObjectAttributes;
            private List<string> _jArrayAttributes;

            #endregion private member

            #region properties

            private List<string> _items;

            public List<string> Items
            {
                get { return _items; }

                set { _items = value; NotifyPropertyChanged(); }
            }

            private object _value;

            public object Value
            {
                get { return _value; }

                set
                {
                    if (value != _value)
                    {
                        _value = value;
                        if (DtoAttributDefinition.DataType == typeof(bool))
                        {
                            KeyValuePair<object, string> foundKvp = _jObjectAttributes.FirstOrDefault((KeyValuePair<object, string> kvp) => kvp.Value.ToUpper() == _value as string);
                            if (foundKvp.Key != null)
                            {
                                if (DtoAttributDefinition.EnumDefinition != null)
                                {
                                    Int32 intValue;
                                    if (int.TryParse(foundKvp.Key as string, out intValue))
                                    {
                                        if (DtoAttributDefinition.EnumDefinition != null)
                                            DtoAttributDefinition.Value = intValue;// != 0;
                                    }
                                }
                                else
                                    DtoAttributDefinition.Value = _value;
                            }
                            else
                                DtoAttributDefinition.Value = null;
                        }
                        else if (DtoAttributDefinition.DataType == typeof(Int32) || DtoAttributDefinition.DataType == typeof(Int64))
                        {
                            string stringValue = _value as string;
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                KeyValuePair<object, string> foundKvp = _jObjectAttributes.FirstOrDefault((KeyValuePair<object, string> kvp) => kvp.Value == stringValue);
                                if (foundKvp.Key != null)
                                {
                                    Int32 intValue;
                                    if (int.TryParse(foundKvp.Key as string, out intValue))
                                    {
                                        DtoAttributDefinition.Value = intValue;
                                    }
                                }
                            }
                            else
                                DtoAttributDefinition.Value = null;
                        }
                        else if (DtoAttributDefinition.DataType == typeof(string))
                        {
                            DtoAttributDefinition.Value = value;
                        }
                        else if (DtoAttributDefinition.DataType == typeof(Guid))
                        {
                            string stringValue = value as string;
                            Guid guid;
                            if (Guid.TryParse(stringValue, out guid))
                            {
                                KeyValuePair<object, string> foundKvp = _jObjectAttributes.FirstOrDefault((KeyValuePair<object, string> kvp) => kvp.Value == stringValue);
                                if (foundKvp.Key != null)
                                {
                                    DtoAttributDefinition.Value = foundKvp.Key;
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            Trace.WriteLine("DtoAttributDefinitionWrapper.Value - Unexpected branch");
#endif
                        }
                        NotifyPropertyChanged();
                    }
                }
            }

            private string _name;

            public string Name
            {
                get { return _name; }

                set { _name = value; NotifyPropertyChanged(); }
            }

            private bool _isChangeable;

            public bool IsChangeable
            {
                get { return _isChangeable; }
                set { _isChangeable = value; NotifyPropertyChanged(); }
            }

            #endregion properties

        }

        internal class DoubleWrapper : ValueWrappper
        {
            public DoubleWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) : base(treeViewItem)
            {
                DtoAttributDefinition = dtoAttributDefinition;
                if (DtoAttributDefinition.Value == null)
                    Double = null;
                else
                {
                    if (DtoAttributDefinition.Value is double)
                    {
                       Double = (double)dtoAttributDefinition.Value;
                    }
                }

                Name = dtoAttributDefinition.Name;
                IsChangeable = dtoAttributDefinition.IsChangeable;
                Unit = dtoAttributDefinition.Unit;
            }

            #region properties

            private double? _double;

            public double? Double
            {
                get { return _double; }

                set
                {
                    if (_double != value)
                    {
                        _double = value;
                        DtoAttributDefinition.Value = value;
                        NotifyPropertyChanged();
                    }
                }
            }

            private string _name;

            public string Name
            {
                get { return _name; }

                set { _name = value; NotifyPropertyChanged(); }
            }

            private bool _isChangeable;

            public bool IsChangeable
            {
                get { return _isChangeable; }
                set { _isChangeable = value; NotifyPropertyChanged(); }
            }

            #endregion
        }

        internal class IntWrapper : ValueWrappper
        {
            public IntWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) : base(treeViewItem)
            {
                DtoAttributDefinition = dtoAttributDefinition;
                if (DtoAttributDefinition.Value == null)
                    Int = null;
                else
                {
                    if (DtoAttributDefinition.Value is int)
                    {
                        Int = (int)dtoAttributDefinition.Value;
                    }
                }

                Name = dtoAttributDefinition.Name;
                IsChangeable = dtoAttributDefinition.IsChangeable;
                Unit = dtoAttributDefinition.Unit;
            }

            #region properties

            private int? _int;

            public int? Int
            {
                get { return _int; }

                set
                {
                    if (_int != value)
                    {
                        _int = value;
                        DtoAttributDefinition.Value = value;
                        NotifyPropertyChanged();
                    }
                }
            }

            private string _name;

            public string Name
            {
                get { return _name; }

                set { _name = value; NotifyPropertyChanged(); }
            }

            private bool _isChangeable;

            public bool IsChangeable
            {
                get { return _isChangeable; }
                set { _isChangeable = value; NotifyPropertyChanged(); }
            }


            #endregion
        }

        internal class DateTimeWrapper : ValueWrappper
        {
            public DateTimeWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) : base(treeViewItem)
            {
                DtoAttributDefinition = dtoAttributDefinition;
                if (DtoAttributDefinition.Value == null)
                    DateTime = null;
                else
                {
                    if (DtoAttributDefinition.Value is DateTime)
                    {
                        DateTime = (DateTime)dtoAttributDefinition.Value;
                    }
                }

                Name = dtoAttributDefinition.Name;
                IsChangeable = dtoAttributDefinition.IsChangeable;
                Unit = dtoAttributDefinition.Unit;
            }

            #region properties

            private DateTime? _dateTime;

            public DateTime? DateTime
            {
                get { return _dateTime; }

                set
                {
                    if (_dateTime != value)
                    {
                        _dateTime = value;
                        DtoAttributDefinition.Value = value;
                        NotifyPropertyChanged();
                    }
                }
            }

            private string _name;

            public string Name
            {
                get { return _name; }

                set { _name = value; NotifyPropertyChanged(); }
            }

            private bool _isChangeable;

            public bool IsChangeable
            {
                get { return _isChangeable; }
                set { _isChangeable = value; NotifyPropertyChanged(); }
            }

            #endregion
        }

        #endregion inner classes

        #region UserControl events

        private void AttributeTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                // Test.
                TreeViewItem treeViewItem = e.NewValue as TreeViewItem;
            }
        }

        private void BaseAttributesUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ObjectTypesList.Focus();
        }

        private void EnumComboBoxTemplate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ComboBox comboBox = sender as ComboBox;
                DtoAttributDefinitionWrapper wrapper = comboBox.DataContext as DtoAttributDefinitionWrapper;
                if (wrapper != null && comboBox != null)
                {
                    bool canSetToNull = true;
                    JObject jObjectDefinition = wrapper.DtoAttributDefinition.EnumDefinition as JObject;
                    if (jObjectDefinition != null)
                    {
                        Dictionary<object, string> jObjectAttributes = JsonConvert.DeserializeObject(jObjectDefinition.ToString(), typeof(Dictionary<object, string>)) as Dictionary<object, string>;
                        if (jObjectAttributes != null)
                        {
                            KeyValuePair<object, string> kvp = jObjectAttributes.FirstOrDefault();
                            if (kvp.Key != null && kvp.Key is string)
                            {
                                string key = kvp.Key as string;
                                Guid guid = Guid.Empty;
                                if (Guid.TryParse(key, out guid))
                                {
                                    canSetToNull = false;
                                }
                            }
                        }
                    }

                    if (canSetToNull)
                    {
                        JArray jArrayDefinition = wrapper.DtoAttributDefinition.EnumDefinition as JArray;
                        if (jArrayDefinition != null)
                        {
                            object[] array = JsonConvert.DeserializeObject(jArrayDefinition.ToString(), typeof(object[])) as object[];
                            if (array != null)
                                canSetToNull = false;
                        }
                    }

                    if (canSetToNull)
                        comboBox.SelectedItem = null;
                }
            }
        }

        private void CheckBox_KeyDown(object sender, KeyEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if (e.Key == Key.Delete)
                    checkBox.IsChecked = null;
            }
        }

        private void AttributeTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                TreeView treeView = sender as TreeView;
                if (treeView != null)
                {
                    object dataContext = (e.OriginalSource as FrameworkElement).DataContext;

                    if (dataContext != null)
                    {
                        TreeViewItem treeViewItem = _treeViewItems.FirstOrDefault(tvi => tvi.DataContext == dataContext);
                        if (treeViewItem == null)
                        {
                            if (dataContext is ValueWrappper)
                            {
                                List<TreeViewItem> wrapperItems = _treeViewItems.Where(tvi => tvi.Header is ValueWrappper).ToList();
                                treeViewItem = wrapperItems.FirstOrDefault(tvi => tvi.Header as ValueWrappper == dataContext);
                            }
                        }

                        if (treeViewItem != null)
                        {
                            int index = _treeViewItems.IndexOf(treeViewItem);

                            if (index > -1)
                            {
                                for (int i = index + 1; i < _treeViewItems.Count; i++)
                                {
                                    treeViewItem = _treeViewItems[i];
                                    if (treeViewItem.DataContext is DtoAttributDefinition)
                                    {
                                        // Getting the ContentPresenter.
                                        ContentPresenter contentPresenter = ExtensionsClass.FindVisualChild<ContentPresenter>(treeViewItem);
                                        _lastFocusedControl = SetEditControl(contentPresenter, treeViewItem);

                                        if (_lastFocusedControl != null)
                                        {
                                            e.Handled = true;
                                            return;
                                        }
                                    }
                                }

                                // Focus the next control.
                                ObjectTypesList.Focus();
                            }
                        }
                    }
                }
            }
        }

        private Control GetControl(ContentPresenter contentPresenter, TreeViewItem treeViewItem)
        {
            Control result = null;

            DtoAttributDefinition dtoAttributDefinition = treeViewItem.DataContext as DtoAttributDefinition;
            if (dtoAttributDefinition != null)
            {
                if (dtoAttributDefinition.IsChangeable == true && !dtoAttributDefinition.IsInternal)
                {
                    DataTemplate dataTemplate = contentPresenter.ContentTemplate;
                    if (dataTemplate != null)
                    {
                        Control control = dataTemplate.FindName("TextBox", contentPresenter) as Control;
                        TextBox textBox = control as TextBox;
                        if (textBox != null)
                        {
                            return textBox;
                        }

                        control = dataTemplate.FindName("DateTimeTextBox", contentPresenter) as Control;
                        DateTimeTextBox dateTimeTextBox = control as DateTimeTextBox;
                        if (dateTimeTextBox != null)
                        {
                            return dateTimeTextBox;
                        }

                        control = dataTemplate.FindName("DoubleTextBox", contentPresenter) as Control;
                        DoubleTextBox doubleTextBox = control as DoubleTextBox;
                        if (doubleTextBox != null)
                        {
                            return doubleTextBox;
                        }

                        control = dataTemplate.FindName("IntegerTextBox", contentPresenter) as Control;
                        IntegerTextBox integerTextBox = control as IntegerTextBox;
                        if (integerTextBox != null)
                        {
                            return integerTextBox;
                        }

                        control = dataTemplate.FindName("CheckBox", contentPresenter) as Control;
                        CheckBox checkBox = control as CheckBox;
                        if (checkBox != null)
                        {
                            return checkBox;
                        }

                        control = dataTemplate.FindName("EnumComboBox", contentPresenter) as Control;
                        ComboBox comboBox = control as ComboBox;
                        if (comboBox != null)
                        {
                            return comboBox;
                        }
                    }
                }
            }
            return result;
        }

        private Control SetEditControl(ContentPresenter contentPresenter, TreeViewItem treeViewItem)
        {
            Control result = null;

            if (contentPresenter != null)
            {
                Control control = GetControl(contentPresenter, treeViewItem);
                if (control != null)
                {
                    control.Focus();
                    return control;
                }
            }

            return result;
        }

        #endregion UserControl events

        #region DataTemplates loaded

        private void DataTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            _dataTemplatesCount++;

            if (_dataTemplatesCount == _notStringHeadersCount)
            {
                for (int i = 0; i < _treeViewItems.Count; i++)
                {
                    TreeViewItem treeViewItem = _treeViewItems[i];
                    // Getting the ContentPresenter.
                    ContentPresenter contentPresenter = ExtensionsClass.FindVisualChild<ContentPresenter>(treeViewItem);
                    _lastFocusedControl = SetEditControl(contentPresenter, treeViewItem);

                    if (_lastFocusedControl != null)
                    {
                        break;
                    }
                }

                // Set GotFocus event.
                for (int i = 0; i < _notStringHeaders.Count; i++)
                {
                    TreeViewItem treeViewItem = _notStringHeaders[i];
                    ContentPresenter contentPresenter = ExtensionsClass.FindVisualChild<ContentPresenter>(treeViewItem);
                    Control control = GetControl(contentPresenter, treeViewItem);
                    if (control != null)
                    {
                        _focusableControls.Add(control);
                        control.GotFocus += Control_GotFocus;
                    }
                }
            }
        }

        private void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            _lastFocusedControl = sender as Control;
        }

        #endregion DataTemplates loaded
    }
}
