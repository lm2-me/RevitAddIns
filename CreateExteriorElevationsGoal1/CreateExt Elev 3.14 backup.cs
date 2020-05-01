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

    public class PlaceExteriorElevations : IExternalCommand
    {
        Application application;

        private void Debug(string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            this.application = commandData.Application.Application;
            //rewrite method to allow for user selection of walls in model
            List<Wall> wallsToUse = ExtWalls(doc);
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Place Elevations");

                foreach (Wall w in wallsToUse)
                {
                    BoundingBoxXYZ wbb = w.get_BoundingBox(null);
                    using (ElevationMarker marker = PlaceMarker(doc, wbb, w))
                    {
                        RotateMarker(doc, w, marker);
                    }

                    //create elevation view facing wall
                    //set elevation view crop box to that of wall
                    //option to hide everything except for the curtain wall 
                    //option to hide elevaion marker in view? maybe put on workset
                }

                tx.Commit();
            }

            return Result.Succeeded;

        }

        //add user input to select walls
        public List<Wall> ExtWalls(Document doc)
        {
            List<Wall> wallsForViews = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w => w.Name.Contains("E_"))
                .ToList();

            return wallsForViews;
        }

        public ElevationMarker PlaceMarker(Document doc, BoundingBoxXYZ wallbb, Wall w)
        {

            XYZ wallbbmin = wallbb.Min;
            XYZ wallbbmax = wallbb.Max;
            ViewPlan plan = DocumentElevPlanViews(doc);
            Parameter p = plan.get_Parameter(BuiltInParameter.VIEW_PHASE);
            //Debug("p equals " + p.AsElementId() + " phase called " + p.AsValueString());
            ////Element planPhase = doc.GetElement(p.AsElementId());
            ////Debug("planPhase equals " + planPhase);

            //List<Room> allRooms = RoomHelpers.GetAllRooms(doc);
            //foreach(Room room in allRooms)
            //{
            //    //Parameter roomPhase = room.get_Parameter(BuiltInParameter.VIEW_PHASE);
            //    //Debug("Room Phase View Phase " + room.Name + " - " + roomPhase + " (" + roomPhase.AsValueString() + ")");

            //    Parameter roomPhase2 = room.get_Parameter(BuiltInParameter.PHASE_CREATED);
            //    Debug("Room Phase Phase Created " + room.Name + " - " + roomPhase2.AsElementId().ToString() + " (" + roomPhase2.AsValueString() + ")");

            //    Parameter roomPhase3 = room.get_Parameter(BuiltInParameter.ROOM_PHASE);
            //    Debug("Room Phase Room Phase " + room.Name + " - " + roomPhase3.AsElementId().ToString() + " (" + roomPhase3.AsValueString() + ")");

            //    //ElementId roomPhase1 = room.CreatedPhaseId;
            //    //Debug("Room Phase 1 " + room.Name + " - " + roomPhase1 + " (" + roomPhase1.ToString() + ")");
            //}

            //Debug("Wallbb min " + wallbbmin);
            //Debug("Wallbb max " + wallbbmax);
            //Debug("Plan phase Id " + p.AsElementId().ToString());
            ////Debug("Plan phase Name " + planPhase.Name);

            XYZ wallCenter = new XYZ(
                (wallbbmax.X + wallbbmin.X) / 2,
                (wallbbmax.Y + wallbbmin.Y) / 2,
                wallbbmin.Z
            );

            List<Room> phasedRooms = RoomHelpers.GetPhasedRooms(doc, p.AsElementId());
            //Debug("Phase of wall " + w.CreatedPhaseId.ToString());
            //Debug("Phase of wall name " + w.get_Parameter(BuiltInParameter.PHASE_CREATED).AsValueString());

            XYZ wallCenterYTranslatedPositive = new XYZ(
                wallCenter.X,
                wallCenter.Y + 10,
                wallCenter.Z
            );

            XYZ wallCenterYTranslatedNegative = new XYZ(
                wallCenter.X,
                wallCenter.Y - 10,
                wallCenter.Z
            );

            XYZ wallCenterXTranslatedPositive = new XYZ(
                wallCenter.X + 10,
                wallCenter.Y,
                wallCenter.Z
            );

            XYZ wallCenterXTranslatedNegative = new XYZ(
                wallCenter.X - 10,
                wallCenter.Y,
                wallCenter.Z
            );

            //Debug("wall center " + wallCenter);

            //Debug("wallCenterYTranslatedPositive " + wallCenterYTranslatedPositive);
            //Debug("wallCenterYTranslatedNegative " + wallCenterYTranslatedNegative);
            //Debug("wallCenterXTranslatedPositive " + wallCenterXTranslatedPositive);
            //Debug("wallCenterXTranslatedNegative " + wallCenterXTranslatedNegative);

            XYZ markerLocation = wallCenter;

            Debug("markerLocation before the loops " + markerLocation);
            Debug("length of list phasedrooms " + phasedRooms.Count);

            if ((wallbbmax.X - wallbbmin.X) > (wallbbmax.Y - wallbbmin.Y))
            {
                foreach (Room r in phasedRooms)
                {
                    if (r.IsPointInRoom(wallCenterYTranslatedPositive))
                    {
                        Debug("room name " + r.Name);
                        Debug("is Y positive in room " + r.IsPointInRoom(wallCenterYTranslatedPositive));

                        markerLocation = wallCenterYTranslatedNegative;
                        break;
                    }
                    else if (r.IsPointInRoom(wallCenterYTranslatedNegative))
                    {
                        Debug("room name " + r.Name);
                        Debug("is Y negative in room " + r.IsPointInRoom(wallCenterYTranslatedNegative));
                        markerLocation = wallCenterYTranslatedPositive;
                        break;
                    }
                }
                Debug("is Y check done");
            }
            else
            {
                foreach (Room r in phasedRooms)
                {
                    if (r.IsPointInRoom(wallCenterXTranslatedPositive))
                    {
                        Debug("room name " + r.Name);
                        Debug("is X positive in room " + r.IsPointInRoom(wallCenterXTranslatedPositive));
                        markerLocation = wallCenterXTranslatedNegative;
                        break;
                    }
                    else if (r.IsPointInRoom(wallCenterXTranslatedNegative))
                    {
                        Debug("room name " + r.Name);
                        Debug("is X negative in room " + r.IsPointInRoom(wallCenterXTranslatedNegative));
                        markerLocation = wallCenterXTranslatedPositive;
                        break;
                    }
                }
                Debug("is X check done");
            }

            Debug("selected marker location after loops " + markerLocation);

            //change scale to user input
            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, FindFamilyTypeId(doc), markerLocation, 96);
            marker.get_Parameter(BuiltInParameter.PHASE_CREATED).Set(p.AsElementId());


            if (marker.IsAvailableIndex(0))
            {
                ViewSection extElev = marker.CreateElevation(doc, plan.Id, 0);

                extElev.get_Parameter(BuiltInParameter.VIEW_PHASE).Set(p.AsElementId());
            }

            return marker;
        }

        public void RotateMarker(Document doc, Wall w, ElevationMarker marker)
        {
            XYZ wallNormal = w.Orientation;

            //rotate maker to be parallel to wall
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


        public ViewPlan DocumentElevPlanViews(Document doc)
        {
            //Add User Selection
            ViewPlan vpElevMarker = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(vp => vp.Name.ToLower().Contains("ground floor plan"))
                .FirstOrDefault();

            Debug("selected view plan " + vpElevMarker.Name);

            if (vpElevMarker == null)
            {
                throw new Exception("Cannot find View Plans in document");
            }

            return vpElevMarker;

        }
    }

}
