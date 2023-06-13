using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Ribbon;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Data.TenantDto;
using BimPlusDemo.UserControls;

namespace BimPlusDemo
{
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        /// <summary>
        /// search requested model.
        /// if it doesn't exist it will be created included the root divisionTopologyNode.
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns>DtoDivision</returns>
        private DtoDivision SelectModel(string modelName) //, string topologyNode=null, out Guid? topologyId=null)
        {
            var model = _intBase.ApiCore.Divisions.GetProjectDivisions(ProjectId)?.Find(x => x.Name == modelName);
            if (model == null)
            {
                // create a new model.
                model = _intBase.ApiCore.Divisions.CreateModel(ProjectId, new DtoDivision {Name = modelName});
                if (model == null)
                {
                    MessageBox.Show("Could not create Model.");
                    return null;
                }
                // create model root node.
                var td = _intBase.ApiCore.DtObjects.PostObject(new TopologyDivision
                {
                    Parent = ProjectId,
                    Division = model.Id,
                    Name = modelName
                });
                if (td == null || td.Id == Guid.Empty)
                {
                    MessageBox.Show("Could not create TopologyDivision object.");
                    return null;
                }
                model.TopologyDivisionId = td.Id;
            }
            return model;
        }

        private void CreateStirrup(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("Geometry Stuff");
            if (model == null)
                return;

            DisposeContentControl();
            var geometryView = new GeometryView(model, "Stirrup", "3x10");
            ContentControl.Content = geometryView;
            EnabledContent = "Geometry";

        }

        private void CreateProfile(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("Geometry Stuff");
            if (model == null)
                return;

            DisposeContentControl();
            var geometryView = new GeometryView( model, "Profile", "IPE200");
            ContentControl.Content = geometryView;
            EnabledContent = "Geometry";
        }

        private void CreateContour(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("Geometry Stuff");
            if (model == null)
                return;

            DisposeContentControl();
            var geometryView = new GeometryView( model, "Contour", "ParametricObject");
            ContentControl.Content = geometryView;
            EnabledContent = "Geometry";
        }
        private void CreateMesh(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("Geometry Stuff");
            if (model == null)
                return;

            DisposeContentControl();
            var geometryView = new GeometryView(model, "Mesh",  "Building A");
            ContentControl.Content = geometryView;
            EnabledContent = "Geometry";
        }


        private void Attributes_Click(object sender, RoutedEventArgs e)
        {
            if (ContentControl.Content is AttributeView)
                return;

            DisposeContentControl();
            var properties = new AttributeView(_intBase);
            if (SelectedObjects.Count > 0)
                properties.AssignObject(SelectedObjects[0]);
            ContentControl.Content = properties;
            EnabledContent = "Attributes";
        }

        private void BaseQuantities(object sender, RoutedEventArgs e)
        {
            if (ContentControl.Content is BaseQuantitiesView)
                return;

            DisposeContentControl();
            var qto = new BaseQuantitiesView(_intBase);
            ContentControl.Content = qto;
            EnabledContent = "Quantities";
        }

        /// <summary>
        /// Show BimPlus catalogs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Catalogs_OnClick(object sender, RoutedEventArgs e)
        {
            var catalogsControl = new BimPlusCatalogs();
            catalogsControl.LoadContent(_intBase, this);

            catalogsControl.Show();
        }
    }
}
