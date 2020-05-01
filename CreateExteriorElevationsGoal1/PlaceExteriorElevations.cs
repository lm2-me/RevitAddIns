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
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]

    public class PlaceExteriorElevations : IExternalCommand
    {
        Autodesk.Revit.ApplicationServices.Application application;

        private void Debug(string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            this.application = commandData.Application.Application;

            Debug("I got here before construct dialog");
            CreateExteriorElevationInput1 wallSelectionForm = new CreateExteriorElevationInput1(UIdoc);
            Debug("I got here before show dialog");
            wallSelectionForm.ShowDialog();
            Debug($"I got here after show dialog {wallSelectionForm.DialogResult}");

            if (wallSelectionForm.DialogResult != true)
            {
                return Result.Cancelled;
            }

            Selection userObjectSelection = UIdoc.Selection;
            IList<Reference> userSelection = userObjectSelection.PickObjects(ObjectType.Element);

            List<Wall> userWallSelection = new List<Wall>();

            foreach (Reference s in userSelection)
            {
                Element el = UIdoc.Document.GetElement(s.ElementId);
                Wall w = el as Wall;

                if (w != null)
                {
                    userWallSelection.Add(w);
                }

            }

            if (0 == userWallSelection.Count)
            {
                //no elements selected
                TaskDialog.Show("Revit", "There are no elements selected.");
            }

            //user wall selction
            List<Wall> wallsToUse = userWallSelection;
            Debug($"Received {wallsToUse.Count} walls");

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Place Elevations");

                foreach (Wall w in wallsToUse)
                {
                    BoundingBoxXYZ wbb = w.get_BoundingBox(null);
                    XYZ elevMarkerPosition = GetElevationMarkerPosition(w);
                    this.Debug("wall Id" + w.Id);

                    this.Debug("wall LevelId" + w.LevelId);

                    using (ElevationMarker marker = PlaceMarker(doc, elevMarkerPosition, w))
                    {
                        XYZ elevViewSectionNormal = GetViewSectionNormal(doc, marker);
                        double angleViewtoWall = GetAngleViewtoWall(elevViewSectionNormal, w);

                        RotateMarker(marker, angleViewtoWall, elevMarkerPosition);

                        SetCropBox(doc, marker, w);
                    }

                    //set view filter
                    //option to hide elevaion marker in view? maybe put on workset
                }

                tx.Commit();
            }

            return Result.Succeeded;

        }

        public XYZ GetElevationMarkerPosition(Wall w)
        {

            LocationCurve wallCurve = w.Location as LocationCurve;
            XYZ wallCenter;
            XYZ offsetCenter;
            XYZ wNormal;



            if ((wallCurve.Curve as Arc) != null)
            {
                Arc wc = wallCurve.Curve as Arc;
                bool concave;
                //multiply by YDirection to accomidate for curved walls drawn endpoint(1) to endpoint(0)
                double wNormalAngle = NormalofCurvedWall(w, out concave);
                XYZ CWEP1 = wallCurve.Curve.GetEndPoint(0);
                XYZ CWEP2 = wallCurve.Curve.GetEndPoint(1);
                this.Debug("CWEP1" + CWEP1);
                this.Debug("CWEP2" + CWEP2);

                double angleLineEP1EP2 = Math.Atan2((CWEP2.Y) - (CWEP1.Y), (CWEP2.X) - (CWEP1.X));
                this.Debug("concave " + concave);
                this.Debug("wNormalAngle " + wNormalAngle);
                this.Debug("angleLineEP1EP2 " + angleLineEP1EP2);

                wNormalAngle = - (wNormalAngle - angleLineEP1EP2);
                this.Debug("wNormalAngle 2" + wNormalAngle);

                if (concave)
                {

                    double[][] rotationMatrix = Matrix.ZAxisRotation(wNormalAngle);
                    double[] orientaitonMatrix = Matrix.xyz2matrix(w.Orientation);
                    double[] rotatedMatrix = Matrix.dot(rotationMatrix, orientaitonMatrix);
                    wNormal = Matrix.matrix2xyz(rotatedMatrix);
                    this.Debug("wNormal" + wNormal);

                    wallCenter = new XYZ(
                        (CWEP1.X + CWEP2.X) / 2,
                        (CWEP1.Y + CWEP2.Y) / 2,
                        CWEP1.Z);
                    this.Debug("wallCenter arc concave" + wallCenter);
                }

                else
                {
                    if (w.CurtainGrid != null)
                    {
                        wNormalAngle -= Math.PI;
                    }

                    //use negative wNormalAngle because we rotate around Z axis which is normally counter clockwise but when rotating marker around Z direction, it is clockwise
                    double[][] rotationMatrix = Matrix.ZAxisRotation(- wNormalAngle);
                    double[] orientaitonMatrix = Matrix.xyz2matrix(w.Orientation);
                    double[] rotatedMatrix = Matrix.dot(rotationMatrix, orientaitonMatrix);
                    wNormal = Matrix.matrix2xyz(rotatedMatrix);
                    this.Debug("wNormal" + wNormal);

                    wallCenter = new XYZ
                    (
                    ((CWEP1.X + CWEP2.X) / 2) + (wc.Radius * wNormal.X),
                    ((CWEP1.Y + CWEP2.Y) / 2) + (wc.Radius * wNormal.Y),
                    CWEP1.Z + (wc.Radius * wNormal.Z)
                    );

                    this.Debug("wallCenter arc convex" + wallCenter);
                }

                this.Debug("wall center arc" + wallCenter);
            }

            else
            {
                XYZ EP1 = wallCurve.Curve.GetEndPoint(0);
                XYZ EP2 = wallCurve.Curve.GetEndPoint(1);

                wallCenter = new XYZ(
                    (EP1.X + EP2.X) / 2,
                    (EP1.Y + EP2.Y) / 2,
                    EP1.Z);
                this.Debug("wall center line" + wallCenter);

                this.Debug("wall center" + wallCenter);

                wNormal = w.Orientation;

                if (w.CurtainGrid != null)
                {
                    double[][] rotationMatrixCW = Matrix.ZAxisRotation(Math.PI);
                    double[] orientaitonMatrixCW = Matrix.xyz2matrix(wNormal);
                    double[] rotatedMatrixCW = Matrix.dot(rotationMatrixCW, orientaitonMatrixCW);
                    wNormal = Matrix.matrix2xyz(rotatedMatrixCW);
                }

                this.Debug("wall normal" + wNormal);

            }

            offsetCenter = new XYZ(
            wNormal.X * 5 + wallCenter.X,
            wNormal.Y * 5 + wallCenter.Y,
            wNormal.Z * 5 + wallCenter.Z);

            return offsetCenter;
        }

        public ElevationMarker PlaceMarker(Document doc, XYZ elevMarkerPosition, Wall w)
        {
            ViewPlan plan = DocumentElevPlanViews(doc, w);
            Parameter p = plan.get_Parameter(BuiltInParameter.VIEW_PHASE);
            //ElementId[] toHide = new ElementId[1];

            //change scale to user input
            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, FindFamilyTypeId(doc), elevMarkerPosition, 96);
            marker.get_Parameter(BuiltInParameter.PHASE_CREATED).Set(p.AsElementId());
            //toHide[0] = marker.Id;

            //move this back into the if statement when code works

            if (marker.IsAvailableIndex(0))
            {
                ViewSection extElev = marker.CreateElevation(doc, plan.Id, 0);
                extElev.get_Parameter(BuiltInParameter.VIEW_PHASE).Set(p.AsElementId());

            }

            return marker;
        }


        public XYZ GetViewSectionNormal(Document doc, ElevationMarker marker)
        {
            ViewSection elevViewSection = (ViewSection)doc.GetElement(marker.GetViewId(0));

            double[][] transform = Matrix.transform2matrix(elevViewSection.CropBox.Transform);
            

            XYZ cbMinT = Matrix.matrix2xyz(Matrix.dot(transform, Matrix.xyz2matrix(elevViewSection.CropBox.Min)));
            XYZ cbMaxT = Matrix.matrix2xyz(Matrix.dot(transform, Matrix.xyz2matrix(elevViewSection.CropBox.Max)));

            XYZ elevViewSectionCoordEndPoint1 = new XYZ(
                cbMinT.X,
                cbMinT.Y,
                cbMaxT.Z);

            XYZ elevViewSectionCoordEndPoint2 = new XYZ(
                cbMinT.X,
                cbMinT.Y,
                cbMinT.Z);

            double[][] inverse = Matrix.invert(transform);
            XYZ cbEP1 = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(elevViewSectionCoordEndPoint1)));
            XYZ cbEP2 = Matrix.matrix2xyz(Matrix.dot(inverse, Matrix.xyz2matrix(elevViewSectionCoordEndPoint2)));
            // these points are already in the model coordiante system so no need to translate

            XYZ elevViewSectionNormal = cbEP2.Subtract(cbEP1);

            return elevViewSectionNormal;
        }

        public double NormalofCurvedWall (Wall w, out bool concave)
        {
            //Only works for curved walls that have not been rotated since they were placed, if the walls were rotated, find you need to consider the wall's transform
            LocationCurve wallCurve = w.Location as LocationCurve;
            Arc wallCurveAsArc = wallCurve.Curve as Arc;

            XYZ EP1 = wallCurve.Curve.GetEndPoint(0);
            XYZ EP2 = wallCurve.Curve.GetEndPoint(1);

            double curveNormal;

            bool xNeg = Math.Round(EP1.X) < Math.Round(EP2.X);
            bool yNeg = Math.Round(EP1.Y) < Math.Round(EP2.Y);

            //round w.orientation to remove floating point rounding errors
            bool xOrientationNeg = Math.Round(w.Orientation.X) < 0;
            bool yOrientationNeg = Math.Round(w.Orientation.Y) < 0;

            //get angle of line between EP1 and EP2 using ArcTan2 because we need to know if it's neg or positive
            double angleLineEP1EP2 = Math.Atan2(EP2.Y - EP1.Y, EP2.X - EP1.X);

            if (xNeg == xOrientationNeg && yNeg == yOrientationNeg)
            {
                concave = false;
                curveNormal = angleLineEP1EP2 + Math.PI / 2 * wallCurveAsArc.YDirection.Y;
            }

            else
            {
                concave = true;
                curveNormal = angleLineEP1EP2 - Math.PI / 2 * wallCurveAsArc.YDirection.Y;
            }

            //returns the normal of a curved wall relative to project coordinate system
            return curveNormal;
        }

        public double GetAngleViewtoWall(XYZ elevViewSectionNormal, Wall w)
        {
            LocationCurve wallCurve = w.Location as LocationCurve;
            Arc wallCurveAsArc = wallCurve.Curve as Arc;
            Double angleViewtoWall;


            if ((wallCurveAsArc) != null)
            {
                double curveNormal = NormalofCurvedWall(w, out bool concave);

                angleViewtoWall = curveNormal - Math.PI;
            }

            else
            {
                XYZ wallNormal = w.Orientation;

                angleViewtoWall = Math.PI - wallNormal.AngleTo(elevViewSectionNormal);

                if (w.CurtainGrid != null)
                {
                    angleViewtoWall -= Math.PI;
                }

                if (wallNormal.Y < 0)
                {
                    angleViewtoWall = - angleViewtoWall;
                }
            }

            if (angleViewtoWall < 0)
            {
                angleViewtoWall += 2 * Math.PI;
            }

            return angleViewtoWall;
        }


        public void RotateMarker(ElevationMarker marker, double angleViewtoWall, XYZ elevMarkerPosition)
        {
            marker.get_BoundingBox(null);

            Line rotationAxis = Line.CreateBound(
                new XYZ(elevMarkerPosition.X, elevMarkerPosition.Y, elevMarkerPosition.Z),
                new XYZ(elevMarkerPosition.X, elevMarkerPosition.Y, elevMarkerPosition.Z + 10)
            );

            marker.Location.Rotate(rotationAxis, angleViewtoWall);
        }

        public void SetCropBox(Document doc, ElevationMarker marker, Wall w)
        {
            ViewSection extElev = (ViewSection)doc.GetElement(marker.GetViewId(0));
            BoundingBoxXYZ eecb = extElev.CropBox;

            XYZ cbboundsmin = extElev.CropBox.Min;
            XYZ cbboundsmax = extElev.CropBox.Max;

            LocationCurve wallCurve = w.Location as LocationCurve;
            XYZ wmin = wallCurve.Curve.GetEndPoint(0);
            XYZ wallEP2 = wallCurve.Curve.GetEndPoint(1);

            Parameter wallHeight = w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
            XYZ wmax = new XYZ
            (
                wallEP2.X,
                wallEP2.Y,
                wallEP2.Z + wallHeight.AsDouble()
            );

            double[][] transform = Matrix.transform2matrix(eecb.Transform);
            double[][] transformInv = Matrix.invert(transform);

            double[] wMinMatrix = Matrix.xyz2matrix(wmin);
            double[] wMinTMatrix = Matrix.dot(transform, wMinMatrix);
            XYZ wMinT = Matrix.matrix2xyz(wMinTMatrix);

            double[] wMaxMatrix = Matrix.xyz2matrix(wmax);
            double[] wMaxTMatrix = Matrix.dot(transform, wMaxMatrix);
            XYZ wMaxT = Matrix.matrix2xyz(wMaxTMatrix);


            double[] originMatrix = Matrix.xyz2matrix(eecb.Transform.Origin);
            double[] originTMatrix = Matrix.dot(transform, originMatrix);
            XYZ originT = Matrix.matrix2xyz(originTMatrix);

            XYZ wMinTOrdered;
            XYZ wMaxTOrdered;
            Utility.ReorderMinMax(wMinT, wMaxT, out wMinTOrdered, out wMaxTOrdered);

            XYZ wbbboundsmin = new XYZ(
                wMinTOrdered.X - originT.X,
                wMinTOrdered.Y - originT.Y,
                cbboundsmin.Z
            );
            XYZ wbbboundsmax = new XYZ(
                wMaxTOrdered.X - originT.X,
                wMaxTOrdered.Y - originT.Y - 0.01,
                cbboundsmax.Z
            );

            eecb.set_Bounds(0, wbbboundsmin);
            eecb.set_Bounds(1, wbbboundsmax);

            extElev.CropBox = eecb;
            extElev.CropBoxVisible = false;
        }

        public ElementId FindFamilyTypeId(Document doc)
        {
            //Add User Selection
            ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Elevation && x.Name.ToLower().Contains("exterior") == true);
            if (viewFamilyType == null)
            {
                throw new Exception("Cannot find View Family Type elevation that contains the word 'exterior'");
            }

            return viewFamilyType.Id;
        }


        public ViewPlan DocumentElevPlanViews(Document doc, Wall w)
        {
            //Add User Selection
            ViewPlan vpElevMarker = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(vp => vp.GenLevel.Id == w.LevelId)
                .FirstOrDefault();

            Debug("selected view plan " + vpElevMarker.Name);
            Debug("selected view plan GenLevel property .id " + vpElevMarker.GenLevel.Id);
            Debug("selected view plan GenLevel property .Name " + vpElevMarker.GenLevel.Name);

            if (vpElevMarker == null)
            {
                throw new Exception("Cannot find View Plans in document");
            }

            return vpElevMarker;
        }
    }
}
