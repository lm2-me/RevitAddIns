/*
LM2.Revit.InteriorElevationsGoal3 places ElevationMarkers and associated ViewSections for each placed room in your project.
Copyright(C) 2019  Lisa-Marie Mueller

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
using System.Linq;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]
    public class InteriorElevationsGoal2 : IExternalCommand
    {
        private static Func<Curve, string> serializeCurve = c => "[" + c.GetEndPoint(0) + "," + c.GetEndPoint(1) + "]";
        private static Func<IEnumerable<Curve>, string> serializeCurves = cs => String.Join(",", cs.Select(serializeCurve));

        Application application;

        private void Debug (string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            this.application = commandData.Application.Application;

            try
            {
                List<Room> allRooms = new FilteredElementCollector(doc)
                    .OfClass(typeof(SpatialElement))
                    .Select(e => e as Room)
                    .Where(e => e != null)
                    .ToList();

                List<ViewPlan> intElevPlans = DocumentElevPlanViews(doc);
                View intElevTemplate = GetViewtemplate(doc);
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

                        List<ViewSection> placedIntElev = PlaceElevations(doc, roomCenter, intElevPlanOfRoom);
                        foreach (ViewSection ie in placedIntElev)
                        {
                            AssignViewTemplate(intElevTemplate, ie);
                            BoundingBoxXYZ roombb = SetCropBox(ie, r);
                            CreateFilledRegion(doc, ie, roombb);
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

            return Result.Succeeded;
        }

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

            XYZ roombb1T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb1)));
            XYZ roombb2T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb2)));
            XYZ roombb3T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb3)));
            XYZ roombb4T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombb4)));

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

            XYZ roombbout1T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout1)));
            XYZ roombbout2T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout2)));
            XYZ roombbout3T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout3)));
            XYZ roombbout4T = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(roombbout4)));

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

        public void CreateFilledRegion(Document doc, ViewSection intElev, BoundingBoxXYZ roombb)
        {
            List<CurveLoop> filledRegionBoundaries = FilledRegionBoundary(intElev);
            ElementId filledRegionTypeId = new FilteredElementCollector(doc)
                .OfClass(typeof(FilledRegionType))
                .Select(f => f as FilledRegionType)
                .Where(f => f != null
                    && f.BackgroundPatternColor.Red == 255
                    && f.BackgroundPatternColor.Blue == 255
                    && f.BackgroundPatternColor.Green == 255
                    && f.IsMasking == true)
                .Select(f => f.Id)
                .FirstOrDefault();

            FilledRegion region = FilledRegion.Create(doc, filledRegionTypeId, intElev.Id, filledRegionBoundaries);
            // set inside and outside lineweight

            Element lineStyle = FilledRegion.GetValidLineStyleIdsForFilledRegion(doc)
                .Select(id => doc.GetElement(id))
                .Where(el => el.Name.Contains("05") && el.Name.ToLower().Contains("solid"))
                .FirstOrDefault();


            region.SetLineStyleId(lineStyle.Id);

        }

        public BoundingBoxXYZ SetCropBox(ViewSection intElev, Room r)
        {
            BoundingBoxXYZ iecb = intElev.CropBox;
            XYZ cbboundsmin = iecb.get_Bounds(0);
            XYZ cbboundsmax = iecb.get_Bounds(1);

            BoundingBoxXYZ rbb = r.get_BoundingBox(null);
            XYZ rbbmin = rbb.get_Bounds(0);
            XYZ rbbmax = rbb.get_Bounds(1);

            double[][] transform = Matrix.transform2matrix(iecb.Transform);

            XYZ rbbmintransformed = new XYZ
            (
                rbbmin.X * transform[0][0] + rbbmin.Y * transform[0][1] + rbbmin.Z * transform[0][2],
                rbbmin.X * transform[1][0] + rbbmin.Y * transform[1][1] + rbbmin.Z * transform[1][2],
                rbbmin.X * transform[2][0] + rbbmin.Y * transform[2][1] + rbbmin.Z * transform[2][2]
            );

            XYZ rbbmaxtransformed = new XYZ
            (
                rbbmax.X * transform[0][0] + rbbmax.Y * transform[0][1] + rbbmax.Z * transform[0][2],
                rbbmax.X * transform[1][0] + rbbmax.Y * transform[1][1] + rbbmax.Z * transform[1][2],
                rbbmax.X * transform[2][0] + rbbmax.Y * transform[2][1] + rbbmax.Z * transform[2][2]
            );

            double minX = rbbmintransformed.X < rbbmaxtransformed.X ? rbbmintransformed.X : rbbmaxtransformed.X;
            double minY = rbbmintransformed.Y < rbbmaxtransformed.Y ? rbbmintransformed.Y : rbbmaxtransformed.Y;

            double maxX = rbbmintransformed.X > rbbmaxtransformed.X ? rbbmintransformed.X : rbbmaxtransformed.X;
            double maxY = rbbmintransformed.Y > rbbmaxtransformed.Y ? rbbmintransformed.Y : rbbmaxtransformed.Y;
            
            XYZ rbbboundsmin = new XYZ(
                minX,
                minY,
                cbboundsmin.Z);
            XYZ rbbboundsmax = new XYZ(
                maxX,
                maxY,
                cbboundsmax.Z);


            BoundingBoxXYZ rbbtransformed = new BoundingBoxXYZ();
            rbbtransformed.set_Bounds(0, rbbboundsmin);
            rbbtransformed.set_Bounds(1, rbbboundsmax);


            XYZ rbbboundsminextended = new XYZ(
                minX - 1,
                minY - 1,
                cbboundsmin.Z);
            XYZ rbbboundsmaxextended = new XYZ(
                maxX + 1,
                maxY + 1,
                cbboundsmax.Z);

           
            intElev.CropBox.Min = rbbboundsminextended;
            intElev.CropBox.Max = rbbboundsmaxextended;
            iecb.set_Bounds(0, rbbboundsminextended);
            iecb.set_Bounds(1, rbbboundsmaxextended);
            intElev.CropBox = iecb;
            intElev.CropBoxVisible = false;

            return rbb;
        }

        public void AssignViewTemplate(View viewTemplate, ViewSection intElev)
        {
            intElev.ViewTemplateId = viewTemplate.Id;
        }

        public View GetViewtemplate(Document doc) 
        {
            //add pop up to select interior elevation view template
            View viewTemplate = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Select(v => v as View)
                .Where(v => v.IsTemplate && v.Name.ToLower().Contains("interior") && v.Name.ToLower().Contains("elevation"))
                .FirstOrDefault();
            return viewTemplate;
        }

        public List<ViewSection> PlaceElevations(Document doc, XYZ center, ViewPlan intElevPlan)
        {
            //add pop up to input scale
            //current scale set to 1/8"
            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, FindFamilyTypeId(doc), center, 96);
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

        public ElementId FindFamilyTypeId(Document doc)
        {
            ViewFamilyType viewFamilyTypeInterior = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Elevation && x.Name.Contains("Interior"));
            if (viewFamilyTypeInterior == null)
            {
                throw new Exception("Cannot find View Family Type containing name Interior ");
            }

            return viewFamilyTypeInterior.Id;
        }
        
    }

}
