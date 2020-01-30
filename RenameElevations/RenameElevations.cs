/*
LM2.Revit.RenameElevations renames interior elevation views that are already located in your project.
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
using System.Windows.Forms;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]

    public class RenameElevations : IExternalCommand
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

            try
            {
                List<KeyValuePair<ViewSection, String>> newNames = new List<KeyValuePair<ViewSection, string>>();
                List<ViewSection> intElevlist = InteriorElevations(doc);

                foreach (ViewSection ie in intElevlist)
                {
                    List<Room> phasedRooms = RoomHelpers.GetPhasedRooms(doc, ie.CreatedPhaseId);

                    Room r = GetElevRoom(ie, phasedRooms, doc);

                    String ieNewName = RenameElevation(doc, ie, r);
                    newNames.Add(new KeyValuePair<ViewSection, String>(ie, ieNewName));
                }

                RenameElevationsDialog ieDialog = new RenameElevationsDialog(this.application, newNames);
                DialogResult dialogResult = ieDialog.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {

                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Rename Elevations");
                        foreach (ViewSection ie in intElevlist)
                        {
                            String ieNewName = newNames.Find(kv => kv.Key.Id == ie.Id).Value;
                            AssignName(ie, ieNewName);
                        }

                        tx.Commit();
                    }

                }

                else
                {
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public void AssignName(ViewSection intElev, String intElevNewName)
        {
            //assign name when "okay" is pressed
            try
            {
                intElev.Name = intElevNewName;
            }
            catch
            {
                return;
            }
        }

        public Room GetElevRoom(ViewSection ie, List<Room> phasedRooms, Document doc)
        {


            XYZ cbmin = ie.CropBox.get_Bounds(0);
            XYZ cbmax = ie.CropBox.get_Bounds(1);

            XYZ viewPoint = new XYZ(
                (cbmax.X + cbmin.X) / 2,
                1,
                cbmax.Z
            );

            double[][] transform = Matrix.transform2matrix(ie.CropBox.Transform);
            Matrix.print("VS Transform", transform, Debug);
            double[] viewPointVector = Matrix.xyz2matrix(viewPoint);
            double[][] transformInv = Matrix.invert(transform);
            double[] viewPointVectorT = Matrix.dot(transformInv, viewPointVector);
            XYZ viewPointT = Matrix.matrix2xyz(viewPointVectorT);

            XYZ offsetOrigin = new XYZ
            (
                viewPointT.X + ie.Origin.X,
                viewPointT.Y + ie.Origin.Y,
                1
            );

            foreach (Room r in phasedRooms)
            {
             
                if (r.IsPointInRoom(offsetOrigin))
                {
                    return r;
                }

            }

            throw new Exception("No room located at point" + offsetOrigin);
        }

        public List<ViewSection> InteriorElevations(Document doc)
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

        public String RenameElevation(Document doc, ViewSection intElev, Room room)
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

            string newName = roomNumber + " - " + roomName + " - " + suffix;

            return newName;

        }

    }
}


