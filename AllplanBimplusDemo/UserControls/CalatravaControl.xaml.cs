using AllplanBimplusDemo.Classes;
using AllplanBimplusDemo.WinForms;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.DbCore.Analysis;
using BimPlus.Sdk.Data.DbCore.Help;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Data.Geometry;
using BimPlus.Sdk.Data.StructuralLoadResource;
using BimPlus.Sdk.Data.TenantDto;
using BimPlus.Sdk.Utilities.V2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows;
using WPFWindows = System.Windows;
using Road = BimPlus.Sdk.Data.Road;
using System.Diagnostics;
using Nemetschek.NUtilLibrary;
using BimPlus.Sdk.Data.DbCore.Steel;
using BimPlus.Sdk.Data.CSG;
using BimPlus.Sdk.Data.DbCore.Connection;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for CalatravaControl.xaml
    /// </summary>
    public partial class CalatravaControl : NotifyPropertyChangedUserControl
    {
        public CalatravaControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region private member

        private IntegrationBase _integrationBase;
        private Window _parentWindow;

        private WebViewer _webViewer;

        private const string _structuralAnalysisName = "StructuralAnalysis";
        private const string _bimRoadName = "BimRoad TestModel_2.7";
        private const string _structuralSteelName = "StructuralSteel";

        #endregion private member

        #region properties

        private bool _buttonsEnabled = false;

        public bool ButtonsEnabled
        {
            get { return _buttonsEnabled; }

            set
            {
                if (_buttonsEnabled != value)
                {
                    _buttonsEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion properties

        #region public methods

        public void LoadContent(IntegrationBase integrationBase, Window parent)
        {
            _integrationBase = integrationBase;
            _integrationBase.EventHandlerCore.DataLoaded += EventHandlerCore_DataLoaded;
            _parentWindow = parent;

            _webViewer = new WebViewer(integrationBase);
            NavigateToControl();

            BimExplorer.Content = _webViewer;
        }

        public void UnloadContent()
        {
            if (_webViewer != null)
                _webViewer.Dispose();

            _integrationBase.EventHandlerCore.DataLoaded -= EventHandlerCore_DataLoaded;
        }

        #endregion public methods

        #region Bimplus events

        private void EventHandlerCore_DataLoaded(object sender, BimPlus.Client.BimPlusEventArgs e)
        {
            ButtonsEnabled = true;
            ProgressWindow.Hide();
        }

        #endregion Bimplus events

        #region private methods

        private DtoDivision GetBimRoadModel()
        {
            DtoDivision model = null;

            string modelName = _bimRoadName;

            ProgressWindow.Text = "Get BimRoad model.";
            ProgressWindow.Show();
            try
            {
                model = _integrationBase.ApiCore.Divisions.GetProjectDivisions(_integrationBase.CurrentProject.Id)?.Find(x => x.Name == modelName);

                bool existModel = model != null;
                if (model == null)
                {
                    model = _integrationBase.ApiCore.Divisions.CreateModel(_integrationBase.CurrentProject.Id,
                        new DtoDivision
                        {
                            Name = modelName //,
                            //ModelType = ModelTypeId.GeneralModel,
                        });
                }

                if (model != null)
                {
                    if (!existModel || model.TopologyDivisionId == null)
                    {
                        // Create main root TopologyDivision node.
                        DtObject topologyDivision = _integrationBase.ApiCore.DtObjects.PostObject(new TopologyDivision
                        {
                            Name = "RootNode",
                            Division = model.Id,
                            Parent = _integrationBase.CurrentProject.Id
                        });

                        if (topologyDivision == null || topologyDivision.Id == Guid.Empty)
                        {
                            MessageBoxHelper.ShowInformation("Could not create TopologyDivision object.", _parentWindow);
                            model = null;
                        }
                        else
                        {
                            model.TopologyDivisionId = topologyDivision.Id;
                            model = _integrationBase.ApiCore.Divisions.Update(model);
                        }
                    }
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            return model;
        }

        private DtoDivision GetStructuralAnalysisModel(string structuralAnalysisName = _structuralAnalysisName)
        {
            DtoDivision model = null;

            ProgressWindow.Text = "Get StructuralAnalysis model.";
            ProgressWindow.Show();

            try
            {
                List<DtoDivision> divisions = _integrationBase.ApiCore.Divisions.GetProjectDivisions(_integrationBase.CurrentProject.Id);
                DtoDivision existingModel = divisions?.Find(x => x.Name == structuralAnalysisName);
                if (existingModel != null)
                    model = existingModel;
                else
                {
                    model = _integrationBase.ApiCore.Divisions.CreateModel(_integrationBase.CurrentProject.Id, new DtoDivision { Name = structuralAnalysisName });

                    // Create main root TopologyDivision node.
                    DtObject topologyDivision = _integrationBase.ApiCore.DtObjects.PostObject(new TopologyDivision
                    {
                        Name = "RootNode",
                        Division = model.Id,
                        Parent = _integrationBase.CurrentProject.Id
                    });

                    if (topologyDivision == null || topologyDivision.Id == Guid.Empty)
                    {
                        MessageBoxHelper.ShowInformation("Could not create TopologyDivision object." ,_parentWindow);
                        model = null;
                    }
                    else
                    {
                        model.TopologyDivisionId = topologyDivision.Id;
                        model = _integrationBase.ApiCore.Divisions.Update(model);
                    }
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            return model;
        }

        private DtoDivision GetModelByName(string name)
        {
            List<DtoDivision> divisions = _integrationBase.ApiCore.Divisions.GetProjectDivisions(_integrationBase.CurrentProject.Id);
            DtoDivision existingModel = divisions?.Find(x => x.Name == name);
            return existingModel;
        }

        /// <summary>
        /// Post axis.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="xmlAxis"></param>
        /// <returns></returns>
        private Axis PostAxis(DtoDivision model, string xmlAxis)
        {
            // Create Axis object by given xml file.
            Axis axis = null;
            try
            {
                axis = Axis.DeserializeFromXmlString(xmlAxis);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("PostAxis: {0}", ex.Message));
            }

            if (axis == null)
            {
                MessageBoxHelper.ShowInformation("Could not create axis object from given xml-file", _parentWindow);
                return null;
            }

            // Fill Bimplus relevant properties.
            axis.Parent = model.TopologyDivisionId;
            axis.Division = model.Id;
            axis.LogParentID = _integrationBase.CurrentProject.Id;

            // Try to create a dummy 3D path for the axis.
            axis.Mesh = new DbGeometry
            {
                Edges = 4,
                Radius = 200,
                Color = (uint)Color.Lime.ToArgb(),
                Vertices = new List<double>()
            };

            // Move axis to origin.
            double? offsetX = null;
            double? offsetY = null;
            foreach (Road.HorizontalAlignment alignment in axis.HorizontalAlignments)
            {
                foreach (Road.HorizontalElement element in alignment.Elements)
                {
                    if (!offsetX.HasValue || offsetX.Value > element.StartX)
                        offsetX = element.StartX;
                    if (!offsetY.HasValue || offsetY.Value > element.StartY)
                        offsetY = element.StartY;
                }
            }

            // Create a dummy path line.
            foreach (Road.HorizontalAlignment alignment in axis.HorizontalAlignments)
            {
                foreach (Road.HorizontalElement element in alignment.Elements)
                {
                    axis.Mesh.Vertices.Add(element.StartX - offsetX.GetValueOrDefault()); // X
                    axis.Mesh.Vertices.Add(element.StartY - offsetY.GetValueOrDefault()); // Y
                    axis.Mesh.Vertices.Add(0);                                            // Z
                }
            }

            // Dummy CrossSection as ChildGeometyObject.
            CElementPoint p0 = new CElementPoint(0,
                axis.HorizontalAlignments[0].Elements[0].StartX - offsetX.GetValueOrDefault(),
                axis.HorizontalAlignments[0].Elements[0].StartY - offsetY.GetValueOrDefault()
            );

            CBaseElementPolyeder polyeder = new CBaseElementPolyeder();
            polyeder.point.Add(p0);
            polyeder.point.Add(new CElementPoint(p0.X, p0.Y + 1000, p0.Z + 1000));
            polyeder.point.Add(new CElementPoint(p0.X, p0.Y - 1000, p0.Z + 1000));
            polyeder.point.Add(new CElementPoint(p0.X, p0.Y + 5000, p0.Z + 2000));
            polyeder.point.Add(new CElementPoint(p0.X, p0.Y - 5000, p0.Z + 2000));
            polyeder.point.Add(new CElementPoint(p0.X, p0.Y + 5000, p0.Z + 2800));
            polyeder.point.Add(new CElementPoint(p0.X, p0.Y - 5000, p0.Z + 2800));

            CElementFace face = new CElementFace
            {
                polyeder.AppendEdge(1, 2),
                polyeder.AppendEdge(2, 4),
                polyeder.AppendEdge(4, 6),
                polyeder.AppendEdge(6, 7),
                polyeder.AppendEdge(7, 5),
                polyeder.AppendEdge(5, 3),
                polyeder.AppendEdge(3, 1)
            };
            polyeder.face.Add(face);
            polyeder.SetSpecialColor((uint)Color.BlueViolet.ToArgb());

            // Add crossSection as Child of Axis object.
            axis.AddChild(new ChildGeometryObject
            {
                Division = model.Id,
                BytePolyeder = polyeder
            });

            // Post object to Bimplus.
            DtObject result = _integrationBase.ApiCore.DtObjects.PostObject(axis);

            if (result == null)
            {
                MessageBoxHelper.ShowInformation("Could not create Axis object.", _parentWindow);
            }

            if (result == null || result.Id == Guid.Empty)
                return null;

            // Test: try to read concrete Axis object from Bimplus server.
            return _integrationBase.ApiCore.DtObjects.GetObject(result.Id, ObjectRequestProperties.InternalValues) as Axis;
        }

        /// <summary>
        /// Validate Axis informations.
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool ValidateAxis(List<Axis> axes, string name)
        {
            if (axes == null || axes.Count == 0)
            {
                MessageBoxHelper.ShowInformation("Something wrong when try to post Axis object to Bimplus server.", _parentWindow);
                return false;
            }

            Axis axism = axes.Find(x => x.Name.Equals(name));
            if (axism == null)
            {
                string message = string.Format("Axis {0} could not be found.", name);
                MessageBoxHelper.ShowInformation(message, _parentWindow);
                return false;
            }

            if (axism.HorizontalAlignments == null)
            {
                string message = string.Format("HorizontalAlignment could not be read from {0}.", name);
                MessageBoxHelper.ShowInformation(message, _parentWindow);
                return false;
            }

            if (axism.VerticalAlignments == null)
            {
                string message = string.Format("VerticalAlignments could not be read from {0}.", name);
                MessageBoxHelper.ShowInformation(message, _parentWindow);
                return false;
            }

            return true;
        }

        private void NavigateToControl()
        {
            ButtonsEnabled = false;
            ProgressWindow.Text = "Load BIM Explorer.";
            ProgressWindow.Show();
            _webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);
        }

        private List<StructuralPointConnection> GetStructuralPointConnections(DtoDivision model)
        {
            List<StructuralPointConnection> nodes = null;

            ProgressWindow.Text = "Get StructuralPointConnections.";
            ProgressWindow.Show();
            try
            {
                using (new TraceCodeTime("Get StructuralPointConnections", "Create Beams"))
                {
                    nodes = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value, false, false, true);
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            return nodes;
        }

        private List<StructuralCurveMember> GetStructuralCurveMember(DtoDivision model)
        {
            List<StructuralCurveMember> result = null;

            ProgressWindow.Text = "Get StructuralCurveMember.";
            ProgressWindow.Show();
            try
            {
                using (new TraceCodeTime("Get StructuralCurveMember", "Create Beams"))
                {
                    result = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(model.TopologyDivisionId.Value, false, false, true);
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            return result;
        }

        #endregion private methods

        #region button events

        private void CreateAxis_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetBimRoadModel();
            if (model == null)
                return;

            ProgressWindow.Text = "Create road axis.";
            ProgressWindow.Show();
            try
            {
                string axisName = "AXA1";
                List<Axis> axises = _integrationBase.ApiCore.DtObjects.GetObjects<Axis>(model.TopologyDivisionId.Value, false, false, true);

                Axis axis = null;

                if (axises == null || axises.Count == 0 || axises.All(x => x.Name != "AXA1"))
                {
                    // Test.
                    axis = axises.FirstOrDefault(a => a.Name == axisName);
                    // Create axis in Bimplus.
                    Axis dtoAxis = PostAxis(model, Properties.Resources.cadicsData);

                    if (dtoAxis != null)
                        axises?.Add(dtoAxis);
                }

                // ConvertGeometry must be called at the end of all post commands to convert GeometryData to Templates.
                if (ValidateAxis(axises, axisName))
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);

                NavigateToControl();
            }
            finally
            {
                //ProgressWindow.Hide();
            }
        }

        private void CreateNodes_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetStructuralAnalysisModel();
            if (model == null)
                return;

            List<StructuralPointConnection> nodes = GetStructuralPointConnections(model);

            if (nodes?.Count >= 12)
            {
                MessageBoxHelper.ShowInformation("Nodes are already created.", _parentWindow);
                return;
            }

            ProgressWindow.Text = "Create Nodes.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                // Create some StructuralPointconnection objects.
                TopologyDivision topologyDivision = new TopologyDivision
                {
                    Id = model.TopologyDivisionId.Value,
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    Children = new List<DtObject>(6)
                    {
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N1",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 0.5,
                            Y = 0.5,
                            Z = 0.2,
                            NodeId = 1,
                            AppliedCondition = new BoundaryNodeCondition("TRigid", true, true, true, true, true, true)
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N2",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 2.0,
                            Y = 0.5,
                            Z = 0.2,
                            NodeId = 2,
                            AppliedCondition = new BoundaryNodeCondition("TRigid", false, false, false, true, false, false)
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N3",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 3.6,
                            Y = 0.5,
                            Z = 0.2,
                            NodeId = 3,
                            AppliedCondition = new BoundaryNodeCondition("TRigid",false,false,false,false,true,true)
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N4",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 0.5,
                            Y = 6.0,
                            Z = 0.2,
                            NodeId = 4,
                            AppliedCondition = new BoundaryNodeCondition("Fixed", false, 3, true, false, 1.9, false)
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N5",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 2.0,
                            Y = 6.0,
                            Z = 0.2,
                            NodeId = 5,
                            AppliedCondition = new BoundaryNodeCondition("RRidig", false, false, false, true, true, true)
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N6",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 3.6,
                            Y = 6.0,
                            Z = 0.2,
                            NodeId = 6,
                            AppliedCondition = new BoundaryNodeCondition("N6", 2.0, true, 3.0, 2.5, false, 4.7)
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N7",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 0.5,
                            Y = 0.5,
                            Z = 3.4,
                            NodeId = 7
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N8",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 2.0,
                            Y = 0.5,
                            Z = 3.4,
                            NodeId = 8
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N9",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 3.6,
                            Y = 0.5,
                            Z = 3.4,
                            NodeId = 9
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N10",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 0.5,
                            Y = 6.0,
                            Z = 4.4,
                            NodeId = 10
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N11",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 2.0,
                            Y = 6.0,
                            Z = 4.4,
                            NodeId = 11
                        },
                        new StructuralPointConnection
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "N12",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            X = 3.600,
                            Y = 6.000,
                            Z = 4.400,
                            NodeId = 12
                        }
                    }
                };

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
                HttpStatusCode status = _integrationBase.ApiCore.DtObjects.PutObject(topologyDivision);
                if (status != HttpStatusCode.OK)
                    MessageBoxHelper.ShowInformation("Could not create TopologyDivision object.", _parentWindow);
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                    return;
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        private void CreateBeams_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetStructuralAnalysisModel();
            if (model == null)
                return;

            List<StructuralPointConnection> nodes = GetStructuralPointConnections(model);

            if (nodes?.Count < 12 || nodes == null)
            {
                MessageBoxHelper.ShowInformation("Please create at first Node objects (StructuralPointConnection).", _parentWindow);
                return;
            }

            List<StructuralCurveMember> members = GetStructuralCurveMember(model);
            if (members != null && members.Count == 13)
            {
                MessageBoxHelper.ShowInformation("Beams already created.", _parentWindow);
                return;
            }

            ProgressWindow.Text = "Create Beams.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                // Create some StructuralCurveMember objects.
                TopologyDivision topologyDivision = new TopologyDivision
                {
                    Id = model.TopologyDivisionId.Value,
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    Children = new List<DtObject>(13)
                    {
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B1",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R1",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 1)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R2",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 7)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B2",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R3",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 2)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R4",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 8)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B3",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R5",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 3)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R6",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 9)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B4",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R7",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 4)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R8",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 10)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B5",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R5",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 5)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R6",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 11)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B6",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R5",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 6)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R6",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 12)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B7",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R7",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 7)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R8",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 8)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B8",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R9",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 8)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R10",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 9)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B9",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R11",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 10)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R12",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 11)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B10",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R12",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 11)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R13",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 12)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B11",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R14",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 7)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R15",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 10)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B12",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R16",
                                    AppliedCondition = new BoundaryNodeCondition("",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 8)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R17",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 11)?.Id
                                }
                            }
                        },
                        new StructuralCurveMember
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "B13",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            { new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R18",
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 9)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 2,
                                    Name = "R19",
                                    AppliedCondition = new BoundaryNodeCondition("Rigid",false,false,false,true,true,true),
                                    RelatedStructuralConnection = nodes.Find(x => x.NodeId == 12)?.Id
                                }
                            }
                        }
                    }
                };

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
                if (_integrationBase.ApiCore.DtObjects.PutObject(topologyDivision) == HttpStatusCode.OK)
                {
                    if (_integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId) == HttpStatusCode.OK)
                    {
                        hideProgressWindow = false;
                        NavigateToControl();
                        return;
                    }
                }
                MessageBoxHelper.ShowInformation("Could not create StructuralMember objects.", _parentWindow);
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        private void CreateSupports_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetStructuralAnalysisModel();
            if (model == null)
                return;

            List<StructuralPointConnection> nodes = GetStructuralPointConnections(model);

            if (nodes?.Count < 12 || nodes == null)
            {
                MessageBoxHelper.ShowInformation("Please create at first Node objects (StructuralPointConnection).", _parentWindow);
                return;
            }

            ProgressWindow.Text = "Create Supports.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                // Create some StructuralPointReaction objects.
                TopologyDivision topologyDivision = new TopologyDivision
                {
                    Id = model.TopologyDivisionId.Value,
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    Children = new List<DtObject>(13)
                    {
                        new StructuralPointReaction
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "S1",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                            { new RelConnectsStructuralActivity
                                {
                                    Name = "AC1",
                                    RelatingElement = nodes.Find(x => x.NodeId == 1)?.Id
                                }
                            }
                        },
                        new StructuralPointReaction
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "S2",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                            { new RelConnectsStructuralActivity
                                {
                                    Name = "AC2",
                                    RelatingElement = nodes.Find(x => x.NodeId == 2)?.Id
                                }
                            }
                        },
                        new StructuralPointReaction
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "S3",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                            { new RelConnectsStructuralActivity
                                {
                                    Name = "AC3",
                                    RelatingElement = nodes.Find(x => x.NodeId == 3)?.Id
                                }
                            }
                        },
                        new StructuralPointReaction
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "S4",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                            { new RelConnectsStructuralActivity
                                {
                                    Name = "AC4",
                                    RelatingElement = nodes.Find(x => x.NodeId == 4)?.Id
                                }
                            }
                        },
                        new StructuralPointReaction
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "S5",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                            { new RelConnectsStructuralActivity
                                {
                                    Name = "AC5",
                                    RelatingElement = nodes.Find(x => x.NodeId == 5)?.Id
                                }
                            }
                        },
                        new StructuralPointReaction
                        {
                            Parent = model.TopologyDivisionId.Value,
                            Name = "S6",
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                            { new RelConnectsStructuralActivity
                                {
                                    Name = "AC6",
                                    RelatingElement = nodes.Find(x => x.NodeId == 6)?.Id
                                }
                            }
                        }
                    }
                };

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
                if (_integrationBase.ApiCore.DtObjects.PutObject(topologyDivision) == HttpStatusCode.OK)
                {
                    if (_integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId) == HttpStatusCode.OK)
                    {
                        hideProgressWindow = false;
                        NavigateToControl();
                        return;
                    }
                    else
                        hideProgressWindow = true;
                }
                else
                    MessageBoxHelper.ShowInformation("Could not create StructuralPointReaction objects.", _parentWindow);
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        private void CreateLoads_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetStructuralAnalysisModel();
            if (model == null)
                return;

            List<StructuralPointConnection> nodes =
                _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value, false, false, true);

            if (nodes?.Count < 12 || nodes == null)
            {
                MessageBoxHelper.ShowInformation("Please create at first Node objects (StructuralPointConnection).", _parentWindow);
                return;
            }

            List<StructuralCurveMember> beams = GetStructuralCurveMember(model);

            if (beams?.Count < 13 || beams == null)
            {
                MessageBoxHelper.ShowInformation("Please create at first Beam objects (StructuralCurveMember).", _parentWindow);
                return;
            }

            ProgressWindow.Text = "Create Loads.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                // Create some StructuralPointAction objects.
                TopologyDivision topologyDivision = new TopologyDivision
                {
                    Id = model.TopologyDivisionId.Value,
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    Children = new List<DtObject>(1)
                {
                    new StructuralPointAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "PointAction1",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadSingleForce { ForceZ = -6000 },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC1",
                                RelatingElement = nodes.Find(x => x.NodeId == 7)?.Id
                            }
                        }
                    },
                    new StructuralPointAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "PointAction2",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadSingleForce { ForceY = -3000 },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC2",
                                RelatingElement = nodes.Find(x => x.NodeId == 8)?.Id
                            }
                        }
                    },
                    new StructuralPointAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "PointAction3",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadSingleForce { ForceX = -3000 },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC3",
                                RelatingElement = nodes.Find(x => x.NodeId == 9)?.Id
                            }
                        }
                    },
                    new StructuralLinearAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LoadConfiguration",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadConfiguration
                        {
                            Values = new List<StructuralLoadOrResult>(2)
                            {
                                new StructuralLoadLinearForce { LinearForceZ = -4000 },
                                new StructuralLoadLinearForce { LinearForceZ = -2000 }
                            },
                            Locations = new List<double>(2) { 200, 2000 }
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "RW1",
                                RelatingElement = beams.Find(x => x.Name == "B12")?.Id
                            }
                        }
                    },
                    new StructuralLinearAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LinearLoad1",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadLinearForce { LinearForceX = 2000 },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "RW2",
                                RelatingElement = beams.Find(x => x.Name == "B11")?.Id
                            }
                        }
                    },
                    new StructuralLinearAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LinearLoad2",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadLinearForce { LinearForceY = -2000 },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "RW3",
                                RelatingElement = beams.Find(x => x.Name == "B9")?.Id
                            }
                        }
                    },
                    new StructuralLinearAction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LinearLoad",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadLinearForce { LinearForceY = -1000 },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "RW4",
                                RelatingElement = beams.Find(x => x.Name == "B10")?.Id
                            }
                        }
                    }
                }
                };

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
                if (_integrationBase.ApiCore.DtObjects.PutObject(topologyDivision) == HttpStatusCode.OK)
                {
                    if (_integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId) == HttpStatusCode.OK)
                    {
                        hideProgressWindow = false;
                        NavigateToControl();
                        return;
                    }
                }
                MessageBoxHelper.ShowInformation("Couldn't create StructuralAction objects.", _parentWindow);
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        private void CreateSlab_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetStructuralAnalysisModel();
            if (model == null)
            {
                MessageBoxHelper.ShowInformation("Could not create a model.", _parentWindow);
                return;
            }

            List<StructuralPointConnection> nodes = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value, false, false, true);
            if (nodes?.Count < 12)
            {
                MessageBoxHelper.ShowInformation("Please create at first node objects (StructuralPointConnection).");
                return;
            }

            if (nodes == null)
                return;

            // use PostObjectAsync to create one StructuralSurfaceMember
            StructuralSurfaceMember structuralSurfaceMember = new StructuralSurfaceMember
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "B12",
                Division = model.Id,
                LogParentID = model.ProjectId,
                ConnectedBy = new List<RelConnectsStructuralMember>(2)
                {
                    new RelConnectsStructuralMember
                    {
                        OrderNumber = 1,
                        Name = "P1",
                        RelatedStructuralConnection = nodes.Find(x => x.NodeId == 9)?.Id
                    },
                    new RelConnectsStructuralMember
                    {
                        OrderNumber = 2,
                        Name = "P2",
                        RelatedStructuralConnection = nodes.Find(x => x.NodeId == 8)?.Id
                    },
                    new RelConnectsStructuralMember
                    {
                        OrderNumber = 3,
                        Name = "P3",
                        RelatedStructuralConnection = nodes.Find(x => x.NodeId == 11)?.Id
                    },
                    new RelConnectsStructuralMember
                    {
                        OrderNumber = 4,
                        Name = "P4",
                        RelatedStructuralConnection = nodes.Find(x => x.NodeId == 12)?.Id
                    }
                }
            };

            Action action = new Action(() =>
            {
                NavigateToControl();
            });

            // Test!
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structuralSurfaceMember);
            _integrationBase.ApiCore.DtObjects.PostObjectAsync(structuralSurfaceMember, (exception, result) =>
            {
                if (exception != null)
                    MessageBoxHelper.ShowInformation(exception.Message);
                else
                {
                    HttpStatusCode code = _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    if (code != HttpStatusCode.OK)
                        MessageBoxHelper.ShowInformation("StructuralSurfaceMember couldn't be created.");
                    else
                    {
                        if (CreateSlab.CheckAccess())
                            NavigateToControl();
                        else
                            Dispatcher.BeginInvoke(WPFWindows.Threading.DispatcherPriority.Normal, action);
                    }
                }
            });
        }

        private void ElementAssembly_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetStructuralAnalysisModel(_structuralSteelName);
            if (model == null)
            {
                MessageBoxHelper.ShowInformation("Can't create a model.", _parentWindow);
                return;
            }

            List<StructuralPointConnection> nodes = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.ProjectId, false, false, true);
            if (nodes == null || nodes.Count < 12)
            {
                MessageBoxHelper.ShowInformation("Please create at first Node objects (StructuralPointConnection).");
                return;
            }

            StructuralPointConnection node3 = nodes.Find(x => x.NodeId == 3);
            StructuralPointConnection node6 = nodes.Find(x => x.NodeId == 6);
            StructuralPointConnection node9 = nodes.Find(x => x.NodeId == 9);
            StructuralPointConnection node12 = nodes.Find(x => x.NodeId == 12);
            if (node12 == null || node3 == null || node6 == null || node9 == null)
            {
                MessageBoxHelper.ShowInformation("Can't identify nodes.");
                return;
            }

            List<StructuralCurveMember> beams = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(model.ProjectId, false, false, true);
            if (beams == null)
            {
                MessageBoxHelper.ShowInformation("Please create at first Beam objects (StructuralCurveMember).");
                return;
            }

            TopologyDivision topologyDivision = new TopologyDivision
            {
                Id = model.TopologyDivisionId.Value,
                Division = model.Id,
                LogParentID = model.ProjectId
            };

            // Create some assemblies with relation to StructuralCurveMembers.
            ElementAssembly assembly1 = new ElementAssembly
            {
                Name = "B3_Column",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = _integrationBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.MediumBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B3_Assembly",
                        RelatedElement = beams.Find(x => x.Name == "B3")?.Id
                    }
                }
            };

            assembly1.CsgTree.Elements.Add(new Path
            {
                Geometry = new List<CsgGeoElement>
                {
                    new StartPolygon {Point = new List<double> {node3.X.GetValueOrDefault() * 1000, node3.Y.GetValueOrDefault() * 1000, node3.Z.GetValueOrDefault() * 1000}},
                    new Line {Point = new List<double> {node9.X.GetValueOrDefault() * 1000, node9.Y.GetValueOrDefault() * 1000, node9.Z.GetValueOrDefault() * 1000}}
                },
                CrossSection = "HEB120"
            });
            topologyDivision.AddChild(assembly1);

            ElementAssembly assembly2 = new ElementAssembly
            {
                Name = "B6_Column",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = _integrationBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.MediumBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B6_Assembly",
                        RelatedElement = beams.Find(x => x.Name == "B6")?.Id
                    }
                }
            };

            assembly2.CsgTree.Elements.Add(new Path
            {
                Rotation = Math.PI / 2,
                Geometry = new List<CsgGeoElement>
                {
                    new StartPolygon {Point = new List<double> {node6.X.GetValueOrDefault() * 1000, node6.Y.GetValueOrDefault() * 1000, node6.Z.GetValueOrDefault() * 1000}},
                    new Line {Point = new List<double> {node12.X.GetValueOrDefault() * 1000, node12.Y.GetValueOrDefault() * 1000, node12.Z.GetValueOrDefault() * 1000}}
                },
                CrossSection = "HEB120"
            });
            topologyDivision.AddChild(assembly2);

            ElementAssembly assembly3 = new ElementAssembly
            {
                Name = "B13_Column",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = _integrationBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.CornflowerBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B13_Assembly",
                        RelatedElement = beams.Find(x => x.Name == "B13")?.Id
                    }
                }
            };

            assembly3.CsgTree.Elements.Add(new Path
            {
                OffsetX = 30,
                OffsetY = -50,
                Geometry = new List<CsgGeoElement>
                {
                    new StartPolygon {Point = new List<double> {node9.X.GetValueOrDefault() * 1000, node9.Y.GetValueOrDefault() * 1000, node9.Z.GetValueOrDefault() * 1000}},
                    new Line {Point = new List<double> {node12.X.GetValueOrDefault() * 1000, node12.Y.GetValueOrDefault() * 1000, node12.Z.GetValueOrDefault() * 1000}}
                },
                CrossSection = "UPE100"
            });
            topologyDivision.AddChild(assembly3);

            ElementAssembly assembly4 = new ElementAssembly
            {
                Name = "B13_ColumnB",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = _integrationBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.CornflowerBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B13_Assembly",
                        RelatedElement = beams.Find(x => x.Name == "B13")?.Id
                    }
                }
            };

            assembly4.CsgTree.Elements.Add(new Path
            {
                Rotation = Math.PI,
                OffsetX = 30,
                OffsetY = 50,
                Geometry = new List<CsgGeoElement>
                {
                    new StartPolygon {Point = new List<double> {node9.X.GetValueOrDefault() * 1000, node9.Y.GetValueOrDefault() * 1000, node9.Z.GetValueOrDefault() * 1000}},
                    new Line {Point = new List<double> {node12.X.GetValueOrDefault() * 1000, node12.Y.GetValueOrDefault() * 1000, node12.Z.GetValueOrDefault() * 1000}}
                },
                CrossSection = "UPE100"
            });
            topologyDivision.AddChild(assembly4);

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);

            // Post object to Bimplus.
            HttpStatusCode result = _integrationBase.ApiCore.DtObjects.PutObject(topologyDivision);

            if (result == HttpStatusCode.OK)
            {
                result = _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                if (result == HttpStatusCode.OK)
                    NavigateToControl();
            }
            else
                MessageBoxHelper.ShowInformation("Can't create a TopologyDivision.");
        }

        private void DeleteModels_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetModelByName(_structuralAnalysisName);
            if (model != null)
            {
                bool deleted = _integrationBase.ApiCore.Divisions.DeleteDtoDivision(model.Id);
            }

            model = GetModelByName(_bimRoadName);
            if (model != null)
            {
                bool deleted = _integrationBase.ApiCore.Divisions.DeleteDtoDivision(model.Id);
            }

            model = GetModelByName(_structuralSteelName);
            if (model != null)
            {
                bool deleted = _integrationBase.ApiCore.Divisions.DeleteDtoDivision(model.Id);
            }

            NavigateToControl();
        }

        #endregion button events
    }
}
