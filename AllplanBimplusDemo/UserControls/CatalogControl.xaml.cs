using AllplanBimplusDemo.WinForms;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.Content;
using BimPlus.Sdk.Data.TenantDto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace AllplanBimplusDemo.UserControls
{
	/// <summary>
	/// Class CatalogsControl.
	/// </summary>
	public partial class CatalogsControl : BaseAttributesUserControl
    {
		/// <summary>
		/// Contructor.
		/// </summary>
        public CatalogsControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CatalogControl_Loaded(object sender, RoutedEventArgs e)
        {
            PropertyChanged += CatalogsControl_PropertyChanged;
            ButtonsEnabled = true;

            List<BaseContentDto> list = _integrationBase.ApiCore.Catalogs.GetContent(BimPlus.Sdk.Api.Catalogs.BaseContentType.Norm);
            if (list != null && list.Count > 0)
            {
                foreach (var norm in list)
                {
                    Norms.Items.Add(norm.Name);
                }
                Norms.SelectedItem = Norms.Items[1];
            }

            list = _integrationBase.ApiCore.Catalogs.GetContent(BimPlus.Sdk.Api.Catalogs.BaseContentType.Type);
            if (list != null && list.Count > 0)
            {
                foreach (var type in list)
                {
                    Types.Items.Add(type.Name);
                }
                Types.SelectedItem = Types.Items[0];
            }

            list = _integrationBase.ApiCore.Catalogs.GetContent(BimPlus.Sdk.Api.Catalogs.BaseContentType.Country);
            if (list != null && list.Count > 0)
            {
                foreach (var type in list)
                {
                    Countries.Items.Add(type.Name);
                }
            }

            ContentAttributes = _integrationBase.ApiCore.Attributes.GetAll();

        }

        private void CatalogsControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Type":
                case "Norm":
                    {
                        var t = _integrationBase.ApiCore.Catalogs.GetAll($"Type={Type}&Norm={Norm}");

                        ItemProperties.Clear();
                        CatalogItems.Clear();
                        _catalogs.Clear();

                        if (t != null && t.Count > 0)
                        {
                            foreach (var item in t)
                            {
                                Catalogs.Add(new CatalogGroup
                                {
                                    Id = item.Id,
                                    Name = item.Name,
                                    Description = item.Description
                                });
                                if (Type == "CrossSection")
                                {
                                    var serviceUrl = $"{_integrationBase.ServerName}/v2/content/crosssectiondefinitions/{item.Id}";
                                    try
                                    {
                                        var cd = BimPlus.LightCaseClient.GenericProxies.RestGet<DtoCrossSectionDefinition>(serviceUrl, _integrationBase.ClientConfiguration);
                                        if (cd == null || cd.Shapes?.Count == 0)
                                            continue;
                                        foreach (var shape in cd.Shapes)
                                        {
                                            Catalogs.Add(new CatalogGroup
                                            {
                                                Id = shape.Id,
                                                Description = shape.Name,
                                                Name = ""
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.WriteLine(string.Format("Get catalogs: {0}", ex.Message));
                                    }
                                }
                            }
                        }
                        break;
                    }
                case "SelectedCatalog":
                    {
                        var catalog = SelectedCatalog as CatalogGroup;
                        if (catalog == null)
                            break;
                        var items = _integrationBase.ApiCore.Catalogs.GetAllCatalogItems(catalog.Id);
                        ItemProperties.Clear();
                        CatalogItems.Clear();
                        foreach (var item in items)
                        {
                            CatalogItems.Add(new CatalogGroup
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description
                            });
                        }
                        break;
                    }
                case "SelectedItem":
                    {
                        var item = SelectedItem as CatalogGroup;
                        if (item == null)
                            break;
                        var properties = _integrationBase.ApiCore.Catalogs.GetCatalogItem(item.Id);
                        ItemProperties.Clear();
                        foreach (var property in properties.Properties)
                        {
                            ItemProperties.Add(new CatalogProperty
                            {
                                Id = property.Key,
                                Name = ContentAttributes.Find(x => x.Id == property.Key)?.Name ?? property.Key.ToString(),
                                AttributeValue = property.Value 
                            });
                        }
                        break;
                    }
            }
            //base.PropertyChanged(sender, e);
        }

        #region private member

        private IntegrationBase _integrationBase;

        #endregion private member

        #region properties

        public string Norm => (string) Norms.SelectedItem ?? "";
        public string Type => (string) Types.SelectedItem ?? "";


        //private List<DtoFreeAttribute> _contentAttributes;
        public List<DtoFreeAttribute> ContentAttributes { get; set; }
        /// <summary>
        /// collections of Catalogs
        /// </summary>
        private ObservableCollection<CatalogGroup> _catalogs = new ObservableCollection<CatalogGroup>();
        public ObservableCollection<CatalogGroup> Catalogs
        {
            get { return _catalogs; }
            set { _catalogs = value; NotifyPropertyChanged("Catalogs"); }
        }

        /// <summary>
        /// collections of CatalogItems
        /// </summary>
        private ObservableCollection<CatalogGroup> _catalogItems = new ObservableCollection<CatalogGroup>();
        public ObservableCollection<CatalogGroup> CatalogItems
        {
            get { return _catalogItems; }
            set { _catalogs = value; NotifyPropertyChanged("CatalogItems"); }
        }

        /// <summary>
        /// collections of CatalogProperties
        /// </summary>
        private ObservableCollection<CatalogProperty> _itemProperties = new ObservableCollection<CatalogProperty>();
        public ObservableCollection<CatalogProperty> ItemProperties
        {
            get { return _itemProperties; }
            set { _itemProperties = value; NotifyPropertyChanged("ItemProperties"); }
        }

        /// <summary>
        /// Property SelectedItem.
        /// </summary>
        private object _selectedCatalog;
        public object SelectedCatalog
        {
            get { return _selectedCatalog; }
            set { _selectedCatalog = value; NotifyPropertyChanged("SelectedCatalog"); }
        }

        private object _SelectedItem;
        public object SelectedItem
        {
            get { return _SelectedItem; }
            set { _SelectedItem = value; NotifyPropertyChanged("SelectedItem"); }
        }

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


        private void Types_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Type");
        }

        private void Norms_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Norm");
        }
    }

    public class CatalogGroup //: INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        //protected void OnPropertyChanged(string name = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //}
    }

    public class CatalogProperty
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public object AttributeValue { get; set; }
    }
}
