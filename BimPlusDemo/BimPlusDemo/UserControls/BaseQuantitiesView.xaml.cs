using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.Content;
using BimPlus.Sdk.Data.TenantDto;
using BimPlusDemo.Annotations;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for BaseQuantitiesView.xaml
    /// </summary>
    public partial class BaseQuantitiesView : INotifyPropertyChanged
    {
        private IntegrationBase IntBase { get; set; }
        public List<DtoDivision> Divisions { get; set; }
        public DataTable Results { get; set; }


        public BaseQuantitiesView(IntegrationBase integrationBase)
        {
            DataContext = this;
            InitializeComponent();
            IntBase = integrationBase;
            Initialize();
        }

        /// <summary>
        /// Initialize combobox and dataGrid.
        /// </summary>
        public void Initialize()
        {
            if (Qto.Items.Count == 0)
            {
                Qto.ItemsSource = Enum.GetValues(typeof(BaseQuantities));
            }
            Divisions = IntBase.ApiCore.Divisions.GetProjectDivisions(IntBase.CurrentProject.Id);
            ModelCmb.ItemsSource = Divisions;
            ModelCmb.DisplayMemberPath = "Name";


            if (MainWindow.SelectedObjects.Count > 0)
            {
                SelectDivision.Visibility = Visibility.Collapsed;
            }
            else
            {
                Selected.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// start quantity take-off service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calculate_OnClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow.SelectedObjects.Count > 0)
                CalculateProperties();
            else
                FillBaseQuantities();
        }

        private void CalculateProperties()
        {
            var selection =
                IntBase.ApiCore.Objects.CreateSelection(IntBase.CurrentProject.Id, MainWindow.SelectedObjects, true);

            try
            {
                var serviceData = new DtoServices
                {
                    BaseQuantities = new QtoService
                    {
                        ProjectId = IntBase.CurrentProject.Id,
                        //DivisionId = div.Id,
                        //Quantities = quantities
                    }
                };

                Dictionary<Guid, Dictionary<Guid, object>> results =
                    IntBase.ApiCore.Services.ExecuteService<Dictionary<Guid, Dictionary<Guid, object>>>(
                        "BaseQuantities", serviceData);

                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("nothing calculated.");
                    return;
                }

                if (Results == null)
                {
                    Results = new DataTable("calulated quantities");
                    Results.Columns.Add("Id", typeof(Guid));
                    Results.PrimaryKey = new[] { Results.Columns["Id"] };
                }
                else
                    Results.Clear();
                foreach (var kvp in results)
                {
                    var attribute = IntBase.ApiCore.Attributes.Get(kvp.Key);
                    if (attribute == null) continue;
                    Results.Columns.Add(attribute.Name, FreeAttribTypeConverter.GuidToType(attribute.FreeAttribType));
                    var elements = kvp.Value;
                    foreach (var element in elements)
                    {
                        var row = Results.Rows.Find(element.Key);
                        if (row == null)
                        {
                            row = Results.NewRow();
                            row["Id"] = element.Key;
                            row[attribute.Name] = element.Value;
                            Results.Rows.Add(row);
                        }
                        else
                            row[attribute.Name] = element.Value;
                    }
                }

                CalculatedProperties.DataContext = Results;
                CalculatedProperties.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show("nothing calculated.");
            }
        }

        private void FillBaseQuantities()
        {
            var div = ModelCmb.SelectedItem as DtoDivision;
            var quantities = Qto.SelectedItems.Cast<BaseQuantities>().ToList();

            if (div == null)
            {
                MessageBox.Show("No Model selected.");
                return;
            }

            if (quantities.Count == 0)
            {
                MessageBox.Show("No Model/BaseQuantity selected.");
                return;
            }

            try
            {
                var serviceData = new DtoServices
                {
                    BaseQuantities = new QtoService
                    {
                        ProjectId = IntBase.CurrentProject.Id,
                        DivisionId = div.Id,
                        Quantities = quantities
                    }
                };

                Dictionary<Guid, Dictionary<Guid, object>> results =
                    IntBase.ApiCore.Services.ExecuteService<Dictionary<Guid, Dictionary<Guid, object>>>(
                        "BaseQuantities", serviceData);

                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("nothing calculated.");
                    return;
                }

                if (Results == null)
                {
                    Results = new DataTable("calulated quantities");
                    Results.Columns.Add("Id", typeof(Guid));
                    Results.PrimaryKey = new[] { Results.Columns["Id"] };
                }
                else
                    Results.Clear();
                foreach (var kvp in results)
                {
                    var attribute = IntBase.ApiCore.Attributes.Get(kvp.Key);
                    if (attribute == null) continue;
                    Results.Columns.Add(attribute.Name, FreeAttribTypeConverter.GuidToType(attribute.FreeAttribType));
                    var elements = kvp.Value;
                    foreach (var element in elements)
                    {
                        var row = Results.Rows.Find(element.Key);
                        if (row == null)
                        {
                            row = Results.NewRow();
                            row["Id"] = element.Key;
                            row[attribute.Name] = element.Value;
                            Results.Rows.Add(row);
                        }
                        else
                            row[attribute.Name] = element.Value;
                    }
                }

                CalculatedProperties.DataContext = Results;
                CalculatedProperties.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show("nothing calculated.");
            }
        }

        /// <summary>
        /// handle selected items in webViewer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResultView_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (CalculatedProperties.SelectedItem == null)
                return;
            var item = ((DataRowView) CalculatedProperties.SelectedItem).Row["Id"];
            if (Guid.TryParse(item.ToString(), out Guid id))
                IntBase.EventHandlerCore.OnModelModified(this, new BimPlusEventArgs { Value = "SelectObject", Id = id});
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
