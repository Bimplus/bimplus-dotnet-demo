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
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.Utilities.V2;
using BimPlusDemo.Annotations;
using BimPlusDemo.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for UpdateAttributeValues.xaml
    /// </summary>
    public partial class UpdateAttributeValues : IDisposable, INotifyPropertyChanged
    {
        public UpdateAttributeValues(IntegrationBase integrationBase)
        {
            InitializeComponent();
            DataContext = this;

            Type type = typeof(DtoAttributDefinition);
            _dtoAttributeDefinitionProperties =
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            _focusableControls = new List<Control>();

            _integrationBase = integrationBase;
        }

        public void InitTreeView(Guid objectId)
        {
            DtObject = _integrationBase.ApiCore.DtObjects.GetObject(objectId, ObjectRequestProperties.AttributDefinition);
            _inputProperties = CloneProperties(DtObject);
            FillTreeView(DtObject);
        }

        #region private member
        private readonly IntegrationBase _integrationBase;
        private DtObject DtObject { get; set; }

        private int _dataTemplatesCount;
        private readonly List<PropertyInfo> _dtoAttributeDefinitionProperties;
        private readonly List<TreeViewItem> _treeViewItems = new List<TreeViewItem>();

        private readonly List<Control> _focusableControls;
        private Control _lastFocusedControl;
        private List<TreeViewItem> _notStringHeaders;
        private Dictionary<string, DtoAttributesGroup> _inputProperties;
        #endregion

        #region UserControl events

        private void FillTreeView(DtObject dtObject)
        {
            //UnloadContent();

            AttributeTreeView.Items.Clear();
            _treeViewItems.Clear();

            TreeViewItem rootTreeViewItem = new TreeViewItem {Header = dtObject.GetType().Name};

            AttributeTreeView.Items.Add(rootTreeViewItem);

            _treeViewItems.Add(rootTreeViewItem);

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in dtObject.AttributeGroups)
            {
                var groupTreeViewItem = new TreeViewItem();
                if (dtObject.LocalizedAttributeGroups != null &&
                    dtObject.LocalizedAttributeGroups.ContainsKey(kvpGroup.Key))
                    groupTreeViewItem.Header = dtObject.LocalizedAttributeGroups[kvpGroup.Key];
                else
                    groupTreeViewItem.Header = kvpGroup.Key;

                groupTreeViewItem.Background = null;
                rootTreeViewItem.Items.Add(groupTreeViewItem);

                _treeViewItems.Add(groupTreeViewItem);

                foreach (KeyValuePair<string, object> kvpAttribute in kvpGroup.Value)
                {
                    // Can not set in TreeView.ItemTemplate.
                    var attributeTreeViewItem = new TreeViewItem();

                    //object value = null;

                    DataTemplate dataTemplate = null;
                    DtoAttributDefinitionWrapper dtoAttributeDefinitionWrapper = null;
                    DoubleWrapper doubleWrapper = null;
                    IntWrapper intWrapper = null;
                    DateTimeWrapper dataTimeWrapper = null;

                    DtoAttributDefinition dtoAttributeDefinition = null;
                    if (kvpAttribute.Value == null)
                        continue;
                    if (kvpAttribute.Value is DtoAttributDefinition dto)
                        dtoAttributeDefinition = dto;
                    else if (kvpAttribute.Value is JObject o)
                        dtoAttributeDefinition = o.ToObject<DtoAttributDefinition>();
                    if (dtoAttributeDefinition == null)
                        continue;

                    if ((dtoAttributeDefinition.DataType == typeof(string) ||
                         dtoAttributeDefinition.DataType == typeof(Guid)) &&
                        dtoAttributeDefinition.EnumDefinition == null)
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
                    else if (dtoAttributeDefinition.DataType == typeof(Int32) &&
                             dtoAttributeDefinition.EnumDefinition == null)
                    {
                        intWrapper = new IntWrapper(attributeTreeViewItem, dtoAttributeDefinition);
                        if (Resources.Contains("Int32TextBoxTemplate"))
                        {
                            dataTemplate = Resources["Int32TextBoxTemplate"] as DataTemplate;
                        }
                    }
                    else if (dtoAttributeDefinition.DataType == typeof(bool) &&
                             dtoAttributeDefinition.EnumDefinition == null)
                    {
                        if (Resources.Contains("BooleanTemplate"))
                        {
                            dataTemplate = Resources["BooleanTemplate"] as DataTemplate;
                        }
                    }
                    else if (dtoAttributeDefinition.EnumDefinition != null)
                    {
                        dtoAttributeDefinitionWrapper =
                            new DtoAttributDefinitionWrapper(attributeTreeViewItem, dtoAttributeDefinition);
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
                    var definitionValue = ExtensionsClass.DtoAttributDefinitionToString(dtoAttributeDefinition,
                        _dtoAttributeDefinitionProperties);
                    

                    if (dtoAttributeDefinitionWrapper != null)
                        attributeTreeViewItem.Header = dtoAttributeDefinitionWrapper;
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

                    if ((dtoAttributeDefinition.IsInternal != true))
                    {
                        groupTreeViewItem.Items.Add(attributeTreeViewItem);
                        _treeViewItems.Add(attributeTreeViewItem);
                    }
                }
            }

            ExtensionsClass.ExpandTreeViewItem(rootTreeViewItem);
            AttributeTreeView.InvalidateVisual();

            if (ExtensionsClass.GetTypeObject(AttributeTreeView, typeof(ScrollViewer)) is ScrollViewer scrollViewer)
                scrollViewer.ScrollToHome();

            AttributeTreeView.Focus();

            // Preparation of the sum of the loaded DataTemplates.
            _dataTemplatesCount = 0;
            _notStringHeaders = _treeViewItems
                .Where(tvi => (tvi.Header != null && tvi.Header.GetType() != typeof(string))).ToList();
        }

        private static DtoAttributDefinition GetAttributeDefinitionFromJObject(JObject jObject)
        {
            DtoAttributDefinition definition = null;
            try
            {
                definition = jObject.ToObject<DtoAttributDefinition>();
                if (definition.Value != null)
                {
                    if (definition.DataType == typeof(double))
                    {
                        if (definition.Value is string value)
                        {
                            if (double.TryParse(value, out var doubleValue))
                            {
                                definition.Value = doubleValue;
                            }
                        }
                    }

                    if (definition.DataType == typeof(int))
                    {
                        if (definition.Value is long)
                            definition.Value = Convert.ToInt32(definition.Value);
                    }

                    if (definition.DataType == typeof(bool) && definition.EnumDefinition != null)
                    {
                        if (definition.Value is bool value)
                        {
                            definition.Value = value ? 1 : 0;
                        }
                        else if (definition.Value is long)
                        {
                            definition.Value = Convert.ToInt32(definition.Value);
                        }
                        else if (definition.Value is int definitionValue)
                        {
                            definition.Value = definitionValue;
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

        private static Dictionary<string, DtoAttributesGroup> CloneAttributeGroups(Dictionary<string, DtoAttributesGroup> attributeGroups)
        {
            Dictionary<string, DtoAttributesGroup> clonedGroups = new Dictionary<string, DtoAttributesGroup>();

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in attributeGroups)
            {
                DtoAttributesGroup newGroup = new DtoAttributesGroup();

                foreach (KeyValuePair<string, object> kvp in kvpGroup.Value)
                {
                    if (kvp.Value is DtoAttributDefinition orgDef)
                    {
                        DtoAttributDefinition newDefinition = orgDef.Clone() as DtoAttributDefinition;

                        if (newDefinition?.DataType == typeof(int))
                        {
                            if (newDefinition.Value != null && newDefinition.Value is long)
                                newDefinition.Value = Convert.ToInt32(newDefinition.Value);
                        }

                        newGroup.Add(kvp.Key, newDefinition);
                    }
                    //else
                    //{
                    //    object value = kvp.Value;
                    //}
                }

                clonedGroups.Add(kvpGroup.Key, newGroup);
            }

            return clonedGroups;
        }

        private Dictionary<string, DtoAttributesGroup> CloneProperties(DtObject dtObject)
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

                            if (kvp.Value is JObject jObject)
                            {
                                definition = GetAttributeDefinitionFromJObject(jObject);

                                if (definition != null)
                                {
                                    if (definition.DataType == typeof(int))
                                    {
                                        if (definition.Value is long)
                                            definition.Value = Convert.ToInt32(definition.Value);
                                    }

                                    KeyValuePair<string, object> newKvp = new KeyValuePair<string, object>(kvp.Key, definition);
                                    kvpList.Remove(kvp);
                                    kvpList.Insert(i, newKvp);
                                }
                            }
                        }
                    }

                    // Create DtoAttributesGroup with DtoAttributeDefinition.
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
            return CloneAttributeGroups(attributeGroups);
        }

        private Dictionary<DtoAttributDefinition, object> GetChangedAttributes(DtObject dtObject)
        {
            Dictionary<DtoAttributDefinition, object> changedAttributes =
                new Dictionary<DtoAttributDefinition, object>();

            foreach (KeyValuePair<string, DtoAttributesGroup> kvpGroup in dtObject.AttributeGroups)
            {
                _inputProperties.TryGetValue(kvpGroup.Key, out var orgGroup);

                foreach (KeyValuePair<string, object> kvpAttribute in kvpGroup.Value)
                {
                    DtoAttributDefinition orgDefinition = null;

                    if (orgGroup != null)
                    {
                        orgGroup.TryGetValue(kvpAttribute.Key, out var orgValue);
                        orgDefinition = orgValue as DtoAttributDefinition;
                    }

                    if (orgDefinition != null && kvpAttribute.Value is DtoAttributDefinition actDefinition)
                    {
                        if (orgDefinition.Value != null && orgDefinition.Value is long)
                            orgDefinition.Value = Convert.ToInt32(orgDefinition.Value);

                        if (IsEqualValue(actDefinition, orgDefinition)) continue;
                        if (!changedAttributes.ContainsKey(actDefinition))
                            changedAttributes.Add(actDefinition, actDefinition.Value);
                        else
                        {
                            changedAttributes.Remove(actDefinition);
                            changedAttributes.Add(actDefinition, actDefinition.Value);
                        }}
                    else
                    {
#if DEBUG
                        Trace.WriteLine("GetChangedAttributes - Unexpected branch");
#endif
                    }
                }
            }
            return changedAttributes;
        }


        /// <summary>
        /// Has the value of an attribute changed.
        /// </summary>
        /// <param name="actDefinition"></param>
        /// <param name="orgDefinition"></param>
        /// <returns>Returns true if the value has changed.</returns>
        private bool IsEqualValue(DtoAttributDefinition actDefinition, DtoAttributDefinition orgDefinition)
        {
            double TOLERANCE = 1E-4;
            if (actDefinition.IsChangeable == false)
                return true;

            bool isEqual;


            if (actDefinition.Value == null && orgDefinition.Value != null || actDefinition.Value != null && orgDefinition.Value == null)
                return false;
            if (actDefinition.Value == null || orgDefinition.Value == null)
                return true;

            Type type = actDefinition.DataType;

            if (type == typeof(int))
            {
                isEqual = (int)actDefinition.Value == (int)orgDefinition.Value;
            }
            else if (type == typeof(bool))
            {
                if (actDefinition.EnumDefinition != null)
                    isEqual = (int)actDefinition.Value == (int)orgDefinition.Value;
                else
                    isEqual = (bool)actDefinition.Value == (bool)orgDefinition.Value;
            }
            else if (type == typeof(double))
            {
                isEqual = Math.Abs((double)actDefinition.Value - (double)orgDefinition.Value) < TOLERANCE;
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

        /// <summary>
        /// SaveProperties
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public HttpStatusCode SaveProperties(object sender, RoutedEventArgs e)
        {
            var changedAttributes = GetChangedAttributes(DtObject);
            if (changedAttributes==null || changedAttributes.Count == 0)
                return HttpStatusCode.NotFound;

            //ProgressWindow.Text = "Save changed attributes.";
            //ProgressWindow.Show();
            var update = (DtObject) DbObjectList.Create(DtObject.Elementtyp);
            update.Id = DtObject.Id;
            foreach (KeyValuePair<DtoAttributDefinition, object> set in changedAttributes)
            {
                update.AddProperty(TableNames.contentAttributes, set.Key.Id.ToString(), set.Value);
            }

            return _integrationBase.ApiCore.DtObjects.PutObject(update);
        }

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
                TreeViewItem rootItem = AttributeTreeView.Items[0] as TreeViewItem;
                if (rootItem == null) return;
                foreach (object item in rootItem.Items)
                {
                    if (item is TreeViewItem treeViewItem)
                        treeViewItem.IsExpanded = isExpanded;
                }
            }
        }

        #endregion

        #region UserControl events

        private void EnumComboBoxTemplate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) 
                return;
            ComboBox comboBox = sender as ComboBox;
            if (!(comboBox?.DataContext is DtoAttributDefinitionWrapper wrapper)) 
                return;
            bool canSetToNull = true;
            if (wrapper.DtoAttributDefinition.EnumDefinition is JObject jObjectDefinition)
            {
                Dictionary<object, string> jObjectAttributes = JsonConvert.DeserializeObject(jObjectDefinition.ToString(), typeof(Dictionary<object, string>)) as Dictionary<object, string>;
                if (jObjectAttributes != null)
                {
                    KeyValuePair<object, string> kvp = jObjectAttributes.FirstOrDefault();
                    if (kvp.Key != null && kvp.Key is string key)
                    {
                        if (Guid.TryParse(key, out _))
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

        private void CheckBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) 
                return;
            if (e.Key == Key.Delete)
                checkBox.IsChecked = null;
        }

        private void AttributeTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab || !(sender is TreeView)) 
                return;

            object dataContext = (e.OriginalSource as FrameworkElement)?.DataContext;
            if (dataContext == null) 
                return;

            TreeViewItem treeViewItem = _treeViewItems.FirstOrDefault(tvi => tvi.DataContext == dataContext);
            if (treeViewItem == null)
            {
                if (dataContext is ValueWrapper)
                {
                    List<TreeViewItem> wrapperItems = _treeViewItems.Where(tvi => tvi.Header is ValueWrapper).ToList();
                    treeViewItem = wrapperItems.FirstOrDefault(tvi => tvi.Header as ValueWrapper == dataContext);
                }
            }

            if (treeViewItem == null) 
                return;
            int index = _treeViewItems.IndexOf(treeViewItem);
            if (index <= -1) 
                return;

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
        }

        private Control GetControl(ContentPresenter contentPresenter, TreeViewItem treeViewItem)
        {
            if (!(treeViewItem.DataContext is DtoAttributDefinition dtoAttributDefinition)) 
                return null;
            if (dtoAttributDefinition.IsChangeable != true || dtoAttributDefinition.IsInternal) 
                return null;

            DataTemplate dataTemplate = contentPresenter.ContentTemplate;
            if (dataTemplate != null)
            {
                Control control = dataTemplate.FindName("TextBox", contentPresenter) as Control;
                if (control is TextBox textBox)
                {
                    return textBox;
                }

                control = dataTemplate.FindName("DateTimeTextBox", contentPresenter) as Control;
                if (control is DateTimeTextBox dateTimeTextBox)
                {
                    return dateTimeTextBox;
                }

                control = dataTemplate.FindName("DoubleTextBox", contentPresenter) as Control;
                if (control is DoubleTextBox doubleTextBox)
                {
                    return doubleTextBox;
                }

                control = dataTemplate.FindName("IntegerTextBox", contentPresenter) as Control;
                if (control is IntegerTextBox integerTextBox)
                {
                    return integerTextBox;
                }

                control = dataTemplate.FindName("CheckBox", contentPresenter) as Control;
                if (control is CheckBox checkBox)
                {
                    return checkBox;
                }

                control = dataTemplate.FindName("EnumComboBox", contentPresenter) as Control;
                if (control is ComboBox comboBox)
                {
                    return comboBox;
                }
            }
            return null;
        }

        private Control SetEditControl(ContentPresenter contentPresenter, TreeViewItem treeViewItem)
        {
            if (contentPresenter == null) 
                return null;
            Control control = GetControl(contentPresenter, treeViewItem);
            if (control == null) 
                return null;
            control.Focus();
            return control;
        }

        #endregion UserControl events

        #region DataTemplates loaded

        private void DataTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            _dataTemplatesCount++;
            var ct = _notStringHeaders.Count;
            if (_dataTemplatesCount == ct)
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

        public void Dispose()
        {
            foreach (Control control in _focusableControls)
            {
                control.GotFocus -= Control_GotFocus;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    #region inner classes

    internal abstract class ValueWrapper : INotifyPropertyChanged
    {
        protected ValueWrapper(TreeViewItem treeViewItem)
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
            get => _unit;

            set
            {
                _unit = value;
                NotifyPropertyChanged();
            }
        }
    }

    internal class DtoAttributDefinitionWrapper : ValueWrapper
    {
        public DtoAttributDefinitionWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) :
            base(treeViewItem)
        {
            DtoAttributDefinition = dtoAttributDefinition;

            Name = DtoAttributDefinition.Name;
            IsChangeable = DtoAttributDefinition.IsChangeable;

            if (DtoAttributDefinition.EnumDefinition is JObject jObject)
            {
                _jObjectAttributes =
                    JsonConvert.DeserializeObject(jObject.ToString(), typeof(Dictionary<object, string>)) as
                        Dictionary<object, string>;
                if (_jObjectAttributes != null)
                {
                    Items = _jObjectAttributes.Values.ToList();
                    if (DtoAttributDefinition.Value != null)
                    {
                        object value = DtoAttributDefinition.Value;
                        Type type = value.GetType();

                        if (type == typeof(int) || type == typeof(long))
                        {
                            value = value.ToString();

                            if (_jObjectAttributes.ContainsKey(value))
                                Value = _jObjectAttributes[value];
                        }
                        else if (type == typeof(bool))
                        {
                            bool boolValue = (bool) value;

                            Value = _jObjectAttributes.Values.FirstOrDefault(v =>
                                boolValue.ToString().ToUpper() == v.ToUpper());
                        }
                        else if (type == typeof(string))
                        {
                            if (DtoAttributDefinition.DataType == typeof(string))
                                Value = value;
                            else if (DtoAttributDefinition.DataType == typeof(Guid))
                            {
                                string stringValue = value as string ?? "";
                                if (Guid.TryParse(stringValue, out _))
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
                            Trace.WriteLine("DtoAttributeDefinitionWrapper - Unexpected branch");
#endif
                        }
                    }
                }
            }
            else
            {
                if (DtoAttributDefinition.EnumDefinition is JArray jArray)
                {
                    var jArrayAttributes = JsonConvert.DeserializeObject(jArray.ToString(), typeof(List<string>)) as List<string>;
                    Items = jArrayAttributes;
                    Value = DtoAttributDefinition.Value;
                }
            }
            Unit = dtoAttributDefinition.Unit;
        }

        #region private member

        private readonly Dictionary<object, string> _jObjectAttributes;

        #endregion private member

        #region properties
        public List<string> Items
        {
            get => _items;
            set
            {
                _items = value;
                NotifyPropertyChanged();
            }
        }
        private List<string> _items;


        public object Value
        {
            get { return _value; }
            set
            {
                if (value == _value) 
                    return;
                _value = value;
                if (DtoAttributDefinition.DataType == typeof(bool))
                {
                    KeyValuePair<object, string> foundKvp =
                        _jObjectAttributes.FirstOrDefault(kvp =>
                            kvp.Value.ToUpper() == _value as string);
                    if (foundKvp.Key != null)
                    {
                        if (DtoAttributDefinition.EnumDefinition != null)
                        {
                            if (int.TryParse(foundKvp.Key as string, out var intValue))
                            {
                                if (DtoAttributDefinition.EnumDefinition != null)
                                    DtoAttributDefinition.Value = intValue; // != 0;
                            }
                        }
                        else
                            DtoAttributDefinition.Value = _value;
                    }
                    else
                        DtoAttributDefinition.Value = null;
                }
                else if (DtoAttributDefinition.DataType == typeof(int) ||
                         DtoAttributDefinition.DataType == typeof(long))
                {
                    string stringValue = _value as string;
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        KeyValuePair<object, string> foundKvp =
                            _jObjectAttributes.FirstOrDefault(kvp =>
                                kvp.Value == stringValue);
                        if (foundKvp.Key != null)
                        {
                            if (int.TryParse(foundKvp.Key as string, out var intValue))
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
                    if (Guid.TryParse(stringValue, out var _))
                    {
                        KeyValuePair<object, string> foundKvp =
                            _jObjectAttributes.FirstOrDefault(kvp =>
                                kvp.Value == stringValue);
                        if (foundKvp.Key != null)
                        {
                            DtoAttributDefinition.Value = foundKvp.Key;
                        }
                    }
                }
                else
                {
#if DEBUG
                    Trace.WriteLine("DtoAttributeDefinitionWrapper.Value - Unexpected branch");
#endif
                }
                NotifyPropertyChanged();
            }
        }
        private object _value;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
        private string _name;

        public bool IsChangeable
        {
            get => _isChangeable;
            set
            {
                _isChangeable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isChangeable;

        #endregion properties

    }

    internal class DoubleWrapper : ValueWrapper
    {
        public DoubleWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) : base(
            treeViewItem)
        {
            DtoAttributDefinition = dtoAttributDefinition;
            if (DtoAttributDefinition.Value == null)
                Double = null;
            else
            {
                if (DtoAttributDefinition.Value is double)
                {
                    Double = (double) dtoAttributDefinition.Value;
                }
            }

            Name = dtoAttributDefinition.Name;
            IsChangeable = dtoAttributDefinition.IsChangeable;
            Unit = dtoAttributDefinition.Unit;
        }

        #region properties


        public double? Double
        {
            get => _double;

            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_double == value) 
                    return;
                _double = value;
                DtoAttributDefinition.Value = value;
                NotifyPropertyChanged();
            }
        }
        private double? _double;


        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
        private string _name;


        public bool IsChangeable
        {
            get => _isChangeable;
            set
            {
                _isChangeable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isChangeable;
        #endregion
    }

    internal class IntWrapper : ValueWrapper
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
                    Int = (int) dtoAttributDefinition.Value;
                }
            }

            Name = dtoAttributDefinition.Name;
            IsChangeable = dtoAttributDefinition.IsChangeable;
            Unit = dtoAttributDefinition.Unit;
        }

        #region properties


        public int? Int
        {
            get => _int;

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
        private int? _int;

        public string Name
        {
            get => _name;

            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
        private string _name;

        public bool IsChangeable
        {
            get => _isChangeable;
            set
            {
                _isChangeable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isChangeable;
        #endregion
    }

    internal class DateTimeWrapper : ValueWrapper
    {
        public DateTimeWrapper(TreeViewItem treeViewItem, DtoAttributDefinition dtoAttributDefinition) : base(
            treeViewItem)
        {
            DtoAttributDefinition = dtoAttributDefinition;
            if (DtoAttributDefinition.Value == null)
                DateTime = null;
            else
            {
                if (DtoAttributDefinition.Value is DateTime)
                {
                    DateTime = (DateTime) dtoAttributDefinition.Value;
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
            get => _dateTime;

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
            get => _name;

            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isChangeable;

        public bool IsChangeable
        {
            get => _isChangeable;
            set
            {
                _isChangeable = value;
                NotifyPropertyChanged();
            }
        }
        #endregion
    }

    #endregion inner classes
}
