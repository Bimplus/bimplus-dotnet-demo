using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.DbCore.Help;
using BimPlus.Sdk.Data.DbCore.Reinforcement;
using BimPlus.Sdk.Data.DbCore.Space;
using BimPlus.Sdk.Data.DbCore.Steel;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Data.TenantDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for PostGeometryObjects.xaml
    /// </summary>
    public partial class GeometryView
    {
        public DtoDivision Model { get; set; }
        public IntegrationBase IntBase { get; set; }
        private JObject ResourceData { get; set; }
        public string ElementName { get; set; } = "";

        public Guid DivisionTopologyId => Model.TopologyDivisionId.GetValueOrDefault(Guid.Empty);

        /// <summary>
        /// generate some dummy objects, based on content type.
        /// </summary>
        internal DtObject Element
        {
            get
            {
                switch (Label.Content)
                {
                    case "Stirrup":
                        return new ReinforcingBar();
                    case "Profile":
                        return new Steel();
                    case "Mesh":
                        return new Room();
                    default:
                        return new GeometryObject();
                }
            }
        }

        public GeometryView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public GeometryView(IntegrationBase intBase, DtoDivision model, string title, string name)
        {
            InitializeComponent();
            DataContext = this;
            Label.Content = title;
            ElementName = name;
            IntBase = intBase;
            Model = model;
            var resourceData = Properties.Resources.ResourceManager.GetObject(title)?.ToString();
            var tooltip = Properties.Resources.ResourceManager.GetObject($"{title}Tooltip")?.ToString();
            if (!string.IsNullOrEmpty(resourceData))
            {
                ResourceData = JsonConvert.DeserializeObject<JObject>(resourceData);
                Data.Text = resourceData;
            }

            if (!string.IsNullOrEmpty(tooltip))
                Data.ToolTip = tooltip;
        }

        /// <summary>
        /// Create object in new Division 'GeometryStuff'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PostObject(object sender, RoutedEventArgs e)
        {
            DtObject element = (sender is Button control && control.Tag != null)
                ? (DtObject)DbObjectList.Create((string)control.Tag)
                : Element;

            if (Model == null || Model.TopologyDivisionId.GetValueOrDefault() == Guid.Empty)
            {
                MessageBox.Show("Invalid ModelData.");
                return;
            }

            // check if this node already exist.
            var topology = IntBase.ApiCore.Objects.GetTopology(DivisionTopologyId);
            var node = topology?.Children.FirstOrDefault(x => x.Name == (string) Label.Content);
            if (node != null)
            {
                if (MessageBox.Show($"{Label.Content} already exist!\nDo you like to replace it?", "",
                        MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
                // remove existing TopologyNode including all child objects.
                IntBase.ApiCore.DtObjects.DeleteObject(node.Id);
            }

            if (ResourceData != null)
                Helper.AddGeometry(element, ResourceData);

            element.Name = ElementName;
            element.Division = Model.Id;
            element.LogParentID = Model.ProjectId;

            var topItem = new TopologyItem
            {
                Parent = Model.TopologyDivisionId,
                Division = Model.Id,
                LogParentID = Model.ProjectId,
                Name = Label.Content.ToString(),
                Children = new List<DtObject>(1) {element}
            };

            // post to BimPlus server.
            if (IntBase.ApiCore.DtObjects.PostObject(topItem) == null)
                MessageBox.Show("could not create geometry object.");
            else
                IntBase.ApiCore.Projects.ConvertGeometry(Model.ProjectId);

            IntBase.EventHandlerCore.OnExportStarted(this,
                new BimPlusEventArgs {Id = Model.Id, Value = "ModelChanged"});
        }
    }
}

