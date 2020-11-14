using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]

    public class TagMaterialsInteriorElevations : IExternalCommand
    {
        Autodesk.Revit.ApplicationServices.Application application;

        //private static Config pluginConfig;

        private void Debug(string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;
            String pluginName = "Tag Materials in Interior Elevation";

            this.application = commandData.Application.Application;

            //Debug("user path" + doc.Application.CurrentUserAddinsLocation);
            //Debug("user data path" + doc.Application.CurrentUsersAddinsDataFolderPath);
            //Debug("all user path" + doc.Application.AllUsersAddinsLocation);
            //pluginConfig = Utility.ReadConfig();
            //Debug("telem url" + pluginConfig.telemetryURL);

            //String slackMessage = ">Started";
            //Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage);

            bool dialogResult = true;

           if (dialogResult == true)
           {
                try
                {
                    View startingView = UIdoc.ActiveView;
                    List<ViewSection>  elevViews = GetAllIntElevViews(doc);
                   
                    foreach (ViewSection v in elevViews)
                    {
                        List<Wall> wallsInView = GetWallsInView(doc, v);
                        Tuple<XYZ, XYZ> elevEndPoints = GetElevEndpoints(v);

                        List<ElementId> tagElements = new List<ElementId>();
                        List<ElementId> invalidElements = new List<ElementId>() { ElementId.InvalidElementId };

                        UIdoc.ActiveView = v;
                        
                        Debug("View Name " + v.Name);

                        foreach(Wall w in wallsInView)
                        {
                            if (w.CurtainGrid != null)
                            {
                                continue;
                            }

                            Debug("Wall ID " + w.Id);
                            Reference wall = new Reference(w);

                            XYZ tagLocation = GetTagLocation(doc, w, elevEndPoints, v);
                            Debug("Tag Location " + tagLocation);

                            if (tagLocation == null)
                            {
                                continue;
                            }

                            try
                            {
                                IndependentTag tag;
                                using (Transaction tx = new Transaction(doc))
                                {
                                    tx.Start("Tag Materials");

                                    tag = IndependentTag.Create(doc, v.Id, wall, false, TagMode.TM_ADDBY_MATERIAL, TagOrientation.Horizontal, tagLocation);
                                    tagElements.Add(tag.Id);

                                    tx.Commit();
                                }

                                UIdoc.RefreshActiveView();

                                using (Transaction tx = new Transaction(doc))
                                {
                                    tx.Start("Add Leader");

                                    tag.HasLeader = true;
                                    tag.LeaderEndCondition = LeaderEndCondition.Free;
                                    tag.TagHeadPosition = tagLocation;

                                    tag.LeaderEnd = new XYZ
                                        (
                                        tagLocation.X,
                                        tagLocation.Y,
                                        tagLocation.Z + 3
                                        );

                                    Debug("Tag Leader End " + tag.LeaderEnd);

                                    tx.Commit();
                                }
                            }

                            catch (Exception ex1)
                            {
                                message = ex1.ToString();
                                return Result.Failed;
                            }
                        }

                        //refresh view
                        UIdoc.Selection.SetElementIds(tagElements);
                        UIdoc.RefreshActiveView(); 
                        UIdoc.Selection.SetElementIds(invalidElements);

                    }

                    UIdoc.ActiveView = startingView;

                }

                catch (Exception ex)
                {
                    message = ex.ToString();
                    return Result.Failed;
                }
            }

            else
            {
                return Result.Cancelled;
            }

            //calculated 8 minutes saved per room automating elevations
            //int timeSavedPerRoom = 8;
            //int ROI = timeSavedPerRoom * numRooms;
            //string ROIString;
            //ROIString = string.Format("{0:0.00} hours", ROI / 60f);


            //String slackMessage2 = $">Completed\n>Rooms in Project: {numRooms} rooms\n>ROI for run: {ROI} minutes ({ROIString}) saved\nData\t{numRooms}\t{ROI}";

            //Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage2);

            return Result.Succeeded;
        }

        public List<ViewSection> GetAllIntElevViews(Document doc)
        {
            ElementId interiorTypeId = FindFamilyTypeId(doc);

            List<ViewSection> intElevs = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSection))
                .Select(ie => ie as ViewSection)
                .Where(ie => ie.ViewType == ViewType.Elevation && ie.GetTypeId() == interiorTypeId)
                .ToList();

            return intElevs;
        }

        public ElementId FindFamilyTypeId(Document doc)
        {
            ViewFamilyType viewFamilyTypeInterior = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Elevation && x.Name.ToLower().Contains("int"));
            if (viewFamilyTypeInterior == null)
            {
                throw new Exception("Cannot find View Family Type containing name 'int'.");
            }

            return viewFamilyTypeInterior.Id;
        }

        public Tuple<XYZ, XYZ> GetElevEndpoints (ViewSection view)
        {
            XYZ vpMin = view.CropBox.Min;
            XYZ vpMax = view.CropBox.Max;

            Debug("vpMin " + vpMin);
            Debug("vpMax " + vpMax);

            XYZ endpoint1 = new XYZ(
                vpMin.X,
                vpMin.Y,
                vpMax.Z);

            XYZ endpoint2 = new XYZ(
                vpMax.X,
                vpMin.Y,
                vpMax.Z);

            Tuple<XYZ, XYZ> viewEndpoints = new Tuple<XYZ, XYZ>(endpoint1, endpoint2);

            Debug("endpoint1 " + endpoint1);
            Debug("endpoint2 " + endpoint2);

            return viewEndpoints;
        }

        public List<Wall> GetWallsInView(Document doc, ViewSection view)
        {
            List<Wall> walls = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .ToList();

            return walls;
        }

        public XYZ GetTagLocation(Document doc, Wall wall, Tuple<XYZ, XYZ> viewEndPoints, ViewSection view)
        {
            Debug("view " + view.Name);

            XYZ vep1 = viewEndPoints.Item1;
            XYZ vep2 = viewEndPoints.Item2;

            LocationCurve locationCurve = wall.Location as LocationCurve;

            XYZ ep1 = locationCurve.Curve.GetEndPoint(0);
            XYZ ep2 = locationCurve.Curve.GetEndPoint(1);

            Debug("ep1 " + ep1);
            Debug("ep2 " + ep2);

            //Transform wall endpoints to elevation view
            BoundingBoxXYZ iecb = view.CropBox;

            double[][] transform = Matrix.transform2matrix(iecb.Transform);
            double[][] inverse = Matrix.invert(transform);

            Debug("transform " + transform);
            Debug("inverse " + inverse);

            XYZ ep1T = Matrix.matrix2xyz(Matrix.dot(transform, Matrix.xyz2matrix(ep1 - iecb.Transform.Origin)));
            XYZ ep2T = Matrix.matrix2xyz(Matrix.dot(transform, Matrix.xyz2matrix(ep2 - iecb.Transform.Origin)));

            Debug("ep1T " + ep1T);
            Debug("ep2T " + ep2T);

            XYZ newep1 = ep1T.X < ep2T.X ? ep1T : ep2T;
            XYZ newep2 = ep1T.X > ep2T.X ? ep1T : ep2T;

            if (newep1.Z < view.CropBox.Min.Z || newep2.Z < view.CropBox.Min.Z)
            {
                return null;
            }

            //Compare wall elevation
            if (newep1.X < vep1.X)
            {
                newep1 = vep1;
            }

            if (newep2.X > vep2.X)
            {
                newep2 = vep2;
            }

            if (newep1.X == newep2.X)
            {
                return null;
            }


            Debug("newep1 " + newep1);
            Debug("newep2 " + newep2);

            XYZ centerT = (newep1 + newep2) / 2;

            Debug("centerT " + centerT);

            if (centerT.X < newep1.X || centerT.X > newep2.X)
            {
                return null;
            }

            //Transform point back to project space
            XYZ center = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(centerT))) + iecb.Transform.Origin;

            Debug("center " + center);

            XYZ location = new XYZ (
                center.X,
                center.Y,
                center.Z - 2);

            return location;
        }

    }
}
