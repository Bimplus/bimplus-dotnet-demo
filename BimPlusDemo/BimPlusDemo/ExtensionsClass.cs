using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.IfcData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BimPlusDemo
{
    public static class ExtensionsClass
    {
        #region public methods

        public static string GetDivisonNames(DtoProject project, IntegrationBase integrationBase)
        {
            string result = "";

            List<DtoDivision> divisionList = integrationBase.ApiCore.Projects.GetDivisions(project.Id);

            if (divisionList != null && divisionList.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < divisionList.Count; i++)
                {
                    DtoDivision division = divisionList[i];
                    sb.Append(division.Name);
                    if (sb.Length > 0 && i < divisionList.Count - 1)
                        sb.Append("\r\n");

                    if (divisionList.Count > 6 && i == 5)
                    {
                        sb.Append("...");
                        break;
                    }
                }

                result = sb.ToString();
            }

            return result;
        }

        //public static Dictionary<DbLayer, Dictionary<Type, List<DtObject>>> ReadLayerTypeObjectsDictionary(IntegrationBase integrationBase)
        //{
        //    string caption = "Read layer.";
        //    ProgressWindow progressWindow = new ProgressWindow(null);
        //    progressWindow.Title = caption;

        //    progressWindow.Show();

        //    Dictionary<DbLayer, Dictionary<Type, List<DtObject>>> layerTypeObjectsDictionary = null;
        //    try
        //    {
        //        layerTypeObjectsDictionary = GetLayerTypeObjectsDictionary(integrationBase);
        //    }
        //    finally
        //    {
        //        progressWindow.Hide();
        //        progressWindow = null;
        //    }

        //    return layerTypeObjectsDictionary;
        //}

        public static List<Type> GetTypes(Dictionary<DbLayer, Dictionary<Type, List<DtObject>>> layerTypeObjectsDictionary, Window parent)
        {
            List<Type> types = new List<Type>();

            foreach (Dictionary<Type, List<DtObject>> dictionary in layerTypeObjectsDictionary.Values)
            {
                foreach (KeyValuePair<Type, List<DtObject>> kvp in dictionary)
                {
                    types.Add(kvp.Key);
                }
            }

            types = types.OrderBy(t => t.Name).ToList();
            if (types.Count == 0)
            {
                MessageBox.Show("No objects found" ); //parent
                return types;
            }
            else
                return types;
        }

        public static DependencyObject GetTypeObject(DependencyObject dependencyObject, Type type)
        {
            if (dependencyObject != null && type != null)
            {
                if (dependencyObject.GetType() == type)
                {
                    return dependencyObject;
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    DependencyObject result = GetTypeObject(child, type);

                    if (result == null)
                    {
                        continue;
                    }
                    else
                    {
                        return result as DependencyObject;
                    }
                }
            }

            return null;
        }

        public static string DtoAttributDefinitionToString(DtoAttributDefinition definition, List<PropertyInfo> dtoAttributDefinitionProperties)
        {
            string result = "";

            StringBuilder stringBuilder = new StringBuilder();
            if (definition != null)
            {
                stringBuilder.AppendLine("DtoAttributDefinition:");
                stringBuilder.AppendLine("");
                for (int i = 0; i < dtoAttributDefinitionProperties.Count; i++)
                {
                    PropertyInfo propertyInfo = dtoAttributDefinitionProperties[i];
                    object propertyValue = propertyInfo.GetValue(definition);
                    if (propertyValue != null)
                    {
                        string output = "";
                        if (propertyInfo.Name != "Value")
                            output = $"{propertyInfo.Name} : {propertyValue.ToString()}";
                        else
                        {
                            if (definition.EnumDefinition == null)
                                output = $"{propertyInfo.Name} : {propertyValue?.ToString() ?? "null"}";
                            else
                                output = $"{propertyInfo.Name} : {GetEnumDefinitionValue(definition)}";
                        }

                        stringBuilder.AppendLine(output);
                    }
                }
            }

            result = stringBuilder.ToString();
            return result;
        }

        public static object GetEnumDefinitionValue(DtoAttributDefinition dtoAttributDefinition)
        {
            object result = null;

            if (!(dtoAttributDefinition.EnumDefinition is JObject jObject))
            {
                if (dtoAttributDefinition.EnumDefinition is JArray jArray)
                {
                    List<string> jArrayAttributes =
                        JsonConvert.DeserializeObject(jArray.ToString(), typeof(List<string>)) as List<string>;
                    result = dtoAttributDefinition.Value;
                }
            }
            else
            {
                if (JsonConvert.DeserializeObject(jObject.ToString(), typeof(Dictionary<object, string>)) is Dictionary<object, string> jObjectAttributes)
                {
                    if (dtoAttributDefinition.Value != null)
                    {
                        object value = dtoAttributDefinition.Value;
                        Type type = value.GetType();

                        if (type == typeof(Int32) || type == typeof(Int64))
                        {
                            value = value.ToString();

                            if (jObjectAttributes.ContainsKey(value))
                                result = jObjectAttributes[value];
                        }
                        else if (type == typeof(bool))
                        {
                            bool boolValue = (bool) value;

                            result = jObjectAttributes.Values.FirstOrDefault(v =>
                                boolValue.ToString().ToUpper() == v.ToUpper());
                        }
                        else if (type == typeof(string))
                        {
                            if (dtoAttributDefinition.DataType == typeof(string))
                                result = value;
                            else if (dtoAttributDefinition.DataType == typeof(Guid))
                            {
                                string stringValue = value as string;
                                if (Guid.TryParse(stringValue, out var guid))
                                {
                                    if (jObjectAttributes.ContainsKey(stringValue))
                                    {
                                        result = jObjectAttributes[stringValue];
                                    }
                                }
                            }
                        }
                        else if (type == typeof(Guid))
                        {
                            result = value;
                        }
                        else
                        {
#if DEBUG
                            Trace.WriteLine("GetEnumDefinitionValue - Unexpected branch");
#endif
                        }
                    }
                }
            }

            return result ?? (result = "");
        }

        public static void ExpandTreeViewItem(TreeViewItem treeViewItem)
        {
            treeViewItem.IsExpanded = true;

            ItemCollection items = treeViewItem.Items;

            foreach (TreeViewItem item in items)
            {
                item.IsExpanded = true;
                ExpandTreeViewItem(item);
            }
        }

        /// <summary>
        /// FindVisualChild.
        /// </summary>
        /// <typeparam name="ChildItem"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static ChildItem FindVisualChild<ChildItem>(DependencyObject obj) where ChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildItem)
                    return (ChildItem)child;
                else
                {
                    ChildItem childOfChild = FindVisualChild<ChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }

        #endregion public methods

        #region private methods

        //private static Dictionary<DbLayer, Dictionary<Type, List<DtObject>>> GetLayerTypeObjectsDictionary(IntegrationBase integrationBase)
        //{
        //    DtoProject project = integrationBase.ApiCore.Projects.GetDtoProject(integrationBase.CurrentProject.Id);

        //    Dictionary<DbLayer, Dictionary<Type, List<DtObject>>> layerTypeObjectsDictionary = new Dictionary<DbLayer, Dictionary<Type, List<DtObject>>>();

        //    List<DtoProjectDiscipline> disciplines = new List<DtoProjectDiscipline>();

        //    try
        //    {
        //        if (project.Disciplines != null && project.Disciplines.FirstOrDefault() != null)
        //        {
        //            foreach (DtoProjectDiscipline discipline in project.Disciplines)
        //            {
        //                if (disciplines.FirstOrDefault(d => d.DisciplineId == discipline.DisciplineId) == null)
        //                    disciplines.Add(discipline);
        //            }
        //        }

        //        List<DtoElementType> existingTypes = new List<DtoElementType>();
        //        Dictionary<Type, List<DtObject>> typeObjectDict = new Dictionary<Type, List<DtObject>>();
        //        layerTypeObjectsDictionary = new Dictionary<DbLayer, Dictionary<Type, List<DtObject>>>();

        //        using (new TraceCodeTime("Load elements", "Import types"))
        //        {
        //            using (new TraceCodeTime("GetProjectElementTypes", "Import types"))
        //            {
        //                existingTypes = integrationBase.ApiCore.Projects.GetProjectElementTypes(integrationBase.CurrentProject.Id);
        //            }

        //            foreach (DtoElementType elementType in existingTypes)
        //            {
        //                Type type = DbObjectList.GetType(elementType.Type);

        //                DbLayer layer = DbLayerHandler.GetLayer(DbObjectList.Discpline(elementType.Id));

        //                if (!layerTypeObjectsDictionary.ContainsKey(layer))
        //                {
        //                    layerTypeObjectsDictionary.Add(layer, new Dictionary<Type, List<DtObject>>());
        //                }

        //                if (!layerTypeObjectsDictionary[layer].ContainsKey(type))
        //                {
        //                    layerTypeObjectsDictionary[layer].Add(type, new List<DtObject>());
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }

        //    return layerTypeObjectsDictionary;
        //}

        #endregion private methods
    }

    public static class DtObjectExtensions
    {
        public static string GetDtObjectName(this DtObject dtObject)
        {
            string name = GetStringProperty(dtObject, TableNames.tabAttribGeneral, "Name");
            return name != null ? name : GetStringProperty(dtObject, TableNames.tabAttribElement, "Name");
        }

        private static string GetStringProperty(this DtObject dtObject, string group, string property)
        {
            DtoAttributesGroup table = PropertyTable(dtObject, group, false);
            if (table == null)
                return null;
            else
                return GetStringProperty(table, property);
        }

        private static string GetStringProperty(DtoAttributesGroup group, string name)
        {
            name = name.ToLowerInvariant();
            Object value;
            group.TryGetValue(name, out value);
            if (value is string)
                return value.ToString();
            else if (value is DtoAttributDefinition)
            {
                DtoAttributDefinition definition = value as DtoAttributDefinition;
                if (definition != null && definition.Value is string)
                    return definition.Value as string;
            }
            return null;
        }

        public static DtoAttributesGroup PropertyTable(DtObject dtObject, string tableName, bool createIfNotExist)
        {
            DtoAttributesGroup group;
            tableName = tableName.ToLowerInvariant();
            dtObject.AttributeGroups.TryGetValue(tableName, out group);

            if (group == null && (tableName == TableNames.tabAttribGeneral.ToLowerInvariant() || tableName == TableNames.tabAttribElement.ToLowerInvariant()))
            {
                string key = dtObject.AttributeGroups.Keys.FirstOrDefault(k => k.IndexOf(tableName) == 0);
                if (!string.IsNullOrEmpty(key))
                    dtObject.AttributeGroups.TryGetValue(key, out group);
            }

            if (group == null && createIfNotExist)
            {
                group = new DtoAttributesGroup();
                dtObject.AttributeGroups.Add(tableName, group);
            }
            return group;
        }
    }

    public static class GetAttributesAllAttribute
    {
        /// <summary>
        /// Get a dictionary of all DtoAttributeTopologies.
        /// </summary>
        /// <param name="integrationBase"></param>
        /// <param name="tenant"></param>
        /// <returns></returns>
        public static Dictionary<Guid, DtoAttributeTopology> ReadAttributeTopology(IntegrationBase integrationBase, bool tenant = false)
        {
            DtoAttributeTopology topology = integrationBase.ApiCore.GetAttributes(tenant);

            Dictionary<Guid, DtoAttributeTopology> dictionary = new Dictionary<Guid, DtoAttributeTopology>();

            WalkToTree(topology, ref dictionary);

            return dictionary;
        }

        private static void WalkToTree(DtoAttributeTopology topology, ref Dictionary<Guid, DtoAttributeTopology> dictionary)
        {
            if (topology != null && topology.Children != null)
            {
                foreach (DtoAttributeTopology child in topology.Children)
                {
                    if (!dictionary.ContainsKey(child.Id))
                        dictionary.Add(child.Id, child);
                    else
                    {
#if DEBUG
                        Trace.WriteLine("WalkToTree - Unexpected branch");
#endif
                    }
                    if (child.Children != null)
                        WalkToTree(child, ref dictionary);
                }
            }
        }
    }
}
