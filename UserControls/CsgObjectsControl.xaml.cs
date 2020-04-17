using AllplanBimplusDemo.Classes;
using BimPlus.Client.Integration;
using BimPlus.Client.WebControls.WPF;
using BimPlus.Sdk.Data.Converter;
using BimPlus.Sdk.Data.CSG;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.DbCore.Analysis;
using BimPlus.Sdk.Data.DbCore.Building;
using BimPlus.Sdk.Data.DbCore.Finishings;
using BimPlus.Sdk.Data.DbCore.Help;
using BimPlus.Sdk.Data.DbCore.Reinforcement;
using BimPlus.Sdk.Data.DbCore.Steel;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Data.Geometry;
using BimPlus.Sdk.Data.GeometryTemplates;
using BimPlus.Sdk.Data.TenantDto;
using Nemetschek.NUtilLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using BimPlus.Sdk.Data.StructuralLoadResource;
using WPFWindows = System.Windows;
using IdeaRS.OpenModel;
using IdeaRS.OpenModel.CrossSection;
using IdeaRS.OpenModel.Geometry3D;
using IdeaRS.OpenModel.Model;
using IdeaRS.OpenModel.Result;
using IOM.SteelFrame;
// ReSharper disable IdentifierTypo
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable LocalizableElement
// ReSharper disable StringLiteralTypo

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for CsgObjectsControl.xaml
    /// </summary>
    public partial class CsgObjectsControl : NotifyPropertyChangedUserControl
    {
        public CsgObjectsControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region private member

        private IntegrationBase _integrationBase;
        private WPFWindows.Window _parentWindow;

        private WebViewer _webViewer;

        #endregion private member

        #region properties

        private bool _buttonsEnabled;

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

        public void LoadContent(IntegrationBase integrationBase, WPFWindows.Window parent)
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
            _webViewer?.Dispose();
            _integrationBase.EventHandlerCore.DataLoaded -= EventHandlerCore_DataLoaded;
        }

        #endregion public methods

        #region Bimplus events

        private void EventHandlerCore_DataLoaded(object sender, BimPlus.Client.BimPlusEventArgs e)
        {
            ProgressWindow.Hide();
            ButtonsEnabled = true;
        }

        #endregion Bimplus events

        #region private methods

        /// <summary>
        /// Find existing model or create the model.
        /// </summary>
        /// <returns></returns>
        public DtoDivision CreateCsgModel(string modelName = "CsgGeometryModel")
        {
            var model = _integrationBase.ApiCore.Divisions.GetProjectDivisions(_integrationBase.CurrentProject.Id)?.Find(x => x.Name == modelName);

            if (model == null)
            {
                model = _integrationBase.ApiCore.Divisions.CreateModel(_integrationBase.CurrentProject.Id, new DtoDivision { Name = modelName });
                MessageBoxHelper.ShowInformation(model == null
                    ? $"Could not create a new model '{modelName}'"
                    : $"model '{modelName}' successfully created");
            }

            if (model == null || model.TopologyDivisionId.GetValueOrDefault(Guid.Empty) != Guid.Empty) 
                return model;

            // Create main root TopologyDivision node.
            DtObject topologyDivision = _integrationBase.ApiCore.DtObjects.PostObject(new TopologyDivision
            {
                Name = $"RootNode{modelName}",
                Division = model.Id,
                Parent = _integrationBase.CurrentProject.Id
            });

            if (topologyDivision == null || topologyDivision.Id == Guid.Empty)
            {
                string message = "Could not create TopologyDivision object.";
                MessageBoxHelper.ShowInformation(message, _parentWindow);

                return null;
            }
            model.TopologyDivisionId = topologyDivision.Id;

            return model;
        }

        private static DtoTemplateArticle Support(bool vx, bool vy, bool vz, bool tx, bool ty, bool tz)
        {
            DtoCsgTree csg = new DtoCsgTree { Color = (uint)Color.LimeGreen.ToArgb() };
            if (vx && tx)
                csg.Elements.Add(new Tube
                {
                    Radius = 4,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 0, 0}},
                        new Line {Point = new List<double> {0, 0.0, -80}},
                        new StartPolygon {Point = new List<double> {60, 0, -80}},
                        new Line {Point = new List<double> {60, 60, -80}},
                        new Line {Point = new List<double> {-60, 60, -80}},
                        new Line {Point = new List<double> {-60, -60, -80}},
                        new Line {Point = new List<double> {60, -60, -80}},
                        new Line {Point = new List<double> {60, 00, -80}},
                        new StartPolygon {Point = new List<double> {60, 0, -80}},
                        new Round {Point = new List<double> {60, 60, -80}, Radius = 60},
                        new Round {Point = new List<double> {-60, 60, -80}, Radius = 60},
                        new Round {Point = new List<double> {-60, -60, -80}, Radius = 60},
                        new Round {Point = new List<double> {60, -60, -80}, Radius = 60},
                        new Round {Point = new List<double> {60, 0, -80}, Radius = 60}
                    }
                });
            else if (vx)
                csg.Elements.Add(new Tube
                {
                    Radius = 4,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 0, 0}},
                        new Line {Point = new List<double> {0, 0, -80}},
                        new StartPolygon {Point = new List<double> {60, 0, -80}},
                        new Line {Point = new List<double> {60, 60, -80}},
                        new Line {Point = new List<double> {-60, 60, -80}},
                        new Line {Point = new List<double> {-60, -60, -80}},
                        new Line {Point = new List<double> {60, -60, -80}},
                        new Line {Point = new List<double> {60, 00, -80}}
                    }
                });
            else if (tx)
                csg.Elements.Add(new Tube
                {
                    Radius = 4,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 0, 0}},
                        new Line {Point = new List<double> {0, 0.0, -80}},
                        new StartPolygon {Point = new List<double> {60, 0, -80}},
                        new Round {Point = new List<double> {60, 60, -80}, Radius = 60},
                        new Round {Point = new List<double> {-60, 60, -80}, Radius = 60},
                        new Round {Point = new List<double> {-60, -60, -80}, Radius = 60},
                        new Round {Point = new List<double> {60, -60, -80}, Radius = 60},
                        new Round {Point = new List<double> {60, 0, -80}, Radius = 60}
                    }
                });

            List<DtoCsgTree> csgGeometry = new List<DtoCsgTree> { csg };

            DtoTemplateArticle template = new DtoTemplateArticle
            {
                Identifier = $"Support_{vx}_{vy}_{vz}_{tx}_{ty}_{tz}",
                LevelOfDetail = 0,
                //ThreeJs = new List<DtoGeometry>(1),
                CsgThreeJs = csgGeometry
            };

            return template;

        }

        private void NavigateToControl()
        {
            ButtonsEnabled = false;
            ProgressWindow.Text = "Load BIM Explorer.";
            ProgressWindow.Show();
            _webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);
        }

        #endregion private methods

        #region button events

        /// <summary>
        /// Create Tube by given Geometry Information. 
        /// Tube as List of CsgGeoElement's.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateTube_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel();
            if (model == null)
                return;

            ProgressWindow.Text = "Create Tube.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                DtoCsgTree csg = new DtoCsgTree { Color = (uint)Color.Red.ToArgb() };
                csg.Elements.Add(new Tube
                {
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {900, 0, 0}},
                        new Line {Point = new List<double> {1000, 0, 0}},
                        new Line {Point = new List<double> {0, 1000, 0}},
                        new Line {Point = new List<double> {-1000, 0, 0}},
                        new Line {Point = new List<double> {0, -1000, 0}},
                        new Line {Point = new List<double> {1000, 0, 50}},
                        new Line {Point = new List<double> {900, 0, 50}},
                        new StartPolygon {Point = new List<double> {900, 0, 200}},
                        new Round {Point = new List<double> {1000, 0, 200}, Radius = 20},
                        new Round {Point = new List<double> {0, 1000, 200}, Radius = 20},
                        new Round {Point = new List<double> {-1000, 0, 200}, Radius = 20},
                        new Round {Point = new List<double> {0, -1000, 200}, Radius = 20},
                        new Round {Point = new List<double> {1000, 0, 250}, Radius = 20},
                        new Line {Point = new List<double> {900, 0, 250}},
                        new StartPolygon {Point = new List<double> {900, 0, 400}},
                        new Round {Point = new List<double> {1000, 0, 400}, Radius = 30},
                        new Line {Point = new List<double> {0, 1000, 400}},
                        new Round {Point = new List<double> {-1000, 0, 400}, Radius = 30},
                        new Line {Point = new List<double> {0, -1000, 400}},
                        new Round {Point = new List<double> {1000, 0, 450}, Radius = 30},
                        new Line {Point = new List<double> {900, 0, 450}}
                    },
                    Radius = 10
                });

                ReinforcingBar bars = new ReinforcingBar
                {
                    Name = "TestBars",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = csg
                };

                // Test.
                string jsonString = JsonConvert.SerializeObject(bars);

                // Post object to Bimplus.
                DtObject existingObject = _integrationBase.ApiCore.DtObjects.PostObject(bars);
                if (existingObject == null)
                {
                    string message = "Could not create 'UndefinedBars' object.";
                    MessageBoxHelper.ShowInformation(message, _parentWindow);
                }
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        /// <summary>
        /// Create 3D body by given CSG Geometry information.
        /// DtoCsgTree as Path, Inner- and OuterContour.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateContour_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel();
            if (model == null)
                return;

            ProgressWindow.Text = "Create contour.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                DtoCsgTree dtoCsgTree = new DtoCsgTree { Color = (uint)Color.Green.ToArgb() };
                dtoCsgTree.Elements.Add(new Path
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 0, 0}},
                        new Line {Point = new List<double> {0, 0, 2500}},
                        new Line {Point = new List<double> {400, 400, 3000}}
                    }
                });

                dtoCsgTree.Elements.Add(new OuterContour
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {1000, 0, 0}},
                        new Line {Point = new List<double> {0, -1000, 0}},
                        new Line {Point = new List<double> {-1000, 0, 0}},
                        new Line {Point = new List<double> {0, 1000, 0}},
                        new Line {Point = new List<double> {1000, 0, 0}},
                    }
                });

                dtoCsgTree.Elements.Add(new InnerContour
                {
                    Rotation = 0,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {500, -500, 0}},
                        new Round {Point = new List<double> {-500, -500, 0}, Radius = 30},
                        new Round {Point = new List<double> {-500, 500, 0}, Radius = 30},
                        new Round {Point = new List<double> {500, 500, 0}, Radius = 30},
                        new Round {Point = new List<double> {500, -500, 0}, Radius = 30}
                    }
                });

                Column column = new Column
                {
                    Name = "TestColumn",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = dtoCsgTree
                };

                // Test.
                string jsonString = JsonConvert.SerializeObject(column);

                // Post object to Bimplus.
                DtObject existingObject = _integrationBase.ApiCore.DtObjects.PostObject(column);

                if (existingObject == null)
                {
                    string message = "Could not create 'Column' object.";
                    MessageBoxHelper.ShowInformation(message, _parentWindow);
                }
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        /// <summary>
        /// Create steel column with reference to IPE CrossSectionDefinition.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateIPE200_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel();
            if (model == null)
                return;

            ProgressWindow.Text = "Create IPE200.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                // Create a horizontal steel column by using Path with reference to IPE200.
                DtoCsgTree dtoCsgTree = new DtoCsgTree { Color = (uint)Color.MediumBlue.ToArgb() };
                dtoCsgTree.Elements.Add(new Path
                {
                    Rotation = Math.PI / 2,
                    Geometry = new List<CsgGeoElement>
                    {
                        new StartPolygon {Point = new List<double> {0, 0, 3000}},
                        new Line {Point = new List<double> {2500, 0, 3000}}
                    },
                    CrossSection = "IPE200"
                });

                Steel steel = new Steel
                {
                    Name = "IPE200_Column",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    CsgTree = dtoCsgTree
                };

                // Test.
                string jsonString = JsonConvert.SerializeObject(steel);

                // Post object to Bimplus.
                DtObject existingObject = _integrationBase.ApiCore.DtObjects.PostObject(steel);

                if (existingObject == null)
                {
                    string message = "Could not create 'Column' object.";
                    MessageBoxHelper.ShowInformation(message, _parentWindow);
                }
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        /// <summary>
        /// Create Faces (also with holes) by using 'Mesh' Property DbGeometry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateFaces_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel();
            if (model == null)
                return;

            ProgressWindow.Text = "Create faces.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                WallCovering wallCovering = new WallCovering
                {
                    Name = "TestCovering",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    Mesh = new DbGeometry
                    {
                        Color = (uint)Color.DarkOrange.ToArgb(),
                        Vertices = new List<double>
                        {
                            0,0,0,          // Point 0
                            0,0,1000,       // Point 1
                            2000,0,0,       // Point 2
                            2000,0,500,     // Point 3
                            3000,0,0,       // Point 4
                            2000,-1000,0,   // Point 5
                            2000,-1000,500, // Point 6
                            2000, -200,200, // Point 7 
                            2000, -200,400, // Point 8
                            2000, -800,200, // Point 9
                            2000, -800,400  // Point 10
                        },

                        Faces = new List<uint>
                        {
                            4, 0, 1, 3, 2,                      // Face 1 with 4 edges
                            10, 2, 3, 6, 5, 7, 8, 10, 9, 7, 5,  // Face 2 with 8+2 edges (including one hole)
                            3, 2, 4, 5                          // Face 3 with 3 edges
                        }
                    }
                };

                // Test.
                string jsonString = JsonConvert.SerializeObject(wallCovering);

                // Post object to Bimplus.
                DtObject existingObject = _integrationBase.ApiCore.DtObjects.PostObject(wallCovering);

                if (existingObject == null)
                {
                    string message = "Could not create 'Column' object.";
                    MessageBoxHelper.ShowInformation(message, _parentWindow);
                }
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }

        }

        /// <summary>
        /// Create 3D body by using CBaseElementPolyeder, Point, Edges and Faces.
        /// Nemetschek.NUtilLibrary in PPAttribPictureType3.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateBasePolyeder_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel();
            if (model == null)
                return;

            ProgressWindow.Text = "Create BasePolyeder.";
            ProgressWindow.Show();
            bool hideProgressWindow = true;

            try
            {
                CBaseElementPolyeder baseElementPolyeder = new CBaseElementPolyeder();
                baseElementPolyeder.point.Add(new CElementPoint(0, 0, 0));
                baseElementPolyeder.point.Add(new CElementPoint(250, 0, 0));
                baseElementPolyeder.point.Add(new CElementPoint(250, 5000, 0));
                baseElementPolyeder.point.Add(new CElementPoint(0, 5000, 0));
                baseElementPolyeder.point.Add(new CElementPoint(0, 0, 600));
                baseElementPolyeder.point.Add(new CElementPoint(250, 0, 600));
                baseElementPolyeder.point.Add(new CElementPoint(250, 5000, 600));
                baseElementPolyeder.point.Add(new CElementPoint(0, 5000, 600));

                baseElementPolyeder.edge.Add(new CElementEdge(4, 1));
                baseElementPolyeder.edge.Add(new CElementEdge(1, 2));
                baseElementPolyeder.edge.Add(new CElementEdge(2, 3));
                baseElementPolyeder.edge.Add(new CElementEdge(3, 4));
                baseElementPolyeder.edge.Add(new CElementEdge(5, 8));
                baseElementPolyeder.edge.Add(new CElementEdge(8, 7));
                baseElementPolyeder.edge.Add(new CElementEdge(7, 6));
                baseElementPolyeder.edge.Add(new CElementEdge(6, 5));
                baseElementPolyeder.edge.Add(new CElementEdge(1, 5));
                baseElementPolyeder.edge.Add(new CElementEdge(2, 6));
                baseElementPolyeder.edge.Add(new CElementEdge(3, 7));
                baseElementPolyeder.edge.Add(new CElementEdge(4, 8));

                baseElementPolyeder.face.Add(new CElementFace { 1, 2, 3, 4 });
                baseElementPolyeder.face.Add(new CElementFace { 5, 6, 7, 8 });
                baseElementPolyeder.face.Add(new CElementFace { -1, 12, -5, -9 });
                baseElementPolyeder.face.Add(new CElementFace { -2, 9, -8, -10 });
                baseElementPolyeder.face.Add(new CElementFace { -3, 10, -7, -11 });
                baseElementPolyeder.face.Add(new CElementFace { -4, 11, -6, -12 });

                SmartSymbol smartSymbol = new SmartSymbol
                {
                    Name = "TestCube",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    BytePolyeder = baseElementPolyeder
                };

                // Post object to Bimplus.
                DtObject existingObject = _integrationBase.ApiCore.DtObjects.PostObject(smartSymbol);

                if (existingObject == null)
                {
                    string message = "Could not create 'SmartSymbol' object.";
                    MessageBoxHelper.ShowInformation(message, _parentWindow);
                }
                else
                {
                    _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                    hideProgressWindow = false;
                    NavigateToControl();
                }
            }
            finally
            {
                if (hideProgressWindow)
                    ProgressWindow.Hide();
            }
        }

        /// <summary>
        /// Create DtoTemplateArticle and objects StructuralPointReactions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateTemplate_Click(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel();
            if (model == null)
                return;

            int postedCount = 0;

            ProgressWindow.Text = "Create template.";
            ProgressWindow.Show();

            try
            {
                DtoTemplateArticle csg1 = Support(true, false, false, false, false, true);
                Guid article1Id = _integrationBase.ApiCore.GeometryTemplate.Create(csg1);

                DtoTemplateArticle csg2 = Support(false, true, false, true, false, true);
                Guid article2Id = _integrationBase.ApiCore.GeometryTemplate.Create(csg2);

                DtoTemplateArticle csg3 = Support(true, true, false, true, false, true);
                Guid article3Id = _integrationBase.ApiCore.GeometryTemplate.Create(csg3);

                TmpMatrix matrix = new TmpMatrix
                {
                    OffsetX = 1000,
                    OffsetY = 1000
                };

                StructuralPointReaction support = new StructuralPointReaction
                {
                    Name = "TestSupport",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = _integrationBase.CurrentProject.Id,
                    Template = article1Id,
                    Matrix = matrix
                };

                DtObject s1 = _integrationBase.ApiCore.DtObjects.PostObject(support);
                if (s1 != null)
                    postedCount++;

                support.Matrix.OffsetX = 2000;
                DtObject s2 = _integrationBase.ApiCore.DtObjects.PostObject(support);
                if (s2 != null)
                    postedCount++;

                support.Matrix.OffsetY = 2000;
                support.Template = article2Id;
                DtObject s3 = _integrationBase.ApiCore.DtObjects.PostObject(support);
                if (s3 != null)
                    postedCount++;

                support.Matrix.OffsetX = 1000;
                support.Template = article2Id;
                DtObject s4 = _integrationBase.ApiCore.DtObjects.PostObject(support);
                if (s4 != null)
                    postedCount++;

                support.Matrix.OffsetY = 3000;
                support.Template = article3Id;
                DtObject s5 = _integrationBase.ApiCore.DtObjects.PostObject(support);
                if (s5 != null)
                    postedCount++;

                support.Matrix.OffsetX = 2000;
                support.Template = article3Id;
                DtObject s6 = _integrationBase.ApiCore.DtObjects.PostObject(support);
                if (s6 != null)
                    postedCount++;
            }
            finally
            {
                ProgressWindow.Hide();
            }

            // Test.
            //List<StructuralPointReaction> result = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointReaction>(_integrationBase.CurrentProject.Id, true, true);

            if (postedCount != 6)
            {
                string message = "Could not create all 'StructuralPointReaction' objects.";
                MessageBoxHelper.ShowInformation(message);
            }
            else
            {
                NavigateToControl();
            }
        }

        /// <summary>
        /// Import 'IdeaStatica' TestData into BimPlus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdeaStatica_OnClick(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel("IdeaStatica");
            if (model == null)
                return;

            ProgressWindow.Text = "Import IdeaStaticaExampleData.";
            ProgressWindow.Show();

            try
            {
                // create IOM and results
                OpenModel example = Example.CreateIOM();
                OpenModelResult result = Helpers.GetResults();

                var nodes = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(
                    model.TopologyDivisionId.GetValueOrDefault(), false, false, true);

                if (nodes == null || nodes.Count < 9)
                {
                    // Create Nodes (StructuralPointConnections)
                    var node = new TopologyItem
                    {
                        Name = "Nodes",
                        Parent = model.TopologyDivisionId,
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        Children = new List<DtObject>(example.Point3D.Count)
                    };

                    foreach (var pt in example.Point3D)
                    {
                        node.Children.Add(new StructuralPointConnection
                        {
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            Name = pt.Name,
                            NodeId = pt.Id,
                            X = pt.X,
                            Y = pt.Y,
                            Z = pt.Z,
                            AppliedCondition = new BoundaryNodeCondition("", false, false, false, false, false, false)
                        });
                    }

                    node = (TopologyItem) _integrationBase.ApiCore.DtObjects.PostObject(node);
                    nodes = node.Children.OfType<StructuralPointConnection>().ToList();
                }

                // Create CurveMembers
                var curveMembers = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(
                    model.TopologyDivisionId.GetValueOrDefault(), false, false, true);
                if (curveMembers.Count < 10)
                {
                    var beams = new TopologyItem
                    {
                        Name = "3DSegments",
                        Parent = model.TopologyDivisionId,
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        Children = new List<DtObject>(example.LineSegment3D.Count)
                    };

                    foreach (var cm in example.LineSegment3D)
                    {
                        beams.Children.Add(new StructuralCurveMember
                        {
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            Name = $"M{cm.Id}",
                            ConnectedBy = new List<RelConnectsStructuralMember>(2)
                            {
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R1",
                                    RelatedStructuralConnection =
                                        nodes.Find(x => x.NodeId == cm.StartPoint.Element.Id)?.Id
                                },
                                new RelConnectsStructuralMember
                                {
                                    OrderNumber = 1,
                                    Name = "R1",
                                    RelatedStructuralConnection =
                                        nodes.Find(x => x.NodeId == cm.EndPoint.Element.Id)?.Id
                                }
                            }
                        });
                    }
                    beams = (TopologyItem) _integrationBase.ApiCore.DtObjects.PostObject(beams);
                    curveMembers = beams.Children.OfType<StructuralCurveMember>().ToList();
                }

                // create Assemblies
                var assemblies = new TopologyItem
                {
                    Name = "Assemblies",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    Children = new List<DtObject>(example.Member1D.Count)
                };
                foreach (var member in example.Member1D)
                {
                    //var elements = member.Elements1D.OfType<Element1D>().ToList();
                    var path = new BimPlus.Sdk.Data.CSG.Path
                    {
                        Geometry = new List<CsgGeoElement>()
                    };
                    CrossSection crossSection = null;
                    foreach (var re in member.Elements1D)
                    {
                        if (!(re.Element is Element1D element))
                            continue;

                        if (crossSection == null)
                            crossSection = element.CrossSectionBegin.Element as CrossSection;

                        if (!(element.Segment.Element is LineSegment3D line)) 
                            continue;

                        if (path.Geometry.Count == 0)
                        {
                            if (crossSection != null)
                                path.CrossSection = crossSection.Name;

                            path.Rotation = element.RotationRx;
                            path.OffsetX = element.EccentricityBeginX;
                            path.OffsetY = element.EccentricityBeginY;
                            if (line.StartPoint.Element is Point3D sp)
                                path.Geometry.Add(new StartPolygon
                                    {Point = new List<double> {sp.X * 1000F, sp.Y * 1000F, sp.Z * 1000F}});
                        }

                        if (line.EndPoint.Element is Point3D ep)
                            path.Geometry.Add(new Line
                                {Point = new List<double> {ep.X * 1000F, ep.Y * 1000F, ep.Z * 1000F}});
                    }

                    assemblies.Children.Add(new ElementAssembly
                    {
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        Name = member.Name,
                        CsgTree = new DtoCsgTree
                        {
                            Color = (uint)Color.CadetBlue.ToArgb(),
                            Elements = new List<CsgElement>(1){ path }
                        }
                    });
                }
                assemblies = (TopologyItem)_integrationBase.ApiCore.DtObjects.PostObject(assemblies);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                // convert geometry templates
                _integrationBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                // refresh webviewer
                NavigateToControl();
            }

        }
        #endregion button events
    }
}
