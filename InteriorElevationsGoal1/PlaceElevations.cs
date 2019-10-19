/*
LM2.Revit.InteriorElevationsGoal1 places ElevationMarkers and associated ViewSections for each placed room in your project.
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
    public class InteriorElevationsGoal1 : IExternalCommand
    {
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

                foreach (Room r in allRooms)
                {
                    Debug($"Operating on room {r.Name}");
                    XYZ roomCenter = Utility.GetCenter(r);
                    if (roomCenter == null)
                    {
                        continue;
                    }
                    Debug($"Found center at ({roomCenter.X}, {roomCenter.Y}, {roomCenter.Z})");
                    ViewPlan intElevPlanOfRoom = GetViewPlanOfRoom(doc, intElevPlans, r);
                    if (intElevPlanOfRoom == null)
                    {
                        continue;
                    }
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Place Elevations");

                        PlaceElevations(doc, roomCenter, intElevPlanOfRoom);

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

        public void PlaceElevations(Document doc, XYZ center, ViewPlan intElevPlan)
        {
            //current scale set to 1/8"
            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, FindFamilyTypeId(doc), center, 96);
            Parameter p = intElevPlan.get_Parameter(BuiltInParameter.VIEW_PHASE);
            marker.get_Parameter(BuiltInParameter.PHASE_CREATED).Set(p.AsElementId());

            for (int markerindex = 0; markerindex < marker.MaximumViewCount; markerindex++)
            {
                Debug($"Writing marker at {markerindex}");
                if (marker.IsAvailableIndex(markerindex))
                {
                    Debug($"Writing market to plan {intElevPlan.Name}");
                    ViewSection intElev = marker.CreateElevation(doc, intElevPlan.Id, markerindex);
                    intElev.get_Parameter(BuiltInParameter.VIEW_PHASE).Set(p.AsElementId());
                }
            }
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
            Debug($"level ids of view and room are not equal");
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
