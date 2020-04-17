using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.Content;
using System;
using System.Collections.Generic;
using System.Windows;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for CatalogsControl.xaml
    /// </summary>
    public partial class CatalogsControl : BaseAttributesUserControl
    {
        public CatalogsControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void BaseAttributesUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonsEnabled = true;
        }

        #region private member

        private IntegrationBase _integrationBase;

        #endregion private member

        #region properties

        private bool _buttonsEnabled = false;

        /// <summary>
        /// Property ButtonsEnabled.
        /// </summary>
        public bool ButtonsEnabled
        {
            get { return _buttonsEnabled; }
            set { _buttonsEnabled = value; NotifyPropertyChanged(); }
        }

        private List<BaseContentDto> _baseContentDtoList;

        /// <summary>
        /// Property BaseContentDtoList.
        /// </summary>
        public List<BaseContentDto> BaseContentDtoList
        {
            get { return _baseContentDtoList; }
            set { _baseContentDtoList = value; NotifyPropertyChanged(); }
        }

        private List<CatalogClass> _catalogClassList;

        /// <summary>
        /// Property CatalogClassList.
        /// </summary>
        public List<CatalogClass> CatalogClassList
        {
            get { return _catalogClassList; }
            set { _catalogClassList = value; NotifyPropertyChanged(); }
        }

        private object _selectedObject;

        /// <summary>
        /// Property SelectedObject.
        /// </summary>
        public object SelectedObject
        {
            get { return _selectedObject; }
            set { _selectedObject = value; NotifyPropertyChanged(); }
        }

        #endregion properties

        internal void LoadContent(IntegrationBase integrationBase, Window parent)
        {
            _integrationBase = integrationBase;
        }

        private void GetNormData_Click(object sender, RoutedEventArgs e)
        {
            ContentType.Content = "Norm";

            BaseContentDtoDataGrid.Visibility = Visibility.Visible;
            DtoCatalogDataGrid.Visibility = Visibility.Collapsed;

            List<BaseContentDto> list = _integrationBase.ApiCore.Catalogs.GetContent(BimPlus.Sdk.Api.Catalogs.BaseContentType.Norm);
            if (list != null && list.Count > 0)
            {
                BaseContentDtoDataGrid.Focus();
                BaseContentDtoList = list;
                SelectedObject = BaseContentDtoList[0];
            }
        }

        private void GetCountryData_Click(object sender, RoutedEventArgs e)
        {
            ContentType.Content = "Country";

            BaseContentDtoDataGrid.Visibility = Visibility.Visible;
            DtoCatalogDataGrid.Visibility = Visibility.Collapsed;

            List<BaseContentDto> list = _integrationBase.ApiCore.Catalogs.GetContent(BimPlus.Sdk.Api.Catalogs.BaseContentType.Country);
            if (list != null && list.Count > 0)
            {
                BaseContentDtoDataGrid.Focus();
                BaseContentDtoList = list;
                SelectedObject = BaseContentDtoList[0];
            }
        }

        private void GetTypeData_Click(object sender, RoutedEventArgs e)
        {
            ContentType.Content = "Type";

            BaseContentDtoDataGrid.Visibility = Visibility.Visible;
            DtoCatalogDataGrid.Visibility = Visibility.Collapsed;

            List<BaseContentDto> list = _integrationBase.ApiCore.Catalogs.GetContent(BimPlus.Sdk.Api.Catalogs.BaseContentType.Type);
            if (list != null && list.Count > 0)
            {
                BaseContentDtoDataGrid.Focus();
                BaseContentDtoList = list;
                SelectedObject = BaseContentDtoList[0];
            }
        }

        private void GetCatalogData_Click(object sender, RoutedEventArgs e)
        {
            ContentType.Content = "Catalog";

            BaseContentDtoDataGrid.Visibility = Visibility.Collapsed;
            DtoCatalogDataGrid.Visibility = Visibility.Visible;

            List<DtoCatalog> list = _integrationBase.ApiCore.Catalogs.GetAll();
            if (list != null && list.Count > 0)
            {
                List<CatalogClass> catalogList = new List<CatalogClass>();
                foreach (DtoCatalog catalog in list)
                {
                    CatalogClass catalogClass = new CatalogClass();
                    if (catalog.Norms != null && catalog.Norms.Count > 0)
                    {
                        string content = "";
                        for (int i = 0; i < catalog.Norms.Count; i++)
                        {
                            if (string.IsNullOrEmpty(content))
                                content = catalog.Norms[i].Name;
                            else
                                content = string.Format("{0}{1}{2}", content, ";", catalog.Norms[i].Name);
                        }
                        catalogClass.Norms = content;
                    }

                    if (catalog.Countries != null && catalog.Countries.Count > 0)
                    {
                        string content = "";
                        for (int i = 0; i < catalog.Countries.Count; i++)
                        {
                            if (string.IsNullOrEmpty(content))
                                content = catalog.Countries[i].Name;
                            else
                                content = string.Format("{0}{1}{2}", content, ";", catalog.Countries[i].Name);
                        }
                        catalogClass.Countries = content;
                    }

                    if (catalog.CatalogType != null)
                    {
                        catalogClass.Name = catalog.Name;
                    }

                    if (!string.IsNullOrEmpty(catalog.Description))
                        catalogClass.Description = catalog.Description;

                    catalogClass.Id = catalog.Id;

                    catalogList.Add(catalogClass);
                }

                CatalogClassList = catalogList;
                if (CatalogClassList.Count > 0)
                {
                    bool focused = DtoCatalogDataGrid.Focus();
                    SelectedObject = CatalogClassList[0];
                }
            }
        }
    }

    /// <summary>
    /// Catalog wrapper class.
    /// </summary>
    public class CatalogClass
    {
        public string Norms { get; set; }

        public string Countries { get; set; }

        public Guid? Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Localization { get; set; }
    }
}
