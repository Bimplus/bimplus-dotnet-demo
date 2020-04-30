using AllplanBimplusDemo.Classes;
using AllplanBimplusDemo.WinForms;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlus.Sdk.Data.CSG;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Data.TenantDto;
using System;
using System.Collections.Generic;
using System.Windows;
using WPFWindows = System.Windows;
using System.Drawing;
using BimPlus.Sdk.Data.DbCore.Steel;
using Newtonsoft.Json;
using BimPlus.Sdk.Data.Converter;
using BimPlus.Sdk.Data.DbCore.Help;
using BimPlus.Sdk.Data.DbCore.Recess;
using BimPlus.Sdk.Data.DbCore.Connection;
using BimPlus.Sdk.Utilities.V2;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Class ConnectionsUserControl.
    /// </summary>
    public partial class ConnectionsUserControl : NotifyPropertyChangedUserControl
    {
		/// <summary>
		/// Constructor.
		/// </summary>
        public ConnectionsUserControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region private member

        private IntegrationBase _integrationBase;
        private WPFWindows.Window _parentWindow;

        private WebViewer _webViewer;

        private ElementAssembly _bar0;
        private ElementAssembly _bar1;
        private DtoDivision _model;

        private DtoConnections _savedDtoConnections;
        private Guid? _savedDtoConnectionsId;

        private string _bar0Name = "I600_Column_1";
        private string _bar1Name = "I600_Column_2";

        private string _modelName = "Objects_Connection";
        private string _divisionName = "RootNode";
        private string _connectionElementName = "Connection_Test";

        #endregion private member

        #region properties

        private bool _buttonsEnabled = false;

        /// <summary>
        /// Property ButtonsEnabled.
        /// </summary>
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

        /// <summary>
        /// Method LoadContent and WebViewer.
        /// </summary>
        /// <param name="integrationBase">IntegrationBase</param>
        /// <param name="parent">Parent window</param>
        public void LoadContent(IntegrationBase integrationBase, Window parent)
        {
            _integrationBase = integrationBase;
            _integrationBase.EventHandlerCore.DataLoaded += EventHandlerCore_DataLoaded;
            _parentWindow = parent;

            _webViewer = new WebViewer(integrationBase);
            NavigateToControl();

            _model = GetConnectionsModel(false);

            if (_model != null)
            {
                DeleteModelAndObject.IsEnabled = true;
                if (_savedDtoConnectionsId != null && _savedDtoConnectionsId != Guid.Empty)
                {
                    DeleteConnection.IsEnabled = true;
                    CreateConnection.IsEnabled = false;
                }
            }

            BimExplorer.Content = _webViewer;
        }

        /// <summary>
        /// Undload control.
        /// </summary>
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
            if (_savedDtoConnectionsId != null && _savedDtoConnectionsId != Guid.Empty)
                DeleteConnection.IsEnabled = true;

            ProgressWindow.Hide();
        }

        #endregion Bimplus events

        #region private methods

        private void NavigateToControl()
        {
            ButtonsEnabled = false;
            ProgressWindow.Text = "Load BIM Explorer.";
            ProgressWindow.Show();
            _webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);
        }

        private DtoDivision GetConnectionsModel(bool createModel = true)
        {
            DtoDivision model = null;

            ProgressWindow.Text = "Get Connections model.";
            ProgressWindow.Show();
            try
            {
                model = _integrationBase.ApiCore.Divisions.GetProjectDivisions(_integrationBase.CurrentProject.Id)?.Find(x => x.Name == _modelName);

                if (createModel)
                {
                    if (model == null)
                        model = _integrationBase.ApiCore.Divisions.CreateModel(_integrationBase.CurrentProject.Id, new DtoDivision { Name = _modelName });

                    if (model != null)
                    {
                        if (model.TopologyDivisionId == null)
                        {
                            // Create main root TopologyDivision node.
                            DtObject topologyDivision = _integrationBase.ApiCore.DtObjects.PostObject(new TopologyDivision
                            {
                                Name = _divisionName,
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
                                _model = model;
                            }
                        }
                    }
                }
            }
            finally
            {
                ProgressWindow.Hide();
            }

            _model = model;

            if (_model != null && model.TopologyDivisionId != null && model.TopologyDivisionId != Guid.Empty)
                SearchElements((Guid)_model.TopologyDivisionId);

            return _model;
        }

        private bool GetElementAssemblies(bool showNotFoundMessage)
        {
            bool hasDuplicateValues = false;

            if (_bar0 == null || _bar1 == null)
            {
                List<ElementAssembly> elementAssemblies =
                    _integrationBase.ApiCore.DtObjects.GetObjects<ElementAssembly>(_integrationBase.CurrentProject.Id, ObjectRequestProperties.Pset | ObjectRequestProperties.InternalValues);

                foreach (ElementAssembly elementAssembly in elementAssemblies)
                {
                    if (elementAssembly.GetDtObjectName() == _bar0Name)
                    {
                        if (_bar0 == null)
                            _bar0 = elementAssembly;
                        else
                        {
                            hasDuplicateValues = true;
                            break;
                        }
                    }
                    if (elementAssembly.GetDtObjectName() == _bar1Name)
                    {
                        if (_bar1 == null)
                            _bar1 = elementAssembly;
                        else
                        {
                            hasDuplicateValues = true;
                            break;
                        }
                    }
                }
            }

            if (hasDuplicateValues)
            {
                _bar0 = null; _bar1 = null;

                string message = "Multiple objects with the same name were found.";
                MessageBoxHelper.ShowInformation(message, _parentWindow);
            }
            else if (_bar0 == null || _bar1 == null)
            {
                if (showNotFoundMessage)
                    MessageBoxHelper.ShowInformation("ElementAssemblies were not found.", _parentWindow);
            }

            return _bar0 != null && _bar1 != null;
        }

        private bool SearchElements(Guid topologyDivisionId)
        {
            List<ElementAssembly> assemblies = _integrationBase.ApiCore.DtObjects.GetObjects<ElementAssembly>(topologyDivisionId, ObjectRequestProperties.InternalValues);

            if (assemblies?.Count > 1)
            {
                _bar0 = assemblies.Find(x => x.Name == _bar0Name);
                _bar1 = assemblies.Find(x => x.Name == _bar1Name);
            }
            else
            {
                _bar0 = null;
                _bar1 = null;
            }

            _savedDtoConnectionsId = _integrationBase.ApiCore.DtObjects.GetObjects<ProjectConnection>(_integrationBase.CurrentProject.Id, ObjectRequestProperties.InternalValues)
                ?.Find(x => x.Name == _connectionElementName)?.Id;

            return (_bar0 != null && _bar1 != null);
        }

        #endregion private methods

        #region button events

        private void CreateObjects_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetConnectionsModel();
            if (model == null)
                return;

            bool hasValues = GetElementAssemblies(false);
            if (hasValues)
            {
                MessageBoxHelper.ShowInformation("ElementAssemblies are already created.", _parentWindow);
                CreateConnection.IsEnabled = true;
                return;
            }

            ProgressWindow.Text = "Create objects.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;
            try
            {
                // Create a horizontal steel column by using Path with reference to IPE600.
                DtoCsgTree csg = new DtoCsgTree { Color = (uint)Color.Gray.ToArgb() };
                csg.Elements.Add(new Path
                {
                    Rotation = Math.PI / 2,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 0, 0}},
                        new Line {Point = new List<double> {1500, 0, 0}}
                    },
                    CrossSection = "IPE600"
                });

                ElementAssembly bar0 = new ElementAssembly
                {
                    Name = _bar0Name,
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = csg
                };

                // Test
                string jsonString = JsonConvert.SerializeObject(bar0);

                // Post object to Bimplus.
                _bar0 = _integrationBase.ApiCore.DtObjects.PostObject(bar0) as ElementAssembly;

                DtoCsgTree csg2 = new DtoCsgTree { Color = (uint)Color.Gray.ToArgb() };
                csg2.Elements.Add(new Path
                {
                    Rotation = Math.PI / 2,
                    OffsetY = -300,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0.0, 310, 0}},
                        new Line {Point = new List<double> {0.0, 1810, 0}}
                    },
                    CrossSection = "IPE600"
                });

                ElementAssembly bar1 = new ElementAssembly
                {
                    Name = _bar1Name,
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = csg2
                };

                // Test
                jsonString = JsonConvert.SerializeObject(bar1);

                // Post object to Bimplus.
                _bar1  = _integrationBase.ApiCore.DtObjects.PostObject(bar1) as ElementAssembly;

                if (_bar0 == null || _bar1 == null)
                {
                    MessageBoxHelper.ShowInformation("Not all objects could be created.", _parentWindow);
                }
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                    DeleteModelAndObject.IsEnabled = true;
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        private void CreateConnection_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = GetConnectionsModel();
            if (model == null)
                return;

            bool hasValues = GetElementAssemblies(false);

            if (!hasValues)
            {
                MessageBoxHelper.ShowInformation("Please create the objects first.", _parentWindow);
                return;
            }

            string propertyName = "ConnectionChild-Element";

            ProgressWindow.Text = "Create connection";
            ProgressWindow.Show();
            bool hideProgressWindow = true;
            try
            {
                DtoCsgTree csgTree = new DtoCsgTree { Color = (uint)Color.LightSteelBlue.ToArgb() };
                csgTree.Elements.Add(new Path
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 300, 6.5}},
                        new Line {Point = new List<double> {0, 300, 15.5}},
                    }
                });

                csgTree.Elements.Add(new OuterContour
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, -40}},
                        new Line {Point = new List<double> {290, -40}},
                        new Line {Point = new List<double> {290, -550}},
                        new Line {Point = new List<double> {0, -550}},
                        new Line {Point = new List<double> {0.0, -40}},
                    }
                });

                SteelPlate Slab = new SteelPlate
                {
                    Name = "Slab",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = csgTree
                };
                Slab.AddProperty(TableNames.tabAttribConstObjInstObj, propertyName, _bar0.Id);

                DtoCsgTree rectangle = new DtoCsgTree { Color = (uint)Color.IndianRed.ToArgb() };
                rectangle.Elements.Add(new Path
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {40, 300.0, 15.5}},
                        new Line {Point = new List<double> {550, 300.0, 15.5}}
                    }
                });

                rectangle.Elements.Add(new OuterContour
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {20, 0.0}},
                        new Line {Point = new List<double> {0.0, 0.0}},
                        new Line {Point = new List<double> {0, 10}},
                        new Line {Point = new List<double> {20, 0.0}}
                    }
                });

                Weld Weld_1 = new Weld
                {
                    Name = "Weld_1",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = rectangle
                };

                Weld_1.AddProperty(TableNames.tabAttribConstObjInstObj, propertyName, _bar0.Id);

                Weld Weld_2 = new Weld
                {
                    Name = "Weld_2",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = rectangle
                };

                TmpMatrix matrix = new TmpMatrix
                {
                    Values = new[]
                    {
                        1, 0.0, 0.0, 0.0,
                        0.0, 1, 0.0, 0.0,
                        0.0, 0.0, -1,  9, //-10
                        0.0, 0.0, 0.0, 1
                    }
                };

                Weld_2.AddProperty(TableNames.tabAttribConstObjInstObj, propertyName, _bar0.Id);
                Weld_2.AddProperty("element", "matrix", matrix);

                DtoCsgTree cabin = new DtoCsgTree { Color = (uint)Color.DarkBlue.ToArgb() };
                cabin.Elements.Add(new Path
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 300, 30}},
                        new Line {Point = new List<double> {0, 300, 40}},
                    }
                });

                cabin.Elements.Add(new OuterContour
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0.0, -10.0}},
                        new Line {Point = new List<double> { 8.666, -5}},
                        new Line {Point = new List<double> { 8.666, 5}},
                        new Line {Point = new List<double> {0.0, 10}},
                        new Line {Point = new List<double> { -8.666, 5}},
                        new Line {Point = new List<double> { -8.666, -5}},
                        new Line {Point = new List<double> {0.0, -10}}
                    }
                });

                DtoCsgTree CircularTree = new DtoCsgTree { Color = (uint)Color.Gray.ToArgb() };
                CircularTree.Elements.Add(new Path
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 300, -20}},
                        new Line {Point = new List<double> {0, 300, 30}}
                    },
                    CrossSection = "RD8"
                });

                ChildGeometryObject Cabin1 = new ChildGeometryObject
                {
                    Name = "Cabin1",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = cabin
                };

                TmpMatrix matrix1 = new TmpMatrix
                {
                    Values = new[]
                    {
                        1, 0.0, 0.0, 100.0,
                        0.0, 1, 0.0, 200.0,
                        0.0, 0.0, 1, 0.0,
                        0.0, 0.0, 0.0, 1
                    }
                };

                Cabin1.AddProperty("element", "matrix", matrix1);

                ChildGeometryObject Circular1 = new ChildGeometryObject
                {
                    Name = "Circular1",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = CircularTree
                };

                Circular1.AddProperty("element", "matrix", matrix1);

                MechanicalFastener Screw_1 = new MechanicalFastener
                {
                    Name = "Screw_1",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                };

                Screw_1.Children = new List<DtObject>
                {
                    Cabin1,
                    Circular1
                };

                ChildGeometryObject Cabin2 = new ChildGeometryObject
                {
                    Name = "Cabin2",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = cabin
                };

                TmpMatrix matrix2 = new TmpMatrix
                {
                    Values = new[]
                    {
                        1, 0.0, 0.0, 500,
                        0.0, 1, 0.0, 100.0,
                        0.0, 0.0, 1, 0.0,
                        0.0, 0.0, 0.0, 1
                    }
                };

                Cabin2.AddProperty("element", "matrix", matrix2);

                ChildGeometryObject Circular2 = new ChildGeometryObject
                {
                    Name = "Circular2",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = CircularTree
                };

                Circular2.AddProperty("element", "matrix", matrix2);

                MechanicalFastener Screw_2 = new MechanicalFastener
                {
                    Name = "Screw_2",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                };

                Screw_2.Children = new List<DtObject>
                {
                    Cabin2,
                    Circular2
                };

                ChildGeometryObject Cabin3 = new ChildGeometryObject
                {
                    Name = "Cabin3",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = cabin
                };

                TmpMatrix matrix3 = new TmpMatrix
                {
                    Values = new[]
                    {
                        1, 0.0, 0.0, 100,
                        0.0, 1, 0.0, 100,
                        0.0, 0.0, 1, 0.0,
                        0.0, 0.0, 0.0, 1
                    }
                };

                Cabin3.AddProperty("element", "matrix", matrix3);

                ChildGeometryObject Circular_3 = new ChildGeometryObject
                {
                    Name = "Circular_3",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = CircularTree
                };

                Circular_3.AddProperty("element", "matrix", matrix3);

                MechanicalFastener Screw_3 = new MechanicalFastener
                {
                    Name = "Screw_3",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                };

                Screw_3.Children = new List<DtObject>
                {
                    Cabin3,
                    Circular_3
                };

                ChildGeometryObject Cabin4 = new ChildGeometryObject
                {
                    Name = "Cabin4",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = cabin
                };

                TmpMatrix matrix4 = new TmpMatrix
                {
                    Values = new[]
                    {
                        1, 0.0, 0.0, 500,
                        0.0, 1, 0.0, 200,
                        0.0, 0.0, 1, 0.0,
                        0.0, 0.0, 0.0, 1
                    }
                };

                Cabin4.AddProperty("element", "matrix", matrix4);

                ChildGeometryObject Circular_4 = new ChildGeometryObject
                {
                    Name = "Circular_4",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    CsgTree = CircularTree
                };

                Circular_4.AddProperty("element", "matrix", matrix4);

                MechanicalFastener Screw_4 = new MechanicalFastener
                {
                    Name = "Screw_4",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                };

                Screw_4.Children = new List<DtObject>
                {
                    Cabin4,
                    Circular_4
                };

                DtoCsgTree Hole = new DtoCsgTree { Color = (uint)Color.Gray.ToArgb() };
                Hole.Elements.Add(new Path
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 300, -100}},
                        new Line {Point = new List<double> {0, 300, 200}}
                    },
                    CrossSection = "RD8"
                });

                Opening Hole_1 = new Opening
                {
                    Name = "Hole_1",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = Hole
                };

                Hole_1.AddProperty("element", "matrix", matrix1);

                Opening Hole_2 = new Opening
                {
                    Name = "Hole_2",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = Hole
                };

                Hole_2.AddProperty("element", "matrix", matrix2);

                Opening Hole_3 = new Opening
                {
                    Name = "Hole_3",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = Hole
                };

                Hole_3.AddProperty("element", "matrix", matrix3);

                Opening Hole_4 = new Opening
                {
                    Name = "Hole_4",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = Hole
                };

                Hole_4.AddProperty("element", "matrix", matrix4);

                DtoConnections connections = new DtoConnections
                {
                    ConnectionElement = new ConnectionElement(),
                    ElementIds = new List<Guid>()
                };

                connections.ConnectionElement.Name = _connectionElementName;
                connections.ConnectionElement.Description = "Connection_Part_Of_Test";
                connections.ElementIds.Add(_bar0.Id);
                connections.ElementIds.Add(_bar1.Id);

                connections.ConnectionElement.Children = new List<DtObject> { Slab, Weld_1, Weld_2, Screw_1, Screw_2, Screw_3, Screw_4, Hole_1, Hole_2, Hole_3, Hole_4 };

                string jsonString = JsonConvert.SerializeObject(connections);
                _savedDtoConnections = _integrationBase.ApiCore.DtoConnection.CreateConnection(_integrationBase.CurrentProject.Id, connections);

                if (_savedDtoConnections == null)
                {
                    MessageBoxHelper.ShowInformation("DtoConnections could not be generated.", _parentWindow);
                }
                else
                {
                    _savedDtoConnectionsId = _savedDtoConnections.Id;
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                    CreateConnection.IsEnabled = false;
                    DeleteConnection.IsEnabled = true;
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        private void DeleteConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_model == null && _savedDtoConnections?.Id != Guid.Empty)
                GetConnectionsModel(true);

            bool deleted = false;
            if (_savedDtoConnectionsId != null && _savedDtoConnectionsId != Guid.Empty)
            {
                deleted = _integrationBase.ApiCore.DtoConnection.DeleteConnections((Guid)_savedDtoConnectionsId);
                if (deleted)
                {
                    _savedDtoConnections = null;
                    _savedDtoConnectionsId = null;
                }
            }
            else
            {
                MessageBoxHelper.ShowInformation("DtoConnections not found.", _parentWindow);
            }

            if (deleted)
                NavigateToControl();

            DeleteConnection.IsEnabled = !deleted;
            CreateConnection.IsEnabled = _savedDtoConnectionsId == null;
        }

        private void DeleteModelAndObject_Click(object sender, RoutedEventArgs e)
        {
            bool hasValues = GetElementAssemblies(false);
            if (hasValues)
            {
                GetConnectionsModel(false);

                if (_savedDtoConnectionsId != null && _savedDtoConnectionsId != Guid.Empty)
                {
                    bool deleted = _integrationBase.ApiCore.DtoConnection.DeleteConnections((Guid)_savedDtoConnectionsId);
                    if (deleted)
                    {
                        _savedDtoConnections = null;
                        _savedDtoConnectionsId = null;
                        DeleteConnection.IsEnabled = false;
                    }
                }

                bool bar0Deleted = _integrationBase.ApiCore.DtObjects.DeleteObject(_bar0.Id);
                if (bar0Deleted)
                    _bar0 = null;

                bool bar1Deleted = _integrationBase.ApiCore.DtObjects.DeleteObject(_bar1.Id);
                if (bar1Deleted)
                    _bar1 = null;

                if (!bar0Deleted || !bar1Deleted)
                    MessageBoxHelper.ShowInformation("Not all ElementAssemblies could be deleted.", _parentWindow);

                if (_model != null)
                {
                    bool modelDeleted = _integrationBase.ApiCore.Divisions.DeleteDtoDivision(_model.Id);
                    if (modelDeleted)
                        _model = null;

                    DeleteConnection.IsEnabled = false;
                    DeleteModelAndObject.IsEnabled = false;
                }
            }

            NavigateToControl();
        }

        #endregion button events

    }
}
