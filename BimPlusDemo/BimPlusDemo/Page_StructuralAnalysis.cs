using BimPlus.Sdk.Data.DbCore.Analysis;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.StructuralLoadResource;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BimPlus.Client;
using BimPlus.Sdk.Data.DbCore.Connection;
using BimPlus.Sdk.Data.TenantDto;
using System;
using System.Drawing;
using BimPlus.Sdk.Data.CSG;
using BimPlus.Sdk.Data.DbCore.Steel;

namespace BimPlusDemo
{
    public partial class MainWindow
    {
        private void Nodes_OnClick(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("StructuralAnalysis");
            if (model?.TopologyDivisionId == null)
                return;

            var nodes =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value);
            if (nodes?.Count >= 12)
            {
                MessageBox.Show("Nodes are already created!");
                return;
            }

            // create some StructuralPointConnection objects 
            var topologyDivision = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Division = model.Id,
                LogParentID = model.ProjectId,
                Name = "Nodes",
                Children = new List<DtObject>(12)
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
                        AppliedCondition = new BoundaryNodeCondition("TRigid", false, false, false, true, true, true)
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
                        AppliedCondition = new BoundaryNodeCondition("TRigid", true, true, true, false, false, false)
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
                        AppliedCondition = new BoundaryNodeCondition("Fixed", 100, 3, 100, 3.5, 1.9, 3.7)
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N5",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 2.000,
                        Y = 6.000,
                        Z = 0.200,
                        NodeId = 5,
                        AppliedCondition = new BoundaryNodeCondition("RRidig", false, false, false, true, true, true)
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N6",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 3.600,
                        Y = 6.000,
                        Z = 0.200,
                        NodeId = 6,
                        AppliedCondition = new BoundaryNodeCondition("N6", 2.0, true, 3.0, 2.5, false, 4.7)
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N7",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 0.500,
                        Y = 0.500,
                        Z = 3.400,
                        NodeId = 7
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N8",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 2.000,
                        Y = 0.500,
                        Z = 3.400,
                        NodeId = 8
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N9",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 3.600,
                        Y = 0.500,
                        Z = 3.400,
                        NodeId = 9
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N10",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 0.500,
                        Y = 6.000,
                        Z = 4.400,
                        NodeId = 10
                    },
                    new StructuralPointConnection
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "N11",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        X = 2.000,
                        Y = 6.000,
                        Z = 4.400,
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
            // ReSharper disable once UnusedVariable
            if (IntBase.ApiCore.DtObjects.PostObject(topologyDivision) == null)
                MessageBox.Show("could not create geometry object.");
            else
                IntBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);

            IntBase.EventHandlerCore.OnExportStarted(this,
                new BimPlusEventArgs {Id = model.ProjectId, Value = "ModelChanged"});
        }

        private void CurveMember_OnClick_OnClick(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("StructuralAnalysis");
            if (model?.TopologyDivisionId == null)
                return;
            var nodes =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (nodes?.Count < 12)
            {
                MessageBox.Show("Please create at first Node objects");
                return;
            }

            if (nodes == null)
                return;

            // create some StructuralPointConnection objects 
            var topologyDivision = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Division = model.Id,
                LogParentID = model.ProjectId,
                Name = "Beams",
                Children = new List<DtObject>(13)
                {
                    new StructuralCurveMember
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "B1",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        // CurveMember is defined by relation to existing nodes.
                        // In Ifc you have to use 'RelConnectsStructuralMember' objects to establish this relation.
                        ConnectedBy = new List<RelConnectsStructuralMember>(2)
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R1",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 1)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R2",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R3",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 2)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R4",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R5",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 3)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R6",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R7",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 4)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R8",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R5",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
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
                        {
                            new RelConnectsStructuralMember
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
                        {
                            new RelConnectsStructuralMember
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R11",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R12",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 11)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R13",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R14",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 7)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R15",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R16",
                                AppliedCondition = new BoundaryNodeCondition("", false, false, false, true, true, true),
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
                        {
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 1,
                                Name = "R18",
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 9)?.Id
                            },
                            new RelConnectsStructuralMember
                            {
                                OrderNumber = 2,
                                Name = "R19",
                                // each 'RelConnectsStructuralMember' can have own stiffness information.
                                AppliedCondition =
                                    new BoundaryNodeCondition("Rigid", false, false, false, true, true, true),
                                RelatedStructuralConnection = nodes.Find(x => x.NodeId == 12)?.Id
                            }
                        }
                    }
                }
            };
            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
            if (IntBase.ApiCore.DtObjects.PostObject(topologyDivision) == null)
                MessageBox.Show("could not create geometry object.");
            else
            {
                IntBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                IntBase.EventHandlerCore.OnExportStarted(this,
                    new BimPlusEventArgs {Id = model.ProjectId, Value = "ModelChanged"});
            }
        }

        private void SurfaceMember_OnClick(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("StructuralAnalysis");
            if (model?.TopologyDivisionId == null)
                return;
            var nodes =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (nodes?.Count < 12)
            {
                MessageBox.Show("Please create at first Node objects");
                return;
            }

            if (nodes == null)
                return;

            var ssm = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Division = model.Id,
                LogParentID = model.ProjectId,
                Name = "Surfaces",
                Children = new List<DtObject>(1)
                {
                    new StructuralSurfaceMember
                    {
                        Name = "B12",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        // surface is defined by relation to existing nodes.
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
                    }
                }
            };

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(ssm);
            if (IntBase.ApiCore.DtObjects.PostObject(ssm) == null)
                MessageBox.Show("could not create geometry object.");
            else
            {
                IntBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                IntBase.EventHandlerCore.OnExportStarted(this,
                    new BimPlusEventArgs {Id = model.ProjectId, Value = "ModelChanged"});
            }
        }

        private void Loads_OnClick(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("StructuralAnalysis");
            if (model?.TopologyDivisionId == null)
                return;
            var nodes =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (nodes == null || nodes.Count < 12)
            {
                MessageBox.Show("Please create at first Node objects");
                return;
            }
            var beams =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (beams == null)
            {
                MessageBox.Show("Please create at first Beam objects (StructuralCurveMember)");
                return;
            }
            var ceiling = IntBase.ApiCore.DtObjects.GetObjects<StructuralSurfaceMember>(model.TopologyDivisionId.Value,
                false, false, true).FirstOrDefault();

            var loadCases = CreateDefaultLoadGroups(model);
            var constructionLc = loadCases.FirstOrDefault(x => x.Name == "Construction Load")?.IsGroupedBy[0]?.Id;
            var ownWeightLc = loadCases.FirstOrDefault(x => x.Name == "Own Weight")?.IsGroupedBy[0]?.Id;
            var trafficLc = loadCases.FirstOrDefault(x => x.Name == "Traffic Load")?.IsGroupedBy[0]?.Id;
            var snowLc = loadCases.FirstOrDefault(x => x.Name == "Snow Load")?.IsGroupedBy[0]?.Id;
            var windxLc = loadCases.FirstOrDefault(x => x.Name == "Wind Load X")?.IsGroupedBy[0]?.Id;
            var windyLc = loadCases.FirstOrDefault(x => x.Name == "Wind Load Y")?.IsGroupedBy[0]?.Id;

            var cl1 = new StructuralPointAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "CA1",
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
            };
            if (constructionLc.HasValue)  // add reference to LoadCase 'construction'
                cl1.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.1", constructionLc.Value);

            var wy1 = new StructuralPointAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "Wy1",
                Division = model.Id,
                LogParentID = model.ProjectId,
                AppliedLoad = new StructuralLoadSingleForce { ForceY = 3000 },
                AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                {
                    new RelConnectsStructuralActivity
                    {
                        Name = "AC2",
                        RelatingElement = nodes.Find(x => x.NodeId == 8)?.Id
                    }
                }
            };
            if (windyLc.HasValue) // add reference to LoadCase Windy'
                wy1.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.1", windyLc.Value);

            var wx1 = new StructuralPointAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "Wx1",
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
            };
            if (windxLc.HasValue)   // add reference to LoadCase Windx'
                wx1.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.1", windxLc.Value);

            var ta1 = new StructuralLinearAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "TA1",
                Division = model.Id,
                LogParentID = model.ProjectId,
                AppliedLoad = new StructuralLoadConfiguration
                {
                    Values = new List<StructuralLoadOrResult>(2)
                    {
                        new StructuralLoadLinearForce {LinearForceZ = -5000},
                        new StructuralLoadLinearForce {LinearForceZ = -2000}
                    },
                    Locations = new List<double>(2) { 200, 4000 }
                },
                AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                {
                    new RelConnectsStructuralActivity
                    {
                        Name = "RW1",
                        RelatingElement = beams.Find(x => x.Name == "B12")?.Id
                    }
                }
            };
            if (trafficLc.HasValue) // add back reference to LoadCase 'traffic'
                ta1.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.1", trafficLc.Value);

            var wx2 = new StructuralLinearAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "Wx2",
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
            };
            if (windxLc.HasValue)   // add reference to LoadCase 'windX'
                wx2.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.2", windxLc.Value);

            var wy2 = new StructuralLinearAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "Wy2",
                Division = model.Id,
                LogParentID = model.ProjectId,
                AppliedLoad = new StructuralLoadLinearForce { LinearForceY = -3000 },
                AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                {
                    new RelConnectsStructuralActivity
                    {
                        Name = "RW3",
                        RelatingElement = beams.Find(x => x.Name == "B9")?.Id
                    }
                }
            };
            if (windyLc.HasValue)
                wy2.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.2", windyLc.Value);

            var sl = new StructuralLinearAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "Sl",
                Division = model.Id,
                LogParentID = model.ProjectId,
                AppliedLoad = new StructuralLoadLinearForce { LinearMomentX = 1500 },
                AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                {
                    new RelConnectsStructuralActivity
                    {
                        Name = "Sl",
                        RelatingElement = beams.Find(x => x.Name == "B1")?.Id
                    }
                }
            };
            if (windyLc.HasValue) // add back reference to LoadCase 'own windY'
                sl.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.3", windyLc.Value);

            var wy3 = new StructuralLinearAction
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "Wy3",
                Division = model.Id,
                LogParentID = model.ProjectId,
                AppliedLoad = new StructuralLoadTemperature { DeltaTConstant = 25 },
                AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                {
                    new RelConnectsStructuralActivity
                    {
                        Name = "RW4",
                        RelatingElement = beams.Find(x => x.Name == "B10")?.Id
                    }
                }
            };
            if (snowLc.HasValue)    // add back reference to LoadCase 'Snow'
                wy3.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.1", snowLc.Value);

            // create some StructuralPointConnection objects 
            var topologyDivision = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Division = model.Id,
                LogParentID = model.ProjectId,
                Name = "Loads",
                Children = new List<DtObject>(7)
                {
                    cl1,
                    wy1,
                    wx1,
                    ta1,
                    wx2,
                    wy2,
                    sl,
                    wy3
                }
            };

            if (ceiling != null)
            {
                var ow = new StructuralPlanarAction
                {
                    Parent = model.TopologyDivisionId.Value,
                    Name = "OW",
                    Division = model.Id,
                    LogParentID = model.ProjectId,
                    AppliedLoad = new StructuralLoadPlanarForce { PlanarForceZ = 400 },
                    AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                    {
                        new RelConnectsStructuralActivity
                        {
                            Name = "Ow",
                            RelatingElement = ceiling.Id
                        }
                    }
                };
                if (ownWeightLc.HasValue)  // add back reference to Loadcase 'own weight'
                    ow.AddProperty(TableNames.tabAttribConstObjInstObj, $"Connection-RelatedElement.1",
                        ownWeightLc.Value);

                topologyDivision.AddChild(ow);
            }

            // ReSharper disable once UnusedVariable
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
            if (IntBase.ApiCore.DtObjects.PostObject(topologyDivision) == null)
                MessageBox.Show("could not create geometry object.");
            else
            {
                IntBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                IntBase.EventHandlerCore.OnExportStarted(this,
                    new BimPlusEventArgs { Id = model.ProjectId, Value = "ModelChanged" });
            }

        }

        private List<StructuralLoadCase> CreateDefaultLoadGroups(DtoDivision model)
        {
            if (!model.TopologyDivisionId.HasValue)
                return new List<StructuralLoadCase>();

            var loadCaseRelations =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralLoadCase>(model.TopologyDivisionId.Value, false, false, true) ?? new List<StructuralLoadCase>();
            if (loadCaseRelations.Count >= 5)
                return loadCaseRelations;

            var ow = Guid.NewGuid();
            var cl = Guid.NewGuid();
            var tl = Guid.NewGuid();
            var sl = Guid.NewGuid();
            var wlx = Guid.NewGuid();
            var wly = Guid.NewGuid();

            var loadCases = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "LoadCases",
                Division = model.Id,
                LogParentID = model.ProjectId,
                Children = new List<DtObject>(13)
                {
                    new StructuralLoadCase
                    {
                        Id = ow,
                        Name = "Own Weight",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "OW"
                            }
                        }
                    },
                    new StructuralLoadCase
                    {
                        Id = cl,
                        Name = "Construction Load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "CL"
                                // ,RelatedObjects = new List<Guid>(1) { loads.Find(x => x.Name == "CA1").Id }
                            }
                        }
                    },
                    new StructuralLoadCase
                    {
                        Id = tl,
                        Name = "Traffic Load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "TL"
                                // ,RelatedObjects = new List<Guid>(1) { loads.Find(x => x.Name == "TA1").Id }
                            }
                        }
                    },
                    new StructuralLoadCase
                    {
                        Id = sl,
                        Name = "Snow Load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "SL"
                            }
                        }
                    },
                    new StructuralLoadCase
                    {
                        Id = wlx,
                        Name = "Wind Load X",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "WLx"
                                //,RelatedObjects = (from load in loads where load.Name.Contains("Wx") select load.Id).ToList() 
                            }
                        }
                    },
                    new StructuralLoadCase
                    {
                        Id = wly,
                        Name = "Wind Load Y",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "WLy"
                                //,RelatedObjects = (from load in loads where load.Name.Contains("Wy") select load.Id).ToList()
                            }
                        }
                    },
                    new StructuralLoadGroup
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LoadCombination O+C",
                        Description = "Combination with ownWeight and construction load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "O+C",
                                RelatedObjects = new List<Guid>(2) { ow, cl}
                            }
                        }
                    },
                    new StructuralLoadGroup
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LoadCombination O+C+T",
                        Description = "Combination with ownWeight, construction load and traffic load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "O+C+T",
                                RelatedObjects = new List<Guid>(3) { ow, cl, tl}
                            }
                        }
                    },
                    new StructuralLoadGroup
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LoadCombination O+C+T+Wx",
                        Description = "Combination with ownWeight, construction load, traffic load and windX load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "O+C+T+Wx",
                                RelatedObjects = new List<Guid>(3)
                                    {ow, cl, tl, wlx}
                            }
                        }
                    },
                    new StructuralLoadGroup
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "LoadCombination O+C+T+Wy",
                        Description = "Combination with ownWeight, construction load, traffic load and windY load",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "O+C+T+Wy",
                                RelatedObjects = new List<Guid>(3)
                                    {ow, cl, tl, wly}
                            }
                        }
                    },
                    new StructuralLoadGroup
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "Ultimate Limit",
                        Description = "Combination with all existing load groups",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        IsGroupedBy = new List<RelAssignsToGroup>(1)
                        {
                            new RelAssignsToGroup
                            {
                                Name = "UL",
                                RelatedObjects = new List<Guid>(3)
                                {
                                    ow, cl, tl, wlx, wly,sl
                                }
                            }
                        }
                    }
                }
            };

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(loadCases);
            var loadCaseTopologyNode = IntBase.ApiCore.DtObjects.PostObject(loadCases);

            loadCaseRelations = loadCaseTopologyNode.Children.OfType<StructuralLoadCase>().ToList();

            if (loadCaseRelations.Count > 5)
            {
                MessageBox.Show($"LoadCases in Node {loadCaseTopologyNode.Name} created");
            }

            return loadCaseRelations;
        }

        private void Results_OnClick(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("StructuralAnalysis");
            if (model?.TopologyDivisionId == null)
                return;
            var nodes =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (nodes == null || nodes.Count < 12)
            {
                MessageBox.Show("Please create at first Node objects");
                return;
            }
            var beams =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (beams == null)
            {
                MessageBox.Show("Please create at first Beam objects (StructuralCurveMember)");
                return;
            }

            if (nodes == null)
                return;

            // create some StructuralPointReaction objects 
            var topologyDivision = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Division = model.Id,
                LogParentID = model.ProjectId,
                Name = "Reactions",
                Children = new List<DtObject>(13)
                {
                    new StructuralPointReaction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "S1",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadSingleForce
                        {
                            ForceX = 300,
                            ForceY = 0,
                            ForceZ = 1800
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
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
                        AppliedLoad = new StructuralLoadSingleForce
                        {
                            ForceX = 500,
                            ForceY = 100,
                            ForceZ = 200
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
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
                        AppliedLoad = new StructuralLoadSingleForce
                        {
                            ForceX = 100,
                            ForceY = 1800,
                            ForceZ = 700
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC3",
                                RelatingElement = nodes.Find(x => x.NodeId == 3)?.Id
                            }
                        }
                    },
                    new StructuralPointReaction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "S5",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadSingleDisplacement
                        {
                            DisplacementX = 3,
                            DisplacementY = 4,
                            DisplacementZ = 2,
                            RotationalDisplacementRX = 5.4,
                            RotationalDisplacementRY = 4.8,
                            RotationalDisplacementRZ = 16.5
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC4",
                                RelatingElement = nodes.Find(x => x.NodeId == 5)?.Id
                            }
                        }
                    },
                    new StructuralCurveReaction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "R6",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadConfiguration
                        {
                            Values = new List<StructuralLoadOrResult>(2)
                            {
                                new StructuralLoadLinearForce {LinearForceX = -3000},
                                new StructuralLoadLinearForce {LinearForceX = 3000}
                            },
                            Locations = new List<double>(2) { 0, 4200 }
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC6",
                                RelatingElement = beams.Find(x => x.Name == "B5")?.Id
                            }
                        }
                    },
                    new StructuralCurveReaction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "R4",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadConfiguration
                        {
                            Values = new List<StructuralLoadOrResult>(2)
                            {
                                new StructuralLoadLinearForce {LinearForceX = -3000, LinearForceY = -500},
                                new StructuralLoadLinearForce {LinearForceX = 3000, LinearForceY = 500}
                            },
                            Locations = new List<double>(2) { 0, 4200 }
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC6",
                                RelatingElement = beams.Find(x => x.Name == "B4")?.Id
                            }
                        }
                    },
                    new StructuralPointReaction
                    {
                        Parent = model.TopologyDivisionId.Value,
                        Name = "S6",
                        Division = model.Id,
                        LogParentID = model.ProjectId,
                        AppliedLoad = new StructuralLoadSingleDisplacement
                        {
                            RotationalDisplacementRX = 25.4,
                            RotationalDisplacementRY = 14.8,
                            RotationalDisplacementRZ = 06.5
                        },
                        AssignedToStructuralItem = new List<RelConnectsStructuralActivity>(1)
                        {
                            new RelConnectsStructuralActivity
                            {
                                Name = "AC6",
                                RelatingElement = nodes.Find(x => x.NodeId == 6)?.Id
                            }
                        }
                    }
                }
            };            // ReSharper disable once UnusedVariable
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
            if (IntBase.ApiCore.DtObjects.PostObject(topologyDivision) == null)
                MessageBox.Show("could not create geometry object.");
            else
            {
                IntBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                IntBase.EventHandlerCore.OnExportStarted(this,
                    new BimPlusEventArgs { Id = model.ProjectId, Value = "ModelChanged" });
            }

        }
        private void Assemblies_OnClick(object sender, RoutedEventArgs e)
        {
            var model = SelectModel("StructuralAnalysis");
            if (model?.TopologyDivisionId == null)
                return;
            var nodes =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(model.TopologyDivisionId.Value,
                    false, false, true);
            if (nodes == null || nodes.Count < 12)
            {
                MessageBox.Show("Please create at first Node objects");
                return;
            }
            var node3 = nodes.Find(x => x.NodeId == 3);
            var node6 = nodes.Find(x => x.NodeId == 6);
            var node9 = nodes.Find(x => x.NodeId == 9);
            var node12 = nodes.Find(x => x.NodeId == 12);
            if (node12 == null || node3 == null || node6 == null || node9 == null)
            {
                MessageBox.Show("Can't identify nodes");
                return;
            }


            var beams =
                IntBase.ApiCore.DtObjects.GetObjects<StructuralCurveMember>(model.ProjectId, false, false, true);
            if (beams == null)
            {
                MessageBox.Show("Please create at first Beam objects (StructuralCurveMember)");
                return;
            }

            var topologyDivision = new TopologyItem
            {
                Parent = model.TopologyDivisionId.Value,
                Name = "SteelAssemblies",
                Division = model.Id,
                LogParentID = model.ProjectId
            };

            // create some assemblies with relation to StructuralCurveMembers
            var assembly1 = new ElementAssembly
            {
                Name = "B3_Column",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = IntBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.MediumBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B3_Assembly",
                        OrderNumber = 3,
                        RelatedElement = beams.Find(x => x.Name == "B3")?.Id
                    }
                }
            };

            assembly1.CsgTree.Elements.Add(new Path
            {
                Geometry = new List<CsgGeoElement>
                {
                    new StartPolygon {Point = new List<double> {node3.X.GetValueOrDefault()*1000, node3.Y.GetValueOrDefault() * 1000, node3.Z.GetValueOrDefault()*1000}},
                    new Line {Point = new List<double> {node9.X.GetValueOrDefault() * 1000, node9.Y.GetValueOrDefault() * 1000, node9.Z.GetValueOrDefault()*1000}}
                },
                CrossSection = "HEB120"
            });
            topologyDivision.AddChild(assembly1);

            var assembly2 = new ElementAssembly
            {
                Name = "B6_Column",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = IntBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.MediumBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B6_Assembly",
                        OrderNumber = 6,
                        RelatedElement = beams.Find(x => x.Name == "B6")?.Id
                    }
                }
            };

            assembly2.CsgTree.Elements.Add(new Path
            {
                Rotation = Math.PI / 2,
                Geometry = new List<CsgGeoElement>
                {
                    new StartPolygon {Point = new List<double> {node6.X.GetValueOrDefault() * 1000, node6.Y.GetValueOrDefault() * 1000, node6.Z.GetValueOrDefault()*1000}},
                    new Line {Point = new List<double> {node12.X.GetValueOrDefault() * 1000, node12.Y.GetValueOrDefault() * 1000, node12.Z.GetValueOrDefault()*1000}}
                },
                CrossSection = "HEB120"
            });
            topologyDivision.AddChild(assembly2);

            var assembly3 = new ElementAssembly
            {
                Name = "B13_Column",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = IntBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.CornflowerBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B13_Assembly",
                        OrderNumber = 13,
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
                    new StartPolygon {Point = new List<double> {node9.X.GetValueOrDefault() * 1000, node9.Y.GetValueOrDefault() * 1000, node9.Z.GetValueOrDefault()*1000}},
                    new Line {Point = new List<double> {node12.X.GetValueOrDefault() * 1000, node12.Y.GetValueOrDefault() * 1000, node12.Z.GetValueOrDefault()*1000}}
                },
                CrossSection = "UPE100"
            });
            topologyDivision.AddChild(assembly3);

            var assembly4 = new ElementAssembly
            {
                Name = "B13_ColumnB",
                Parent = model.TopologyDivisionId,
                Division = model.Id,
                LogParentID = IntBase.CurrentProject.Id,
                CsgTree = new DtoCsgTree { Color = (uint)Color.CornflowerBlue.ToArgb() },
                Connections = new List<ConnectionElement>
                {
                    new RelConnectsElements
                    {
                        Parent = model.ProjectId,
                        Name = "Relation_B13_Assembly",
                        OrderNumber = 12,
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
                    new StartPolygon {Point = new List<double> {node9.X.GetValueOrDefault() * 1000, node9.Y.GetValueOrDefault() * 1000, node9.Z.GetValueOrDefault()*1000}},
                    new Line {Point = new List<double> {node12.X.GetValueOrDefault() * 1000, node12.Y.GetValueOrDefault() * 1000, node12.Z.GetValueOrDefault()*1000}}
                },
                CrossSection = "UPE100"
            });
            topologyDivision.AddChild(assembly4);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(topologyDivision);
            if (IntBase.ApiCore.DtObjects.PostObject(topologyDivision) == null)
                MessageBox.Show("could not create geometry object.");
            else
            {
                IntBase.ApiCore.Projects.ConvertGeometry(model.ProjectId);
                IntBase.EventHandlerCore.OnExportStarted(this,
                    new BimPlusEventArgs { Id = model.ProjectId, Value = "ModelChanged" });
            }

        }
    }
}

