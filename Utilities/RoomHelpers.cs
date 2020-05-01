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
    public static class RoomHelpers
    {
        public static List<Room> GetAllRooms(Document doc)
        {

            List<Room> allRooms = new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement))
                .Select(e => e as Room)
                .Where(e => e != null)
                .ToList();

            return allRooms;
        }
        public static List<Room> GetPhasedRooms(Document doc, ElementId Phase)
        {
            List<Room> allRooms = GetAllRooms(doc);
            List<Room> phasedRooms = allRooms
                .Where(e => e.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId() == Phase)
                .ToList();

            return phasedRooms;
        }

    }
}
