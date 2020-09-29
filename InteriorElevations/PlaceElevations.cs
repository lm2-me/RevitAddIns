/*
LM2.Revit.InteriorElevations places ElevationMarkers and associated ViewSections for each placed room in your project.
Copyright(C) 2020  Lisa-Marie Mueller

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<https://www.gnu.org/licenses/> 
*/

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]
    
    public class PlaceInteriorElevations : IExternalCommand
    {
        private static Func<Curve, string> serializeCurve = c => "[" + c.GetEndPoint(0) + "," + c.GetEndPoint(1) + "]";
        private static Func<IEnumerable<Curve>, string> serializeCurves = cs => String.Join(",", cs.Select(serializeCurve));

        Autodesk.Revit.ApplicationServices.Application application;

        private static Config pluginConfig;

        private void Debug (string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;
            String pluginName = "PlaceInteriorElevations";
            
            this.application = commandData.Application.Application;

            Debug("user path" + doc.Application.CurrentUserAddinsLocation);
            Debug("user data path" + doc.Application.CurrentUsersAddinsDataFolderPath);
            Debug("all user path" + doc.Application.AllUsersAddinsLocation);
            pluginConfig = Utility.ReadConfig();
            Debug("telem url" + pluginConfig.telemetryURL);

            String slackMessage = ">Started";
            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage);

            List <ViewFamilyType> viewFamilyTypeList = GetViewFamilyTypeList(doc);
            List<Autodesk.Revit.DB.View> viewTemplateList = GetViewtemplate(doc);
            List<FilledRegionType> filledRegionList = GetFilledRegions(doc);
            List<Element> lineStyleList = GetLineStyles(doc);

            DialogPlaceIntElevations ieDialog = new DialogPlaceIntElevations(this.application, viewFamilyTypeList, viewTemplateList, filledRegionList, lineStyleList);
            Boolean? dialogResult = ieDialog.ShowDialog();

            int numRooms = 0;

            if (dialogResult == true)
            {
                try
                {
                    List<Room> allRooms = new FilteredElementCollector(doc)
                        .OfClass(typeof(SpatialElement))
                        .Select(e => e as Room)
                        .Where(e => e != null)
                        .ToList();

                    numRooms = allRooms.Count;

                    List<ViewPlan> intElevPlans = DocumentElevPlanViews(doc);

                    foreach (Room r in allRooms)
                    {
                        XYZ roomCenter = Utility.GetCenter(r);
                        if (roomCenter == null)
                        {
                            continue;
                        }
                        ViewPlan intElevPlanOfRoom = GetViewPlanOfRoom(doc, intElevPlans, r);
                        if (intElevPlanOfRoom == null)
                        {
                            continue;
                        }
                        using (Transaction tx = new Transaction(doc))
                        {
                            tx.Start("Place Elevations");

                            int viewScale = ieDialog.SelectedScale;

                            ElevationMarker marker;
                            List<ViewSection> placedIntElev = PlaceElevations(doc, roomCenter, intElevPlanOfRoom, ieDialog.SelectedViewFamilyType, viewScale, out marker);
                            Double angle = AngletoRotateElev(r);
                            RotateMarker(marker, angle, roomCenter);

                            foreach (ViewSection ie in placedIntElev)
                            {
                                AssignViewTemplate(ieDialog.SelectedViewTemplate, ie);
                                SetCropBox(ie, r, roomCenter);
                                try
                                {
                                    CreateFilledRegion(doc, ie, ieDialog.SelectedFilledRegion, ieDialog.SelectedLineStyle);
                                }
                                catch (Exception e)
                                {
                                    Debug("failed to create filled region " + e.ToString());
                                }
                                RenameElevation(doc, ie, r);
                            }


                            tx.Commit();
                        }

                    }
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
            int timeSavedPerRoom = 8;
            int ROI = timeSavedPerRoom * numRooms;
            string ROIString;
            ROIString = string.Format("{0:0.00} hours", ROI / 60f);


            String slackMessage2 = $">Completed\n>Rooms in Project: {numRooms} rooms\n>ROI for run: {ROI} minutes ({ROIString}) saved\nData\t{numRooms}\t{ROI}";

            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage2);

            return Result.Succeeded;
        }

        /// <summary>
        /// Gets a list of all View Family Types of type Elevation in the project
        /// </summary>
        /// <param name="doc">UI document</param>
        /// <returns>list of View Family Types</returns>
        public List<ViewFamilyType> GetViewFamilyTypeList(Document doc)
        {
 
            List <ViewFamilyType> viewFamilyTypeInterior = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(x => x.ViewFamily == ViewFamily.Elevation)
                .ToList();

            return viewFamilyTypeInterior;
        }

        /// <summary>
        /// Gets all view tempaltes in project
        /// </summary>
        /// <param name="doc">UI document</param>
        /// <returns>A list of View Templates</returns>
        public List<Autodesk.Revit.DB.View> GetViewtemplate(Document doc)
        {
            List<Autodesk.Revit.DB.View> viewTemplate = new FilteredElementCollector(doc)
                .OfClass(typeof(Autodesk.Revit.DB.View))
                .Cast<Autodesk.Revit.DB.View>()
                .Select(v => v as Autodesk.Revit.DB.View)
                .Where(v => v.IsTemplate)
                .ToList();
            return viewTemplate;
        }

        /// <summary>
        /// Collects all Filled Region Types in the project
        /// </summary>
        /// <param name="doc">UI Document</param>
        /// <returns>a list of Filled Region Types</returns>
        public List<FilledRegionType> GetFilledRegions(Document doc)
        {
            List<FilledRegionType> filledRegionTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FilledRegionType))
                .Select(f => f as FilledRegionType)
                .Where(f => f != null
                    && f.IsMasking == true)
                .ToList();

            if (filledRegionTypes.Count <= 0)
            {
                List<FilledRegionType> filledRegionTypesNotMasking = new FilteredElementCollector(doc)
                    .OfClass(typeof(FilledRegionType))
                    .Select(f => f as FilledRegionType)
                    .Where(f => f != null)
                    .ToList();

                return filledRegionTypesNotMasking;
            }

            return filledRegionTypes;
        }

        /// <summary>
        /// Gets all the line styles in the document
        /// </summary>
        /// <param name="doc">UI Document</param>
        /// <returns>List of line styles</returns>
        public List<Element> GetLineStyles(Document doc)
        {
            List<Element> lineStyleList = FilledRegion.GetValidLineStyleIdsForFilledRegion(doc)
                .Select(id => doc.GetElement(id))
                .ToList();

            return lineStyleList;
        }

        /// <summary>
        /// Finds the angle of the longest bounrady segment of the first wall enclosing a room
        /// </summary>
        /// <param name="r">room used to find boundary</param>
        /// <returns>the angle in radians to rotate the elevation marker by</returns>
        public double AngletoRotateElev(Room r)
        {
            IList<IList<BoundarySegment>> segmentList = r.GetBoundarySegments(new SpatialElementBoundaryOptions());

            IList<BoundarySegment> wallList = segmentList[0];

            BoundarySegment segment = null;

            foreach (BoundarySegment wbs in wallList)
            {
                if (segment == null)
                {
                    segment = wbs;
                }

                else if (wbs.GetCurve().Length > segment.GetCurve().Length)
                {
                    segment = wbs;
                }
            }

            XYZ EP1 = segment.GetCurve().GetEndPoint(0);
            XYZ EP2 = segment.GetCurve().GetEndPoint(1);

            Debug("segment" + segment);
            Debug("EP1" + EP1);
            Debug("EP2" + EP2);

            double angleLineEP1EP2 = Math.Atan2((EP2.Y) - (EP1.Y), (EP2.X) - (EP1.X));
            Debug("angleLineEP1EP2" + angleLineEP1EP2);

            return angleLineEP1EP2;
        }

        /// <summary>
        /// rotates the elevation marker based on the angle entered
        /// </summary>
        /// <param name="marker">the elevation marker to rotate</param>
        /// <param name="angleViewtoWall">angle used to rotate the marker</param>
        /// <param name="elevMarkerPosition"> the position of the marker around which to rotate</param>
        public void RotateMarker(ElevationMarker marker, double angleViewtoWall, XYZ elevMarkerPosition)
        {
            marker.get_BoundingBox(null);

            Line rotationAxis = Line.CreateBound(
                new XYZ(elevMarkerPosition.X, elevMarkerPosition.Y, elevMarkerPosition.Z),
                new XYZ(elevMarkerPosition.X, elevMarkerPosition.Y, elevMarkerPosition.Z + 10)
            );

            marker.Location.Rotate(rotationAxis, angleViewtoWall);
        }

        /// <summary>
        /// Renames the elevation view by room name, room number, and cardinal direction
        /// </summary>
        /// <param name="doc">UI document</param>
        /// <param name="intElev">interior elevation View Section</param>
        /// <param name="room">corresponding room</param>
        public void RenameElevation(Document doc, ViewSection intElev, Room room)
        {
            string roomNumber = room.Number;
            string roomName = room.Name;

            string viewDir = StringHelpers.Angle2Cardinal(
                doc,
                Utility.GetVectorAngle(intElev.ViewDirection)
            );

            string suffix;

            switch (viewDir)
            {
                case "NORTH":
                    suffix = "A";
                    break;
                case "EAST":
                    suffix = "B";
                    break;
                case "SOUTH":
                    suffix = "C";
                    break;
                case "WEST":
                    suffix = "D";
                    break;
                default:
                    suffix = "?";
                    break;
            }

            try
            {
                intElev.Name = roomNumber + " - " + roomName + " - " + suffix;
            }
            catch
            {
                return;
            }

        }

        /// <summary>
        /// Creates boundaries for masking region based on the room boundaries. the 
        /// inner loop is the interior elevation boundary, the outside loop is offset by 6 inches
        /// </summary>
        /// <param name="intElev">interior elevation View Section</param>
        /// <returns>2 sets of curve loops to use as boundaries for creating a masking region</returns>
        public List<CurveLoop> FilledRegionBoundary(ViewSection intElev)
        {
            BoundingBoxXYZ iecb = intElev.CropBox;

            XYZ cbboundsmin = iecb.get_Bounds(0);
            XYZ cbboundsmax = iecb.get_Bounds(1);

            double[][] transform = Matrix.transform2matrix(iecb.Transform);
            double[][] inverse = Matrix.invert(transform);

            //inside boundary

            XYZ roombb1 = new XYZ(
                cbboundsmin.X + 1,
                cbboundsmin.Y + 1,
                cbboundsmin.Z
                );
            XYZ roombb2 = new XYZ(
                cbboundsmin.X + 1,
                cbboundsmax.Y - 1,
                cbboundsmin.Z
                );
            XYZ roombb3 = new XYZ(
                cbboundsmax.X - 1,
                cbboundsmax.Y - 1,
                cbboundsmin.Z
                );
            XYZ roombb4 = new XYZ(
                cbboundsmax.X - 1,
                cbboundsmin.Y + 1,
                cbboundsmin.Z
                );

            XYZ roombb1T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb1))) + iecb.Transform.Origin;
            XYZ roombb2T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb2))) + iecb.Transform.Origin;
            XYZ roombb3T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb3))) + iecb.Transform.Origin;
            XYZ roombb4T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb4))) + iecb.Transform.Origin;

            Line li0 = Line.CreateBound(roombb4T, roombb1T);
            Line li1 = Line.CreateBound(roombb1T, roombb2T);
            Line li2 = Line.CreateBound(roombb2T, roombb3T);
            Line li3 = Line.CreateBound(roombb3T, roombb4T);

            IList<Curve> insidecurves = new List<Curve>()
            {
                li0,
                li1,
                li2,
                li3
            };

            XYZ roombbout1 = new XYZ(
                cbboundsmin.X - 0.5,
                cbboundsmin.Y - 0.5,
                cbboundsmin.Z
                );
            XYZ roombbout2 = new XYZ(
                cbboundsmin.X - 0.5,
                cbboundsmax.Y + 0.5,
                cbboundsmin.Z
                );
            XYZ roombbout3 = new XYZ(
                cbboundsmax.X + 0.5,
                cbboundsmax.Y + 0.5,
                cbboundsmin.Z
                );
            XYZ roombbout4 = new XYZ(
                cbboundsmax.X + 0.5,
                cbboundsmin.Y - 0.5,
                cbboundsmin.Z
                );

            XYZ roombbout1T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout1))) + iecb.Transform.Origin;
            XYZ roombbout2T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout2))) + iecb.Transform.Origin;
            XYZ roombbout3T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout3))) + iecb.Transform.Origin;
            XYZ roombbout4T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout4))) + iecb.Transform.Origin;

            Line lo0 = Line.CreateBound(roombbout4T, roombbout1T);
            Line lo1 = Line.CreateBound(roombbout1T, roombbout2T);
            Line lo2 = Line.CreateBound(roombbout2T, roombbout3T);
            Line lo3 = Line.CreateBound(roombbout3T, roombbout4T);

            IList<Curve> outsidecurves = new List<Curve>()
            {
                lo0,
                lo1,
                lo2,
                lo3
            };

            //boundaries
            CurveLoop inside = CurveLoop.Create(insidecurves);
            CurveLoop outside = CurveLoop.Create(outsidecurves);
            List<CurveLoop> frBoundaries = new List<CurveLoop>(){
                inside,
                outside
            };

            return frBoundaries;
        }

        /// <summary>
        /// Creates a filled region for the elevatin using the previously found boundaries 
        /// and the user selected filled region type and line style
        /// </summary>
        /// <param name="doc">UI Document</param>
        /// <param name="intElev">Interior Elevation</param>
        /// <param name="filledRegionId">Id of user selected filled region</param>
        /// <param name="lineStyle">Id of user selected line style</param>
        public void CreateFilledRegion(Document doc, ViewSection intElev, ElementId filledRegionId, ElementId lineStyle)
        {
            List<CurveLoop> filledRegionBoundaries = FilledRegionBoundary(intElev);

            FilledRegion region = FilledRegion.Create(doc, filledRegionId, intElev.Id, filledRegionBoundaries);

            region.SetLineStyleId(lineStyle);

        }

        /// <summary>
        /// Sets the interior elevation view's cropbox based on the room's bounding box
        /// </summary>
        /// <param name="intElev">Interior Elevations</param>
        /// <param name="r">room</param>
        /// <returns>room bounding box</returns>
        public void SetCropBox(ViewSection intElev, Room r, XYZ markerPosition)
        {
            BoundingBoxXYZ iecb = intElev.CropBox;
            XYZ cbboundsmin = iecb.get_Bounds(0);
            XYZ cbboundsmax = iecb.get_Bounds(1);

            Debug("room " + r.Name + r.Number);
            Debug("cbboundsmin " + cbboundsmin);
            Debug("cbboundsmax " + cbboundsmax);

            Debug("cb origin " + iecb.Transform.Origin);

            iecb.Transform.Origin = markerPosition;
            Debug("cb origin reset 1 " + iecb.Transform.Origin);

            BoundingBoxXYZ rbb = r.get_BoundingBox(null);
            XYZ rbbmin = rbb.get_Bounds(0);
            XYZ rbbmax = rbb.get_Bounds(1);

            Debug("rbbmin " + rbbmin);
            Debug("rbbmax " + rbbmax);

            Debug("rbb origin " + rbb.Transform.Origin);

            double[][] transform = Matrix.transform2matrix(iecb.Transform);

            double centerOffsetX = markerPosition.X - iecb.Transform.Origin.X;
            double centerOffsetY = markerPosition.Y - iecb.Transform.Origin.Y;

            XYZ offsetVector = new XYZ(
                centerOffsetX,
                centerOffsetY,
                0);

            double[] offsetMatrix = Matrix.xyz2matrix(offsetVector);
            double[] offsetTMatrix = Matrix.dot(transform, offsetMatrix);
            XYZ offsetT = new XYZ(
                offsetTMatrix[0],
                offsetTMatrix[1],
                offsetTMatrix[2]);

            //double[] minXMatrix = Matrix.xyz2matrix(new XYZ(cbboundsmin.X, 0, 0));
            //double[] maxXMatrix = Matrix.xyz2matrix(new XYZ(cbboundsmax.X, 0, 0));
            //double[] minXTMatrix = Matrix.dot(transform, minXMatrix);
            //double[] maxXTMatrix = Matrix.dot(transform, maxXMatrix);

            double transformangle = Math.Atan2(offsetVector.Y, offsetVector.X);
            double transformangle2 = Math.Acos(transform[0][0]);

            Debug("transformangle " + transformangle + ", " + (transformangle * 180 / Math.PI));
            Debug("transformangle2 " + transformangle2 + ", " + (transformangle2 * 180 / Math.PI));

            double transformanglePos = transformangle < 0 ? Math.PI + transformangle : transformangle;

            int transformangleRounded = (int)(Math.Round(transformanglePos * 100));

            Debug("transformangleRounded " + transformangleRounded);

            var segments = r.GetBoundarySegments(new SpatialElementBoundaryOptions()).FirstOrDefault();

            if (segments == null)
            {
                Debug("null segments");
                return;
            }

            double wallLength = 4;

            foreach (var s in segments)
            {
                var c = s.GetCurve();
                var ep1 = c.GetEndPoint(0);
                var ep2 = c.GetEndPoint(1);

                double rise = ep2.Y - ep1.Y;
                double run = ep2.X - ep1.X;

                double angle = Math.Atan2(rise, run);
                double anglePos = angle < 0 ? Math.PI + angle : angle;

                int angleRounded = (int)(Math.Round(anglePos * 100));

                double length = Math.Sqrt(Math.Pow(rise, 2) + Math.Pow(run, 2));

                if (angleRounded == transformangleRounded && length > wallLength)
                {
                    wallLength = length;
                    Debug("selected wallLength " + wallLength);
                }

                Debug("angle " + angle + ", " + (angle * 180 / Math.PI));
                Debug("angleRounded " + angleRounded);
                Debug("length " + length);
            }

            Debug("centerOffset v " + offsetVector);
            Debug("centerOffset vT " + offsetT);

            Debug("wallLength " + wallLength);

            XYZ newcbmin = new XYZ(
                offsetT.X - wallLength / 2 - 1,
                rbbmin.Z - 1,
                cbboundsmin.Z);

            XYZ newcbmax = new XYZ(
                offsetT.X + wallLength / 2 + 1,
                rbbmax.Z + 1,
                cbboundsmax.Z);



            iecb.set_Bounds(0, newcbmin);
            iecb.set_Bounds(1, newcbmax);

            Debug("reset bounds 0 " + iecb.get_Bounds(0));
            Debug("reset bounds 1 " + iecb.get_Bounds(1));

            intElev.CropBox = iecb;
            intElev.CropBoxVisible = false;

            Debug("cb origin reset 2 " + iecb.Transform.Origin);

        }

        /// <summary>
        /// Assigns the user selected view tempalte to the interior elevation view
        /// </summary>
        /// <param name="viewTemplate">ID of user selected view template</param>
        /// <param name="intElev">interior elevation</param>
        public void AssignViewTemplate(ElementId viewTemplate, ViewSection intElev)
        {
            intElev.ViewTemplateId = viewTemplate;
        }

        /// <summary>
        /// Places interior elevation makers and inter elevation views for the room
        /// </summary>
        /// <param name="doc">UI Document</param>
        /// <param name="center">coordinate where marker will be placed</param>
        /// <param name="intElevPlan">the plan in which the marker will be visible</param>
        /// <param name="selectedFamilyType">user selected view family type</param>
        /// <param name="scale">user selected scale for interior elevations</param>
        /// <returns>a list of the interior elevations created</returns>
        public List<ViewSection> PlaceElevations(Document doc, XYZ center, ViewPlan intElevPlan, ElementId selectedFamilyType, int scale, out ElevationMarker marker)
        {
            marker = ElevationMarker.CreateElevationMarker(doc, selectedFamilyType, center, scale);
            Parameter p = intElevPlan.get_Parameter(BuiltInParameter.VIEW_PHASE);
            marker.get_Parameter(BuiltInParameter.PHASE_CREATED).Set(p.AsElementId());
            List<ViewSection> intElevList = new List<ViewSection>();

            for (int markerindex = 0; markerindex < marker.MaximumViewCount; markerindex++)
            {
                if (marker.IsAvailableIndex(markerindex))
                {
                    ViewSection intElev = marker.CreateElevation(doc, intElevPlan.Id, markerindex);
                    intElev.get_Parameter(BuiltInParameter.VIEW_PHASE).Set(p.AsElementId());
                    intElevList.Add(intElev);
                }
            }

            return intElevList;
        }

        /// <summary>
        /// Gets a view in which the room is visible
        /// </summary>
        /// <param name="doc">UI Document</param>
        /// <param name="intElevPlans">A list of view plans</param>
        /// <param name="r">room</param>
        /// <returns>if founr, the view plan in which the room is visible. If none can be founded returns null</returns>
        public ViewPlan GetViewPlanOfRoom(Document doc, List<ViewPlan> intElevPlans, Room r)
        {
            foreach (ViewPlan vp in intElevPlans)
            {
                Room RoominView = new FilteredElementCollector(doc, vp.Id)
                    .OfClass(typeof(SpatialElement))
                    .Select(e => e as Room)
                    .Where(e => e != null && e.Id.IntegerValue == r.Id.IntegerValue)
                    .FirstOrDefault();
                if (RoominView != null)
                {
                    return vp;
                }
            }
            return null;

        }

        /// <summary>
        /// Creates list of View Plans where the name contains "Interior Elevation"
        /// </summary>
        /// <param name="doc">UI Document</param>
        /// <returns>list of ViewPlans</returns>
        public List<ViewPlan> DocumentElevPlanViews(Document doc)
        {
            List<ViewPlan> viewIntElevPlans = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(x => x.Name.Contains("Interior Elevations"))
                .ToList();
  

            if (viewIntElevPlans == null)
            {
                throw new Exception("Cannot find View Plans where name containts 'Interior Elevations'");
            }

            return viewIntElevPlans;

        }

}

}
