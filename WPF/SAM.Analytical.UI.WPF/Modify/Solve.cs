// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using SAM.Analytical.Solver;
using SAM.Architectural;
using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static AnalyticalModel? Solve(this AnalyticalModel analyticalModel, out IEnumerable<SAMObject>? sAMObjects)
        {
            sAMObjects = null;

            if (analyticalModel is null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return new AnalyticalModel(analyticalModel);
            }

            List<Panel> panels_All = adjacencyCluster.GetPanels();
            List<double> elevations = panels_All.Levels()?.ConvertAll(x => x.Elevation) ?? [];

            BoundingBox3D boundingBox3D = panels_All.BoundingBox3D();
            if (boundingBox3D is not null && !elevations.Contains(boundingBox3D.Max.Z))
            {
                elevations.Add(boundingBox3D.Max.Z);
            }

            SolverWindow solverWindow = new()
            {
                Levels = [.. elevations.Distinct()],
                Languages = ActiveManager.GetSpecialCharacterMapNames()
            };

            bool? showDialogResult = solverWindow.ShowDialog();
            if (!(showDialogResult is not null && showDialogResult.Value))
            {
                return null;
            }

            using ProgressBarWindowManager progressBarWindowManager = new();

            progressBarWindowManager.Show("Solve", "Solving...");

            List<PanelType> panelTypes_Excluded = solverWindow.ExcludedPanelTypes;
            bool removePanelInternalEdges = solverWindow.RemovePanelInternalEdges;

            bool filterPanels = solverWindow.FilterPanels;
            double minArea = filterPanels ? solverWindow.MinArea : double.NaN;
            double minThinnessRatio = filterPanels ? solverWindow.MinThinnessRatio : double.NaN;
            double bucketSize_SingleLevel = solverWindow.BucketSize_SingleLevel;
            double bucketSize_MultipleLevel = solverWindow.BucketSize_MultipleLevel;
            double weight = solverWindow.Weight;
            double maxExtension = solverWindow.MaxExtension;
            double levelOffset = solverWindow.LevelOffset;

            bool replaceNameSpecialCharacters = solverWindow.ReplaceNameSpecialCharacters;
            string language = solverWindow.SelectedLanguage;

            bool removeUnusedSpaces = solverWindow.RemoveUnusedSpaces;

            elevations = solverWindow.Levels;

            List<Range<double>> ranges = [];
            for (int i = 0; i < elevations.Count - 1; i++)
            {
                ranges.Add(new Range<double>(elevations[i], elevations[i + 1]));
            }

            List<Panel> panels_Filtered = panels_All.Where(panel => !panelTypes_Excluded.Contains(panel.PanelType)).ToList();

            Dictionary<Guid, SAMObject> dictionary = [];

            if (panels_Filtered != null && panels_Filtered.Count > 0)
            {
                if (filterPanels)
                {
                    for (int i = panels_Filtered.Count - 1; i >= 0; i--)
                    {
                        Panel panel = panels_Filtered[i];

                        Face3D face3D = panel.GetFace3D();

                        if (face3D is null || face3D.GetArea() < minArea)
                        {
                            panels_Filtered.RemoveAt(i);
                            dictionary[panel.Guid] = panel;
                            adjacencyCluster.RemoveObject(panel);
                            continue;
                        }

                        double thinnessRatio = face3D.ThinnessRatio();

                        if (thinnessRatio < minThinnessRatio)
                        {
                            panels_Filtered.RemoveAt(i);
                            dictionary[panel.Guid] = panel;
                            adjacencyCluster.RemoveObject(panel);
                            continue;
                        }
                    }
                }

                if (removePanelInternalEdges)
                {
                    for (int i = 0; i < panels_Filtered.Count; i++)
                    {
                        Panel panel = panels_Filtered[i];

                        Face3D face3D = panel.GetFace3D();
                        if (face3D is null)
                        {
                            continue;
                        }

                        if (face3D.GetInternalEdge3Ds() is not List<IClosedPlanar3D> internalEdge3Ds || internalEdge3Ds.Count == 0)
                        {
                            continue;
                        }

                        Guid guid = panel.Guid;

                        panel = Analytical.Create.Panel(guid, panel, new Face3D(face3D.GetExternalEdge3D()));

                        dictionary[guid] = panel;
                        panels_Filtered[i] = panel;
                    }
                }

                for (int i = 0; i < panels_Filtered.Count; i++)
                {
                    Panel panel = panels_Filtered[i];
                    if (panel is null)
                    {
                        continue;
                    }

                    panel.SetValue(SolverParameter.BucketSize, bucketSize_SingleLevel);
                    panel.SetValue(SolverParameter.Weight, weight);
                    panel.SetValue(SolverParameter.MaxExtend, maxExtension);

                    panels_Filtered[i] = panel;
                }
            }

            Solver<Panel> solver = new(panels_Filtered, ranges)
            {
                Tolerance_Angle = Tolerance.Angle,
                Tolerance_Distance = Tolerance.Distance,
                Tolerance_Arc = Geometry.Solver.SnapSolver.DEFAULT_ArcToleranceAngleRad,
                MinLength = Geometry.Solver.SnapSolver.DEFAULT_MinWallSegmentLength,
                SnapDistance = Geometry.Solver.SnapSolver.DEFAULT_NakedNodeSnapDistance,
                Offset = levelOffset
            };

            List<Panel> panels_New = solver.Execute(out List<Point3D> nakedPoint3Ds, bucketSize_MultipleLevel);
            Dictionary<Guid, Guid> dictionary_Guid = [];
            if (panels_New is not null)
            {
                for (int i = 0; i < panels_New.Count; i++)
                {
                    Guid guid = panels_New[i].Guid;

                    dictionary_Guid[guid] = guid;

                    List<Panel> panels_Guid = panels_New.FindAll(x => x.Guid == guid);
                    while (panels_Guid != null && panels_Guid.Count > 1)
                    {
                        guid = Guid.NewGuid();
                        panels_New[i] = Analytical.Create.Panel(guid, panels_New[i]);
                        panels_Guid = panels_New.FindAll(x => x.Guid == guid);

                        dictionary_Guid[guid] = panels_New[i].Guid;
                    }
                }

                panels_Filtered = panels_New;
            }

            if (panels_Filtered is not null)
            {
                foreach (Panel panel_Filtered in panels_Filtered)
                {
                    if (panel_Filtered is null)
                    {
                        continue;
                    }

                    dictionary[panel_Filtered.Guid] = panel_Filtered;

                    adjacencyCluster.AddObject(panel_Filtered);
                }
            }

            List<Panel> panels_Roof = adjacencyCluster.GetPanels().FindAll(x => x.PanelGroup == PanelGroup.Roof);

            List<Shell> shells_Roof = [];

            List<Shell> shells = Analytical.Query.Shells(panels_Filtered, 0.1, Tolerance.MacroDistance, Tolerance.Distance);
            if (shells is not null && shells.Count != 0)
            {
                double length = boundingBox3D.Max.Z - boundingBox3D.Min.Z;
                if (length > Tolerance.MacroDistance)
                {
                    Vector3D vector3D = Vector3D.WorldZ * length;

                    foreach (Panel panel_Roof in panels_Roof)
                    {
                        Face3D face3D = new Face3D(panel_Roof.GetFace3D().GetExternalEdge3D());
                        if (face3D is null || face3D.GetArea() < minArea)
                        {
                            continue;
                        }

                        double thinnessRatio = face3D.ThinnessRatio();

                        if (thinnessRatio < minThinnessRatio)
                        {
                            continue;
                        }

                        Extrusion extrusion = new Extrusion(face3D, vector3D);

                        shells_Roof.Add(extrusion.Shell());
                    }

                    List<Shell> shells_Result = [];
                    foreach (Shell shell in shells)
                    {
                        List<Shell> shells_Difference = Geometry.Spatial.Query.Difference(shell, shells_Roof);
                        if (shells_Difference == null || shells_Difference.Count == 0)
                        {
                            shells_Result.Add(shell);
                        }
                        else
                        {
                            shells_Result.AddRange(shells_Difference);
                        }
                    }

                    shells = shells_Result;
                }
            }

            AdjacencyCluster adjacencyCluster_New = Analytical.Create.AdjacencyCluster(shells, elevations[0], 0.001, minArea, 0.01, 0.0872664626, Tolerance.MacroDistance, Tolerance.Distance, Tolerance.Angle);

            List<Tuple<Panel, Face3D>> tuples_New = adjacencyCluster_New.GetPanels().ConvertAll(x => new Tuple<Panel, Face3D>(x, x.GetFace3D()));
            List<Tuple<Panel, Face3D>>? tuples_Filtered = panels_Filtered?.ConvertAll(x => new Tuple<Panel, Face3D>(x, x.GetFace3D()));

            List<Guid> guids_Filtered = panels_Filtered.ConvertAll(x => x.Guid);
            List<Tuple<Panel, Face3D>>? tuples_Rest = adjacencyCluster.GetPanels().FindAll(x => !guids_Filtered.Contains(x.Guid)).ConvertAll(x => new Tuple<Panel, Face3D>(x, x.GetFace3D()));


            HashSet<Guid> guids = [];
            for (int i = 0; i < tuples_New.Count; i++)
            {
                Point3D? point3D = tuples_New[i]?.Item2?.GetInternalPoint3D();
                if (point3D is null)
                {
                    continue;
                }

                List<Tuple<Panel, Face3D>>? tuples_Temp = tuples_Filtered.FindAll(x => x.Item1.PanelGroup == tuples_New[i].Item1.PanelGroup).FindAll(x => x.Item2.Distance(point3D) < 1);

                if (tuples_Temp.Count <= 0)
                {
                    tuples_Temp = tuples_Rest.FindAll(x => x.Item1.PanelGroup == tuples_New[i].Item1.PanelGroup).FindAll(x => x.Item2.Distance(point3D) < 1);
                }

                if (tuples_Temp.Count <= 0)
                {
                    continue;
                }

                tuples_Temp.Sort((x, y) => x.Item2.Distance(point3D).CompareTo(y.Item2.Distance(point3D)));

                Panel panel_Updated = Analytical.Create.Panel(tuples_New[i].Item1, tuples_Temp[0].Item1.Construction);

                adjacencyCluster_New.AddObject(panel_Updated);

                tuples_New[i] = new Tuple<Panel, Face3D>(panel_Updated, tuples_New[i].Item2);
            }


            foreach (Panel panel in panels_All)
            {
                List<Aperture>? apertures = panel?.Apertures;
                if (apertures is null || apertures.Count == 0)
                {
                    continue;
                }

                foreach (Aperture aperture in apertures)
                {
                    Face3D? face3D = aperture?.GetFace3D();
                    if (face3D is null)
                    {
                        continue;
                    }

                    Point3D point3D = face3D.InternalPoint3D();

                    tuples_New.Sort((x, y) => x.Item2.Distance(point3D).CompareTo(y.Item2.Distance(point3D)));

                    Panel panel_Temp = tuples_New[0].Item1;

                    face3D = tuples_New[0].Item2.GetPlane().Project(face3D);

                    List<Aperture> apertures_New = Analytical.Modify.AddApertures(panel_Temp, aperture.ApertureConstruction, face3D, true, Tolerance.MacroDistance, 0.5);
                    if (apertures_New != null && apertures_New.Count != 0)
                    {
                        guids.Add(panel_Temp.Guid);
                    }
                }
            }

            foreach (Guid guid in guids)
            {
                Panel? panel = tuples_New.Find(x => x.Item1.Guid == guid)?.Item1;
                if (panel is null)
                {
                    continue;
                }

                adjacencyCluster_New.AddObject(panel);
                dictionary[panel.Guid] = panel;
            }

            Dictionary<Space, Shell> dictionary_Shells = adjacencyCluster_New.ShellDictionary();

            List<Panel> panels_Cut = [];
            if (panels_Roof != null)
            {
                panels_Cut.AddRange(panels_Roof);
            }

            if (panels_Filtered != null)
            {
                panels_Cut.AddRange(panels_Filtered);
            }

            List<Panel> panels_Floor = adjacencyCluster.GetPanels().FindAll(x => x.PanelGroup == PanelGroup.Floor);
            if (panels_Floor != null)
            {
                panels_Cut.AddRange(panels_Floor);
            }

            foreach (Panel panel in panels_Cut)
            {
                Face3D? face3D = panel?.GetFace3D();
                if (face3D is null)
                {
                    continue;
                }

                List<Face3D> face3Ds_Split = Geometry.Spatial.Query.Split(face3D, dictionary_Shells.Values, Tolerance.MacroDistance, Tolerance.Angle, Tolerance.Distance);

                foreach (Face3D face3D_Split in face3Ds_Split)
                {
                    if (face3D_Split is null || face3D_Split.GetArea() < minArea)
                    {
                        continue;
                    }

                    double thinnessRatio = face3D_Split.ThinnessRatio();
                    if (thinnessRatio < minThinnessRatio)
                    {
                        continue;
                    }

                    Point3D point3D = face3D_Split.GetInternalPoint3D();
                    if (point3D is null)
                    {
                        continue;
                    }

                    Shell? shell = dictionary_Shells.Values.ToList()?.FindAll(x => x.GetBoundingBox().Inside(point3D, true, Tolerance.Distance))?.Find(x => x.Inside(point3D, Tolerance.Distance) || x.On(point3D, Tolerance.Distance));
                    if (shell is null)
                    {
                        Panel panel_New = Analytical.Create.Panel(panel!.Construction, PanelType.Shade, face3D_Split);
                        dictionary[panel_New.Guid] = panel_New;
                        adjacencyCluster_New.AddObject(panel_New);
                    }
                }

            }

            List<Space> spaces_Existing = adjacencyCluster.GetSpaces();
            if (spaces_Existing != null && spaces_Existing.Count != 0)
            {
                foreach (Space space_Existing in spaces_Existing)
                {
                    Point3D? point3D = space_Existing?.Location?.GetMoved(new Vector3D(0, 0, 0.1)) as Point3D;
                    if (point3D is null)
                    {
                        continue;
                    }

                    foreach (KeyValuePair<Space, Shell> keyValuePair in dictionary_Shells)
                    {
                        if (keyValuePair.Value.GetBoundingBox().Inside(point3D) && (keyValuePair.Value.Inside(point3D) || keyValuePair.Value.On(point3D)))
                        {
                            Space space = keyValuePair.Key;

                            space = new Space(space.Guid, space_Existing);

                            dictionary[space.Guid] = space;
                            adjacencyCluster_New.AddObject(space);

                            break;
                        }
                    }

                }
            }

            List<Tuple<Panel, Face3D>> tuples_Shade = adjacencyCluster_New.GetPanels().FindAll(x => x.PanelType == PanelType.Shade).ConvertAll(x => new Tuple<Panel, Face3D>(x, x.GetFace3D()));
            if (tuples_Shade != null)
            {
                foreach (Tuple<Panel, Face3D> tuple in tuples_Shade)
                {
                    List<Face3D> face3Ds_Split = Geometry.Spatial.Query.Split(tuple.Item2, tuples_Shade.ConvertAll(x => x.Item2), Tolerance.MacroDistance, Tolerance.Angle, Tolerance.Distance).FindAll(x => x.GetArea() > Tolerance.MacroDistance);
                    if (face3Ds_Split == null || face3Ds_Split.Count < 2)
                    {
                        continue;
                    }

                    Panel panel = tuple.Item1;

                    dictionary[panel.Guid] = panel;
                    adjacencyCluster_New.RemoveObject(panel);

                    foreach (Face3D face3D_Split in face3Ds_Split)
                    {
                        Panel panel_New = Analytical.Create.Panel(panel.Construction, PanelType.Shade, face3D_Split);

                        dictionary[panel_New.Guid] = panel_New;
                        adjacencyCluster_New.AddObject(panel_New);
                    }

                }
            }

            if (shells_Roof.Count > 0)
            {
                List<Panel> panels_Shade = adjacencyCluster_New.GetPanels().FindAll(x => x.PanelType == PanelType.Shade);
                if (panels_Shade != null)
                {
                    foreach (Panel panel_Shade in panels_Shade)
                    {
                        Point3D point3D = panel_Shade?.GetInternalPoint3D();
                        if (point3D is not null)
                        {
                            if (shells_Roof.Count != 0)
                            {
                                Shell? shell_Roof = shells_Roof.FindAll(x => x.GetBoundingBox().Inside(point3D, true, Tolerance.Distance))?.Find(x => x.Inside(point3D, Tolerance.Distance) && !x.On(point3D, Tolerance.Distance));
                                if (shell_Roof is not null)
                                {
                                    dictionary[panel_Shade.Guid] = panel_Shade;
                                    adjacencyCluster_New.RemoveObject(panel_Shade);
                                }
                            }
                        }
                    }


                }
            }

            if (removeUnusedSpaces)
            {
                List<Space> spaces = adjacencyCluster_New.GetSpaces();
                foreach (Space space in spaces)
                {
                    List<Panel> panels = adjacencyCluster_New.GetPanels(space);
                    if (panels == null || panels.Count == 0)
                    {
                        adjacencyCluster_New.RemoveObject(space);
                        dictionary[space.Guid] = space;
                    }
                }
            }

            if (replaceNameSpecialCharacters && !string.IsNullOrWhiteSpace(language))
            {
                adjacencyCluster_New.ReplaceNameSpecialCharacters(language);
            }

            progressBarWindowManager.Close();

            IEnumerable<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions(false, true);
            if (internalConditions != null)
            {
                adjacencyCluster_New.AddObjects(internalConditions);
            }

            sAMObjects = dictionary.Values;

            return new AnalyticalModel(analyticalModel, adjacencyCluster_New);
        }

        public static void Solve(this UIAnalyticalModel uIAnalyticalModel)
        {
            if (uIAnalyticalModel is null)
            {
                return;
            }

            AnalyticalModel? analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel is null)
            {
                return;
            }

            analyticalModel = Solve(analyticalModel, out IEnumerable<SAMObject>? sAMObjects);
            if(analyticalModel is null)
            {
                return;
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new AnalyticalModelModification(sAMObjects));
        }
    }
}
