using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.Content;
using BimPlus.Sdk.Data.TenantDto;
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable StringLiteralTypo

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for Catalogs.xaml
    /// </summary>
    public partial class BimPlusCatalogs : INotifyPropertyChanged
    {
        public BimPlusCatalogs()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CatalogControl_Loaded(object sender, RoutedEventArgs e)
        {
            PropertyChanged += CatalogsControl_PropertyChanged;

            ContentAttributes = _integrationBase.ApiCore.Attributes.GetAll();
        }

        private void CatalogsControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "SelectedForm":
                    {
                        if (!(SelectedForm is CatalogGroup catalog))
                            break;
                        Shapes.Clear();
                        CrossSectionItems.Clear();
                        CrossSectionProperties.Clear();
                        var serviceUrl =
                            $"{_integrationBase.ServerName}/v2/content/crosssectiondefinitions/{catalog.Id}";
                        try
                        {
                            var cd = BimPlus.LightCaseClient.GenericProxies.RestGet<DtoCrossSectionDefinition>(
                                serviceUrl, _integrationBase.ClientConfiguration);
                            if (cd?.Shapes == null || cd.Shapes?.Count == 0)
                                break;
                            foreach (var shape in cd.Shapes)
                            {
                                Shapes.Add(new CatalogGroup
                                {
                                    Id = shape.Id,
                                    Name = shape.Name,
                                    Description = shape.Description
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Get catalogs: {ex.Message}");
                        }
                        break;
                    }
                case "SelectedShape":
                    {
                        if (!(SelectedShape is CatalogGroup catalog))
                            break;
                        var items = _integrationBase.ApiCore.Catalogs.GetAllCatalogItems(catalog.Id);
                        if (items == null) break;
                        CrossSectionProperties.Clear();
                        CrossSectionItems.Clear();
                        foreach (var item in items)
                        {
                            CrossSectionItems.Add(new CatalogGroup
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description
                            });
                        }
                        break;
                    }
                case "SelectedCrossSection":
                    {
                        var item = SelectedCrossSection as CatalogGroup;
                        if (item == null)
                            break;
                        var properties = _integrationBase.ApiCore.Catalogs.GetCatalogItem(item.Id);
                        CrossSectionProperties.Clear();
                        foreach (var property in properties.Properties)
                        {
                            var p = ContentAttributes.Find(x => x.Id == property.Key);
                            if (p == null) break;
                            CrossSectionProperties.Add(new CatalogProperty
                            {
                                Id = property.Key,
                                Name = p.Name,
                                AttributeValue = $"{property.Value} {p.Unit}",
                                Type = p.FreeAttribTypeStr
                            });
                        }
                        break;
                    }
                case "CrossSectionFilter":
                    {
                        var filter = "Type=CrossSection";
                        if (!string.IsNullOrEmpty(CrossSectionNorm))
                            filter += $"&Norm={CrossSectionNorm}";
                        if (sender is TabItem)
                        {
                            CbxCrossSectionNorms.Items.Add("");
                        }
                        else
                        {
                            Forms.Clear();
                            Shapes.Clear();
                            CrossSectionItems.Clear();
                            CrossSectionProperties.Clear();
                        }

                        var catalogs = _integrationBase.ApiCore.Catalogs.GetAll(filter);
                        foreach (var item in catalogs)
                        {
                            if (sender is TabItem)
                            {
                                foreach (var norm in item.Norms.Where(norm => !CbxCrossSectionNorms.Items.Contains(norm.Name)))
                                {
                                    CbxCrossSectionNorms.Items.Add(norm.Name);
                                }
                            }

                            Forms.Add(new CatalogGroup
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description
                            });
                        }
                        break;
                    }
                case "SelectedMaterialCatalog":
                    {
                        if (!(SelectedMaterialCatalog is CatalogGroup catalog))
                            break;
                        var items = _integrationBase.ApiCore.Catalogs.GetAllCatalogItems(catalog.Id);
                        if (items == null) break;
                        MaterialProperties.Clear();
                        MaterialItems.Clear();
                        foreach (var item in items)
                        {
                            MaterialItems.Add(new CatalogGroup
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description
                            });
                        }

                        break;
                    }
                case "SelectedMaterialItem":
                    {
                        var item = SelectedMaterialItem as CatalogGroup;
                        if (item == null)
                            break;
                        var properties = _integrationBase.ApiCore.Catalogs.GetCatalogItem(item.Id);
                        MaterialProperties.Clear();
                        foreach (var property in properties.Properties)
                        {
                            var p = ContentAttributes.Find(x => x.Id == property.Key);
                            if (p == null) break;
                            MaterialProperties.Add(new CatalogProperty
                            {
                                Id = property.Key,
                                Name = p.Name,
                                AttributeValue = $"{property.Value} {p.Unit}",
                                Type = p.FreeAttribTypeStr
                            });
                        }

                        break;
                    }
                case "MaterialFilter":
                    {
                        var filter = "Type=Material";
                        if (!string.IsNullOrEmpty(MaterialNorm))
                            filter += $"&Norm={MaterialNorm}";
                        if (!string.IsNullOrEmpty(MaterialCountry))
                            filter += $"&Country={MaterialCountry}";

                        if (sender is TabItem)
                        {
                            CbxMaterialCountries.Items.Add("");
                            CbxMaterialNorms.Items.Add("");
                        }
                        else
                        {
                            MaterialCatalogs.Clear();
                            MaterialItems.Clear();
                            MaterialProperties.Clear();
                        }

                        var catalogs = _integrationBase.ApiCore.Catalogs.GetAll(filter);
                        foreach (var item in catalogs)
                        {
                            if (sender is TabItem)
                            {
                                foreach (var country in item.Countries.Where(country =>
                                             !CbxMaterialCountries.Items.Contains(country.Name)))
                                {
                                    CbxMaterialCountries.Items.Add(country.Name);
                                }

                                foreach (var norm in item.Norms.Where(norm => !CbxMaterialNorms.Items.Contains(norm.Name)))
                                {
                                    CbxMaterialNorms.Items.Add(norm.Name);
                                }
                            }

                            MaterialCatalogs.Add(new CatalogGroup
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description
                            });
                        }
                        break;
                    }
            }
        }

        #region private member

        private IntegrationBase _integrationBase;

        #endregion private member
        #region CrossSectionProperties

        public string CrossSectionNorm => (string)CbxCrossSectionNorms.SelectedItem ?? "";

        private ObservableCollection<CatalogGroup> _forms = new ObservableCollection<CatalogGroup>();
        public ObservableCollection<CatalogGroup> Forms
        {
            get => _forms;
            set
            {
                _forms = value;
                NotifyPropertyChanged("Forms");
            }
        }

        private ObservableCollection<CatalogGroup> _shapes = new ObservableCollection<CatalogGroup>();
        public ObservableCollection<CatalogGroup> Shapes
        {
            get => _shapes;
            set
            {
                _shapes = value;
                NotifyPropertyChanged("Shapes");
            }
        }

        private ObservableCollection<CatalogGroup> _crossSectionItems = new ObservableCollection<CatalogGroup>();
        public ObservableCollection<CatalogGroup> CrossSectionItems
        {
            get => _crossSectionItems;
            set
            {
                _crossSectionItems = value;
                NotifyPropertyChanged("CrossSectionItems");
            }
        }


        /// <summary>
        /// collections of CatalogProperties
        /// </summary>
        private ObservableCollection<CatalogProperty> _crossSectionProperties = new ObservableCollection<CatalogProperty>();
        public ObservableCollection<CatalogProperty> CrossSectionProperties
        {
            get => _crossSectionProperties;
            set
            {
                _crossSectionProperties = value;
                NotifyPropertyChanged("CrossSectionProperties");
            }
        }

        private object _selectedForm;
        public object SelectedForm
        {
            get => _selectedForm;
            set
            {
                _selectedForm = value;
                NotifyPropertyChanged("SelectedForm");
            }
        }

        private object _selectedShape;
        public object SelectedShape
        {
            get => _selectedShape;
            set
            {
                _selectedShape = value;
                NotifyPropertyChanged("SelectedShape");
            }
        }

        private object _selectedCrossSection;
        public object SelectedCrossSection
        {
            get => _selectedCrossSection;
            set
            {
                _selectedCrossSection = value;
                NotifyPropertyChanged("SelectedCrossSection");
            }
        }
        #endregion properties

        #region MaterialProperties

        public string MaterialNorm => (string)CbxMaterialNorms.SelectedItem ?? "";
        public string MaterialCountry => (string)CbxMaterialCountries.SelectedItem ?? "";

        private ObservableCollection<CatalogGroup> _materialCatalogs = new ObservableCollection<CatalogGroup>();

        public ObservableCollection<CatalogGroup> MaterialCatalogs
        {
            get => _materialCatalogs;
            set
            {
                _materialCatalogs = value;
                NotifyPropertyChanged("MaterialCatalogs");
            }
        }

        /// <summary>
        /// collections of CatalogItems
        /// </summary>
        private ObservableCollection<CatalogGroup> _materialItems = new ObservableCollection<CatalogGroup>();

        public ObservableCollection<CatalogGroup> MaterialItems
        {
            get => _materialItems;
            set
            {
                _materialItems = value;
                NotifyPropertyChanged("MaterialItems");
            }
        }

        private ObservableCollection<CatalogProperty> _materialProperties = new ObservableCollection<CatalogProperty>();

        public ObservableCollection<CatalogProperty> MaterialProperties
        {
            get => _materialProperties;
            set
            {
                _materialProperties = value;
                NotifyPropertyChanged("MaterialProperties");
            }
        }

        private object _selectedMaterialCatalog;

        public object SelectedMaterialCatalog
        {
            get => _selectedMaterialCatalog;
            set
            {
                _selectedMaterialCatalog = value;
                NotifyPropertyChanged("SelectedMaterialCatalog");
            }
        }

        private object _selectedMaterialItem;

        public object SelectedMaterialItem
        {
            get => _selectedMaterialItem;
            set
            {
                _selectedMaterialItem = value;
                NotifyPropertyChanged("SelectedMaterialItem");
            }
        }

        #endregion
        #region properties

        public List<DtoFreeAttribute> ContentAttributes { get; set; }

        #endregion properties

        /// <summary>
        /// Load content.
        /// </summary>
        /// <param name="integrationBase">IntegrationBase</param>
        /// <param name="parent">Parent window</param>
        internal void LoadContent(IntegrationBase integrationBase, Window parent)
        {
            _integrationBase = integrationBase;

        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyPropertyChanged(object sender, [CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
        }

        private void CrossSections_OnLoaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(sender, "CrossSectionFilter");
            //var t = _integrationBase.ApiCore.Catalogs.GetAll($"Type=CrossSection");
        }

        private void Materials_OnLoaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(sender, "MaterialFilter");
        }

        private void MaterialFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(sender, "MaterialFilter");
        }
        private void CrossSectionFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(sender, "CrossSectionFilter");
        }
    }

    public class CatalogGroup //: INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid Id { get; set; }
    }

    public class CatalogProperty
    {
        public string Name { get; set; }
        public string AttributeValue { get; set; }
        public string Type { get; set; }
        public Guid Id { get; set; }
    }
}
