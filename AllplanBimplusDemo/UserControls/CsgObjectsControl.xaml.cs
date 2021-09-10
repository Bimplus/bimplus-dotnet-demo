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
using System.Globalization;
using System.Linq;
using System.Windows;
using BimPlus.Sdk.Data.DbCore.Connection;
using BimPlus.Sdk.Data.DbCore.Recess;
using BimPlus.Sdk.Data.StructuralLoadResource;
using WPFWindows = System.Windows;
using IdeaRS.OpenModel;
using IdeaRS.OpenModel.CrossSection;
using IdeaRS.OpenModel.Geometry3D;
using IdeaRS.OpenModel.Model;
using IdeaRS.OpenModel.Result;
using IOM.GeneratorExample;
using Point = System.Drawing.Point;
using IdeaRS.Connections.Commands;
using WM = System.Windows.Media.Media3D;
using CI;

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
        public static string CrossSectionAttributeId = "32b57db0-f4a1-49e7-ab8b-0d81f0bf8684";
        public static string MaterialAttributeId = "f2d74244-feb2-45e3-b87b-07ef37bb7174";
        public static string RotationAttributeId = "e47e1bcc-7f14-4f91-9222-17b8fb15bcdc";

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
        #endregion button events

        #region IdeaStatica
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
                OpenModel example = SteelFrameExample.CreateIOM();
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

                    node = (TopologyItem)_integrationBase.ApiCore.DtObjects.PostObject(node);
                    nodes = node.Children.OfType<StructuralPointConnection>().ToList();
                }

                // Create CurveMembers
                var curveMembers = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(
                    model.TopologyDivisionId.GetValueOrDefault(), false, false, true);
                if (curveMembers.Count < 10)
                {
                    var beams = new TopologyItem
                    {
                        Name = "Element1D",
                        Parent = model.TopologyDivisionId,
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        Children = new List<DtObject>(example.LineSegment3D.Count)
                    };

                    foreach (var elem1d in example.Element1D)
                    {
                        var cm = elem1d.Segment.Element as LineSegment3D;
                        var cs = elem1d.CrossSectionBegin.Element as CrossSectionParameter;
                        var mat = cs?.Material.Element as IdeaRS.OpenModel.Material.MatSteelEc2;

                        if (cm == null)
                            continue;
                        var curveMember = new StructuralCurveMember
                        {
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            OrderNumber = cm.Id,
                            Name = elem1d.Name,
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
                                    OrderNumber = 2,
                                    Name = "R2",
                                    RelatedStructuralConnection =
                                        nodes.Find(x => x.NodeId == cm.EndPoint.Element.Id)?.Id
                                }
                            }
                        };
                        curveMember.AddProperty(TableNames.contentAttributes, RotationAttributeId, elem1d.RotationRx);
                        curveMember.AddProperty(TableNames.contentAttributes, CrossSectionAttributeId, cs.Name);
                        if (mat != null)
                            curveMember.AddProperty(TableNames.contentAttributes, MaterialAttributeId, mat.Name);

                        beams.Children.Add(curveMember);
                    }
                    beams = (TopologyItem)_integrationBase.ApiCore.DtObjects.PostObject(beams);
                    curveMembers = beams.Children.OfType<StructuralCurveMember>().ToList();
                }

                // create Assemblies
                var assemblies = new TopologyItem
                {
                    Name = "Member1D",
                    Parent = model.TopologyDivisionId,
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    Children = new List<DtObject>(example.Member1D.Count)
                };
                foreach (var member in example.Member1D)
                {
                    var relatedConnections = new List<ConnectionElement>();
                    //var elements = member.Elements1D.OfType<Element1D>().ToList();
                    var path = new BimPlus.Sdk.Data.CSG.Path
                    {
                        Geometry = new List<CsgGeoElement>()
                    };
                    CrossSectionParameter crossSection = null;
                    double rot = 0F;
                    foreach (var re in member.Elements1D)
                    {
                        if (!(re.Element is Element1D element))
                            continue;

                        rot = element.RotationRx;
                        if (crossSection == null)
                            crossSection = element.CrossSectionBegin.Element as CrossSectionParameter;

                        if (!(element.Segment.Element is LineSegment3D line))
                            continue;

                        var rel = curveMembers.Find(x => x.OrderNumber == line.Id);
                        if (rel != null)
                            relatedConnections.Add(new RelConnectsElements
                            {
                                Parent = model.ProjectId,
                                RelatedElement = rel.Id
                            });

                        if (path.Geometry.Count == 0)
                        {
                            if (crossSection != null)
                                path.CrossSection = crossSection.Name;

                            path.Rotation = element.RotationRx;
                            path.OffsetX = element.EccentricityBeginX;
                            path.OffsetY = element.EccentricityBeginY;
                            if (line.StartPoint.Element is Point3D sp)
                                path.Geometry.Add(new StartPolygon
                                { Point = new List<double> { sp.X * 1000F, sp.Y * 1000F, sp.Z * 1000F } });
                        }

                        if (line.EndPoint.Element is Point3D ep)
                            path.Geometry.Add(new Line
                            { Point = new List<double> { ep.X * 1000F, ep.Y * 1000F, ep.Z * 1000F } });
                    }

                    var assembly = new ElementAssembly
                    {
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        Name = member.Name,
                        OrderNumber = member.Id,
                        CsgTree = new DtoCsgTree
                        {
                            Color = (uint)Color.CadetBlue.ToArgb(),
                            Elements = new List<CsgElement>(1) { path }
                        },
                        Connections = relatedConnections
                    };

                    // add some attributes 'crossSection', 'material'
                    assembly.AddProperty(TableNames.contentAttributes, RotationAttributeId, rot);
                    assembly.AddProperty(TableNames.contentAttributes, CrossSectionAttributeId, crossSection.Name);
                    IdeaRS.OpenModel.Material.MatSteelEc2 material = crossSection.Material.Element as IdeaRS.OpenModel.Material.MatSteelEc2;
                    if (material != null)
                        assembly.AddProperty(TableNames.contentAttributes, MaterialAttributeId, material.Name);
                    assemblies.Children.Add(assembly);
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

        private void IdeaStaticaConnection_OnClick(object sender, RoutedEventArgs e)
        {
            DtoDivision model = CreateCsgModel("IdeaStatica");
            if (model == null)
                return;

            ProgressWindow.Text = "Import IdeaStaticaExampleData.";
            ProgressWindow.Show();

            // create/read bolt template
            var boltTemplateId = _integrationBase.ApiCore.GeometryTemplate.Create(
                JsonConvert.DeserializeObject<DtoTemplateArticle>(Properties.Resources.bolt_5074991643486136591));

            var test = _integrationBase.ApiCore.GeometryTemplate.Get(boltTemplateId);

            try
            {
                // create IOM and results
                OpenModel example = SteelFrameExample.CreateIOM();
                OpenModelResult result = Helpers.GetResults();

                var assemblies = _integrationBase.ApiCore.DtObjects.GetObjects<ElementAssembly>(
                    model.TopologyDivisionId.GetValueOrDefault(), false, false, true);
                if (assemblies == null || assemblies.Count == 0)
                {
                    MessageBoxHelper.ShowInformation("could not find steel assemblies\n please use Import IdeaStatica", _parentWindow);
                    return;
                }

                var connections = _integrationBase.ApiCore.DtObjects.GetObjects<ProjectConnection>(model.ProjectId);
                if (connections != null && connections.Count > 0)
                {
                    foreach (var con in connections)
                    {
                        _integrationBase.ApiCore.DtoConnection.DeleteConnections(con.Id);
                    }
                }

                foreach (var idCon in example.Connections)
                {
                    // mappingTable between IdeaStatica.Id and BimPlus.Id
                    Dictionary<int,Guid> connectionIds = new Dictionary<int, Guid>();
                    foreach (var b in idCon.Beams)
                    {
                        var assembly = assemblies.Find(x => x.Name == b.Name);
                        if (assembly == null) continue;
                        connectionIds.Add(b.Id,assembly.Id);
                    }

                    DtoConnections connection = new DtoConnections
                    {
                        ElementIds = connectionIds.Values.ToList(),
                        ConnectionElement = new ConnectionElement
                        {
                            Name =  "IdeaStatica_Connection",
                            Children = new List<DtObject>()
                        }
                    };

                    // create plates
                    foreach (var p in idCon.Plates)
                    {
                        var rotation = -Math.PI / 2;
                        var direction = CreateFromLCS(p.AxisX, p.AxisY, p.AxisZ, out rotation);
                        rotation = -Math.PI / 2;
                        var plate = new SteelPlate
                        {
                            Name = p.Name,
                            Parent = connection.ElementIds[0], // TODO: p.OriginalModelId
                            Division = model.Id,
                            LogParentID = model.ProjectId,
                            CsgTree = new DtoCsgTree
                            {
                                Color = (uint) Color.DarkCyan.ToArgb(),
                                Elements = new List<CsgElement>(2)
                                {
                                    new Path
                                    {
                                        Rotation = rotation,
                                        Geometry = new List<CsgGeoElement>(2)
                                        {
                                            new StartPolygon
                                            {
                                                Point = new List<double>{0, 0, 0}
                                            },
                                            new Line
                                            { 
                                                //Point = new List<double> { p.Thickness * 1000F * direction.DirectionX, p.Thickness * 1000F * direction.DirectionY,p.Thickness * 1000F * direction.DirectionZ}
                                                Point = new List<double>{ p.Thickness * 1000F * (Math.Abs(p.AxisX.Z) + Math.Abs(p.AxisY.X) + Math.Abs(p.AxisZ.Y))/3F,
                                                                          p.Thickness * 1000F * (Math.Abs(p.AxisX.Y) + Math.Abs(p.AxisY.Z) + Math.Abs(p.AxisZ.X))/3F,
                                                                          p.Thickness * 1000F * (Math.Abs(p.AxisX.X) + Math.Abs(p.AxisY.Y) + Math.Abs(p.AxisZ.Z))/3F }
                                            }
                                        }
                                    },
                                    new OuterContour
                                    {
                                        Geometry = new List<CsgGeoElement>()
                                    }
                                }
                            }
                        };
                        // OuterContour
                        var items = p.Region.Split(' ');
                        for (int i = 0; i < items.Length; i += 3)
                        {
                            double x = double.Parse(items[i + 1], CultureInfo.InvariantCulture) * 1000F;
                            double y = double.Parse(items[i + 2], CultureInfo.InvariantCulture) * 1000F;
                            if (items[i] == "M")
                                plate.CsgTree.Elements[1].Geometry.Add(new StartPolygon {Point = new List<double> {x, y}});
                            else if (items[i] == "L")
                                plate.CsgTree.Elements[1].Geometry.Add(new Line {Point = new List<double> {x, y}});
                        }

                        plate.Matrix = new TmpMatrix
                        {
                            Values = new[]
                            {
                                p.AxisX.X, p.AxisX.Y, p.AxisX.Z, p.Origin.X * 1000F,
                                p.AxisY.X, p.AxisY.Y, p.AxisY.Z, p.Origin.Y * 1000F,
                                p.AxisZ.X, p.AxisZ.Y, p.AxisZ.Z, p.Origin.Z * 1000F,
                                0F, 0F, 0F, 1F
                            }
                        };

                        plate.AddProperty(TableNames.tabAttribConstObjInstObj, "ConnectionChild-Element", connection.ElementIds[0]);

                        connection.ConnectionElement.Children.Add(plate);
                    }

                    // add cuts
                    foreach (var cut in idCon.CutBeamByBeams)
                    {
                        Point3D cuttingPos = null;
                        Vector3D vx = null, vy = null, vz = null;
                        // TODO: find reference to cutted beam !!
                        if (cut.CuttingObject.TypeName.Equals("PlateData"))
                        {
                            var p = cut.CuttingObject.Element as IdeaRS.OpenModel.Connection.PlateData;
                            cuttingPos = p.Origin;
                            vx = p.AxisX;
                            vy = p.AxisY;
                            vz = p.AxisZ;
                        }

                        if (cuttingPos == null || vx == null || vy == null || vz == null)
                            continue;

                        var direction = CreateFromLCS(vx, vy, vz, out double rotation);
                        var poly = new CBaseElementPolyeder();
                        poly.point.Add(new CElementPoint(cuttingPos.X * 1000F, cuttingPos.Y * 1000F, cuttingPos.Z * 1000F));
                        poly.point.Add(new CElementPoint(cuttingPos.X * 1000F + direction.DirectionX, cuttingPos.Y * 1000F + direction.DirectionY, cuttingPos.Z * 1000F + direction.DirectionZ));
                        poly.edge.Add(new CElementEdge(1, 2));
   

                        if (connectionIds.TryGetValue(cut.ModifiedObject.Id, out Guid parent) == false)
                            parent = connection.ElementIds[0];

                        var cutPlane = new PolygonOpening
                        {
                            Name = "Cut",
                            Parent = parent,
                            Division = model.Id,
                            LogParentID = _integrationBase.CurrentProject.Id,
                            BytePolyeder = poly
                        };
                        connection.ConnectionElement.Children.Add(cutPlane);
                    }

                    // create bolts and openings
                    foreach (var bGrid in idCon.BoltGrids)
                    {
                        foreach (var bolt in bGrid.Positions)
                        {
                            var fastener = new MechanicalFastener
                            {
                                Name = bGrid.Standard,
                                Parent = connection.ElementIds[0], // TODO: check assembly
                                Division = model.Id,
                                LogParentID = _integrationBase.CurrentProject.Id,
                                Template = boltTemplateId,
                                Matrix = new TmpMatrix
                                {
                                    Values = new[]
                                    {
                                        bGrid.AxisX.X, bGrid.AxisX.Y, bGrid.AxisX.Z, bolt.X * 1000F,
                                        bGrid.AxisY.X, bGrid.AxisY.Y, bGrid.AxisY.Z, bolt.Y * 1000F,
                                        bGrid.AxisZ.X, bGrid.AxisZ.Y, bGrid.AxisZ.Z, bolt.Z * 1000F,
                                        0F, 0F, 0F, 1F
                                    }
                                }
                            };
                            var json = Newtonsoft.Json.JsonConvert.SerializeObject(fastener);
                            connection.ConnectionElement.Children.Add(fastener);

                            var t1 = 100F * bGrid.AxisX.X + 100F * bGrid.AxisX.Y + 100F * bGrid.AxisX.Z;
                            var t2 = bGrid.BoreHole * 100F * bGrid.AxisX.X + 100F * bGrid.AxisX.Y + 100F * bGrid.AxisX.Z;
                            var opening = new Opening
                            {
                                Name = bGrid.Standard,
                                Parent = connection.ElementIds[0], // TODO: check assembly
                                Division = model.Id,
                                LogParentID = _integrationBase.CurrentProject.Id,
                                CsgTree = new DtoCsgTree
                                {
                                    Color = (uint)Color.Gray.ToArgb(),
                                    Elements = new List<CsgElement>(1) 
                                    { 
                                        new Path {
                                            Geometry = new List<CsgGeoElement>
                                            {
                                                new StartPolygon {Point = new List<double> {0, -40, 0}},
                                                //new Line {Point = new List<double> { 100F * bGrid.AxisX.X, 100F * bGrid.AxisX.Y, 100F * bGrid.AxisX.Z}},
                                                new Line {Point = new List<double> { 100F * (bGrid.AxisX.Z + bGrid.AxisY.X + bGrid.AxisZ.Y)/3F,
                                                                                     100F * (bGrid.AxisX.Y + bGrid.AxisY.Z + bGrid.AxisZ.X)/3F,
                                                                                     100F * (bGrid.AxisX.X + bGrid.AxisY.Y+ bGrid.AxisZ.Z)/3F}}
                                            },
                                            CrossSection = "RD16"
                                        }
                                    }
                                },
                                Matrix = new TmpMatrix
                                {
                                    Values = new[]
                                    {
                                        bGrid.AxisX.X, bGrid.AxisX.Y, bGrid.AxisX.Z, bolt.X * 1000F,
                                        bGrid.AxisY.X, bGrid.AxisY.Y, bGrid.AxisY.Z, bolt.Y * 1000F,
                                        bGrid.AxisZ.X, bGrid.AxisZ.Y, bGrid.AxisZ.Z, bolt.Z * 1000F,
                                        0F, 0F, 0F, 1F
                                    }
                                }
                            };
                            connection.ConnectionElement.Children.Add(opening);
                        }
                    }

                    // create welds
                    foreach (var w in idCon.Welds)
                    {
                        Guid parent = connection.ElementIds[0];
                        foreach (var t in w.ConnectedPartIds)
                        {
                            if (connectionIds.ContainsKey(Convert.ToInt32(t)))
                                parent = connectionIds[Convert.ToInt32(t)];
                        }

                        var weld = new Weld
                        {
                            Name = $"W{w.Id}",
                            Parent = parent,
                            Division = model.Id,
                            LogParentID = _integrationBase.CurrentProject.Id,
                            CsgTree = new DtoCsgTree
                            {
                                Color = (uint)Color.DarkBlue.ToArgb(),
                                Elements = new List<CsgElement>(1)
                                    {
                                        new Path {
                                            Geometry = new List<CsgGeoElement>
                                            {
                                                new StartPolygon {Point = new List<double> {w.Start.X * 1000F, w.Start.Y * 1000F, w.Start.Z * 1000F}},
                                                new Line {Point = new List<double> { w.End.X * 1000F, w.End.Y * 1000F, w.End.Z * 1000F}},
                                            },
                                            CrossSection = "RD8"
                                        }
                                    }
                            }
                        };
                        //weld.AddProperty(TableNames.tabAttribConstObjInstObj, "ConnectionChild-Element", parent);
                        connection.ConnectionElement.Children.Add(weld);
                    }

                    string jsonString = JsonConvert.SerializeObject(connection);
                    connection = _integrationBase.ApiCore.DtoConnection.CreateConnection(model.ProjectId, connection);
                    if (connection == null || connection.Id == Guid.Empty)
                        MessageBoxHelper.ShowInformation("DtoConnections could not be generated.", _parentWindow);
                }

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

        private static CI.Geometry3D.Vector3D CreateFromLCS(Vector3D axisX, Vector3D axisY, Vector3D axisZ, out double rotation)
        {
            //rotation = -Math.PI / 2;
            //CI.Geometry3D.Matrix44 matrix = new CI.Geometry3D.Matrix44(new CI.Geometry3D.Vector3D(axisX.X, axisX.Y, axisX.Z),
            //                                                            new CI.Geometry3D.Vector3D(axisY.X, axisY.Y, axisY.Z),
            //                                                            new CI.Geometry3D.Vector3D(axisZ.X, axisZ.Y, axisZ.Z));


            //return matrix.TransformToGCS(new CI.Geometry3D.Vector3D(1, 0, 0));
            double beta, gamma, betaEnd, gammaEnd;


            GetDirVectorAngles(new System.Windows.Media.Media3D.Vector3D(axisZ.X, axisZ.Y, axisZ.Z), false, false, out beta, out gamma, out betaEnd, out gammaEnd);

            var notRotadedMatrix = new CI.Geometry3D.Matrix44();
            if (!gamma.IsZero())
            {
                // gamma pitch
                notRotadedMatrix.Rotate(gamma, new CI.Geometry3D.Vector3D(0, 1, 0));
            }

            if (!beta.IsZero())
            {
                // beta direction
                notRotadedMatrix.Rotate(beta, new CI.Geometry3D.Vector3D(0, 0, 1));
            }

            rotation = CI.Geometry3D.GeomOperation.GetClockwiseAngle(notRotadedMatrix.AxisY, new CI.Geometry3D.Vector3D(axisY.X, axisY.Y, axisY.Z), notRotadedMatrix.AxisX) - Math.PI / 2;

            return new CI.Geometry3D.Vector3D(axisZ.X, axisZ.Y, axisZ.Z);
            //return new CI.Geometry3D.Vector3D(0, 0, 1);
        }

        private static void GetDirVectorAngles(WM.Vector3D dirVector, bool IsContinuous, bool isInside, out double beta, out double gamma, out double betaFromEnd, out double gammaFromEnd)
        {
            beta = 0;
            gamma = 0;
            betaFromEnd = 0;
            gammaFromEnd = 0;

            bool isZeroX = dirVector.X.IsZero();
            bool isZeroY = dirVector.Y.IsZero();
            bool isZeroZ = dirVector.Z.IsZero();

            if (isZeroX && isZeroY)
            {
                // parallel to global Z
                if (dirVector.Z.IsGreater(0))
                {
                    gamma = -Math.PI*2;
                    if (IsContinuous && !isInside)
                    {
                        gamma = Math.PI * 2;
                    }
                }
                else
                {
                    gamma = Math.PI * 2;
                    if (IsContinuous && !isInside)
                    {
                        gamma = -Math.PI * 2;
                    }
                }
            }
            else
            {
                if (isZeroY)
                {
                    // parallel to global X
                    if (IsContinuous && !isInside)
                    {
                        if (dirVector.X.IsGreater(0))
                        {
                            beta = Math.PI;
                            betaFromEnd = Math.PI;
                        }
                        else
                        {
                            beta = 0;// MathConstants.PI;
                            betaFromEnd = 0;// MathConstants.PI;
                        }
                    }
                    else
                    {
                        if (dirVector.X.IsGreater(0))
                        {
                            beta = 0;
                            betaFromEnd = 0;
                        }
                        else
                        {
                            beta = Math.PI;
                            betaFromEnd = Math.PI;
                        }
                    }

                    if (IsContinuous && !isInside)
                    {
                        gamma = Math.Atan2(dirVector.Z, Math.Abs(dirVector.X));
                        gammaFromEnd = gamma;
                    }
                    else
                    {
                        gamma = -Math.Atan2(dirVector.Z, Math.Abs(dirVector.X));
                        gammaFromEnd = gamma;

                    }
                }
                else if (isZeroX)
                {
                    // parallel to global X
                    if (IsContinuous && !isInside)
                    {
                        if (dirVector.Y.IsGreater(0))
                        {
                            beta = Math.PI*2;
                            betaFromEnd = Math.PI*2;
                        }
                        else
                        {
                            beta = -Math.PI*2;
                            betaFromEnd = -Math.PI*2;
                        }
                    }
                    else
                    {
                        if (dirVector.Y.IsGreater(0))
                        {
                            beta = Math.PI * 2;
                            betaFromEnd = Math.PI * 2;
                        }
                        else
                        {
                            beta = -Math.PI * 2;
                            betaFromEnd = -Math.PI * 2;
                        }
                    }

                    gamma = -Math.Atan2(dirVector.Z, Math.Abs(dirVector.Y));
                    gammaFromEnd = -Math.Atan2(dirVector.Z, Math.Abs(dirVector.Y));
                }
                else
                {
                    // parallel to no axis
                    if (IsContinuous && !isInside)
                    {
                        dirVector.Negate();
                    }
                    beta = Math.Atan2(dirVector.Y, dirVector.X);
                    betaFromEnd = beta;
                    double lenInXY = Math.Sqrt(dirVector.Y * dirVector.Y + dirVector.X * dirVector.X);
                    gamma = -Math.Atan2(dirVector.Z, lenInXY);
                    gammaFromEnd = gamma;
                }
            }
        }

        #endregion IdeaStatica

    }
}
