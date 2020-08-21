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
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]

    public class RandomCurtainWallPanelization : IExternalCommand
    {
        Autodesk.Revit.ApplicationServices.Application application;
        Random rand;

        private void Debug(string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            this.application = commandData.Application.Application;

            //generate random integer for use in program
            int seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            rand = new Random(seed);

            Wall userSelectedWall = SelectWall(UIdoc, false);
            if (userSelectedWall == null)
            {
                return Result.Cancelled;
            }

            Wall curtainWall = userSelectedWall;
            List<ElementId> panels = GetPanelTypes(doc, curtainWall);

            List<KeyValuePair<string, Int32>> materialOptions = panels.Select(p => new KeyValuePair<string, int>(doc.GetElement(p).Name, p.IntegerValue)).ToList();
            RandomizeUserInterface uIInformation = new RandomizeUserInterface(materialOptions);
            uIInformation.ShowDialog();
            if (uIInformation.DialogResult != true)
            {   
                return Result.Cancelled;
            }

            //user input for which direction(s) to randomize
            bool userHorizontalRandom = uIInformation.HorizontalRandom ?? false;
            bool userVerticalRandom = uIInformation.VerticalRandom ?? false;

            //user input for probability of removing horiz and vert segments
            int probabilityHoriz = uIInformation.WPFHorizProbab;
            int probabilityVert = uIInformation.WPFVertProbab;

            //user input
            bool userUseOwn = uIInformation.UseOwn ?? false;
            int maxHorizDelete = uIInformation.WPFMaxHorizDelete;
            int maxVertDelete = uIInformation.WPFMaxVertDelete;

            //user input min and max width and height
            bool userCreateForMe = uIInformation.CreateForMe ?? false;
            double minWidth = uIInformation.WPFMinWidth;
            double maxWidth = uIInformation.WPFMaxWidth;
            double minHeight = uIInformation.WPFMinHeight;
            double maxHeight = uIInformation.WPFMaxHeight;

            double horizCounterMax = 0;
            double vertCounterMax = 0;

            int mtl1Max = (int)uIInformation.sl1.Value;
            ElementId mtl1 = new ElementId((int)uIInformation.cwPanelType1.SelectedValue);
            int mtl2Max = (int)uIInformation.sl2.Value + mtl1Max;
            ElementId mtl2 = new ElementId((int)uIInformation.cwPanelType2.SelectedValue);
            int mtl3Max = (int)uIInformation.sl3.Value + mtl2Max;
            ElementId mtl3 = new ElementId((int)uIInformation.cwPanelType3.SelectedValue);
            int mtl4Max = (int)uIInformation.sl4.Value + mtl3Max;
            ElementId mtl4 = new ElementId((int)uIInformation.cwPanelType4.SelectedValue);
            int mtl5Max = (int)uIInformation.sl5.Value + mtl4Max;
            ElementId mtl5 = new ElementId((int)uIInformation.cwPanelType5.SelectedValue);

            //delete mullions
            bool optDeleteMullions = uIInformation.DeleteMullions ?? false;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Randomize Curtain Wall Grid");

                if (userCreateForMe)
                {
                    DeteleAllGrids(doc, curtainWall);
                    Tuple<XYZ, XYZ> minMaxofCW = GetCWMinMax(curtainWall);

                    MakeHorizGrids(doc, curtainWall, minHeight, minMaxofCW);
                    MakeVertGrids(doc, curtainWall, minWidth, minMaxofCW);

                    horizCounterMax = (maxHeight / minHeight) - 1;
                    vertCounterMax = (maxWidth / minWidth) - 1;
                }

                else
                {
                    horizCounterMax = maxHorizDelete;
                    vertCounterMax = maxVertDelete;
                }

                if (userHorizontalRandom)
                {
                    RandDeteleHorizGrids(doc, curtainWall, probabilityHoriz, horizCounterMax);
                }

                if (userVerticalRandom)
                {
                    RandDeteleVertGrids(doc, curtainWall, probabilityVert, vertCounterMax);
                }

                tx.Commit();
            }

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Randomize Panel Material");

                RandomizePanelMaterial(doc, UIdoc, curtainWall,
                    mtl1Max, mtl1,
                    mtl2Max, mtl2,
                    mtl3Max, mtl3,
                    mtl4Max, mtl4,
                    mtl5Max, mtl5);

                tx.Commit();
            }

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Delete Mullions");

                if (optDeleteMullions)
                {
                    DeleteMullions(doc, UIdoc, curtainWall);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }

        public Wall SelectWall(UIDocument UIdoc, bool isRetry)
        {
            RandomizerSelectWall uISelectWall = new RandomizerSelectWall(isRetry);
            uISelectWall.ShowDialog();

            if (uISelectWall.DialogResult != true)
            {
                return null;
            }

            Selection userObjectSelection = UIdoc.Selection;
            Reference userSelection = userObjectSelection.PickObject(ObjectType.Element);
            Element el = UIdoc.Document.GetElement(userSelection.ElementId);
            Wall userSelectedWall = el as Wall;

            if (userSelectedWall != null && userSelectedWall.CurtainGrid != null)
            {
                return userSelectedWall;
            }

            else
            {
                return SelectWall(UIdoc, true);
            }
        }

        public List<ElementId> GetPanelTypes(Document doc, Wall curtainWall)
        {
            ElementId panelId = curtainWall.CurtainGrid.GetPanelIds().First();
            Element panel = doc.GetElement(panelId);

            List<ElementId> validPanelTypes = panel.GetValidTypes().ToList();
            Debug("Valid Panel Types List Count " + validPanelTypes.Count);
            List<ElementId> finalPanelTypes = new List<ElementId>();

            foreach (ElementId id in validPanelTypes)
            {
                Element e = doc.GetElement(id);
                if (e.Category.Name == "Curtain Panels")
                {
                    finalPanelTypes.Add(e.Id);
                }
            }

            Debug("Panel Types List Count total" + finalPanelTypes.Count);
            
            return finalPanelTypes;
        }

        public Wall DeteleAllGrids(Document doc, Wall curtainWall)
        {
            CurtainGrid grid = curtainWall.CurtainGrid;

            ICollection<ElementId> horizGrids = grid.GetUGridLineIds();
            ICollection<ElementId> vertGrids = grid.GetVGridLineIds();

            foreach (ElementId eIdH in horizGrids)
            {
                Debug("Horiz Grid ID " + eIdH);
                CurtainGridLine cglH = (CurtainGridLine)doc.GetElement(eIdH);
                Parameter typeAssociationH = cglH.GetParameters("Type Association").FirstOrDefault();
                Debug($"Horiz Type Association {typeAssociationH?.AsValueString()}");
                Debug("Horiz Lock " + cglH.Lock);

                if (typeAssociationH?.AsInteger() == 1)
                {
                    typeAssociationH.Set(0);
                    Debug("Horiz Type Association Reset " + (typeAssociationH?.AsValueString() ?? "null"));
                    Debug("Horiz Type Association Element Id " + typeAssociationH.AsElementId());
                }

                if (cglH.Lock)
                {
                    cglH.Lock = false;
                    Debug("Horiz Lock Reset " + cglH.Lock);
                }

                doc.Delete(eIdH);
            }

            //deletes all vert grids
            foreach (ElementId eIdV in vertGrids)
            {
                Debug("Vert Grid ID " + eIdV);
                CurtainGridLine cglV = (CurtainGridLine)doc.GetElement(eIdV);
                Parameter typeAssociationV = cglV.GetParameters("Type Association").FirstOrDefault();
                Debug("Vert Type Association " + (typeAssociationV?.AsValueString() ?? "null"));
                Debug("Vert Lock " + cglV.Lock);

                if (typeAssociationV?.AsInteger() == 1)
                {
                    typeAssociationV.Set(0);
                    Debug("Vert Type Association Reset " + (typeAssociationV?.AsValueString() ?? "null"));
                    Debug("Vert Type Association Element Id " + typeAssociationV.AsElementId());
                }

                if (cglV.Lock)
                {
                    cglV.Lock = false;
                    Debug("Vert Lock Reset " + cglV.Lock);
                }

                doc.Delete(eIdV);
            }

            return curtainWall;
        }

        public Tuple<XYZ, XYZ> GetCWMinMax(Wall curtainWall)
        {
            Tuple<XYZ, XYZ> CWMinMax;
            List<XYZ> cWOriginList = new List<XYZ>();

            CurveArray cWPlane = curtainWall.CurtainGrid.GetCurtainCells().First().CurveLoops.get_Item(0);

            foreach (Curve cWPC in cWPlane)
            {
                cWOriginList.Add((cWPC as Line).Origin);
            }

            if (cWOriginList.Count != 0)
            {
                CWMinMax = Utility.GetMinMaxFromList(cWOriginList);
            }

            else
            {
                throw new Exception("cannot find min and max values of curtain wall endpoints");
            }

            return CWMinMax;
        }


        public void MakeHorizGrids(Document doc, Wall curtainWall, double minHeight, Tuple<XYZ, XYZ> minMax)
        {
            XYZ cwMin = minMax.Item1;
            XYZ cwMax = minMax.Item2;

            XYZ position = cwMin;


            while (position.Z < cwMax.Z - minHeight)
            {
                Debug("position of Horiz " + position);
                position = new XYZ
                (
                    position.X,
                    position.Y,
                    position.Z + minHeight
                );
                //horizontal grid lines are U grid lines
                try
                {
                    curtainWall.CurtainGrid.AddGridLine(true, position, false);
                }

                catch (Exception e)
                {
                    Debug(e.ToString());
                }

            }
        }

        public void MakeVertGrids(Document doc, Wall curtainWall, double minWidth, Tuple<XYZ, XYZ> minMax)
        {
            XYZ cwMin = minMax.Item1;
            XYZ cwMax = minMax.Item2;

            XYZ position = cwMin;
            double positionDistance = 0;

            XYZ btmCorner = new XYZ(
                cwMax.X,
                cwMax.Y,
                cwMin.Z);

            double distance = Math.Sqrt(
                   Math.Pow(btmCorner.X - cwMin.X, 2) +
                   Math.Pow(btmCorner.Y - cwMin.Y, 2)
                   );

            XYZ vector = new XYZ(
                (btmCorner.X - cwMin.X) / distance,
                (btmCorner.Y - cwMin.Y) / distance,
                0
                );

            while (positionDistance < (distance - minWidth))
            {
                Debug("position of Vert " + position);
                position = new XYZ
               (
                   position.X + minWidth * vector.X,
                   position.Y + minWidth * vector.Y,
                   position.Z
               );
                try
                {
                    //horizontal grid lines are U grid lines
                    curtainWall.CurtainGrid.AddGridLine(false, position, false);
                }

                catch (Exception e)
                {
                    Debug(e.ToString());
                }

                positionDistance = Math.Sqrt(
                    Math.Pow(position.X - cwMin.X, 2) +
                    Math.Pow(position.Y - cwMin.Y, 2)
                );
            }
        }

        public void RandDeteleHorizGrids(Document doc, Wall curtainWall, int porbabilityHoriz, double horizCounterMax)
        {
            CurtainGrid grid = curtainWall.CurtainGrid;
            ICollection<ElementId> horizGrids = grid.GetUGridLineIds();
            List<int> removalCounters = null;

            foreach (ElementId eIdH in horizGrids)
            {
                CurtainGridLine cglH = (CurtainGridLine)doc.GetElement(eIdH);
                Debug("CW Grid ID" + cglH.Id);
                List<Curve> curvesH = cglH.AllSegmentCurves.Cast<Curve>().ToList();

                if (removalCounters == null)
                {
                    removalCounters = curvesH.Select(g => 0).ToList();
                }

                for (int i = 0; i < curvesH.Count; i++)
                {
                    Curve cH = curvesH[i];
                    int randNumber = rand.Next(0, 99);

                    if (randNumber < porbabilityHoriz && removalCounters[i] < horizCounterMax)
                    {
                        cglH.RemoveSegment(cH);
                        removalCounters[i]++;
                        Debug("Deleted Horiz Seg " + i);
                        Debug("Horiz randNumber " + randNumber);
                    }
                    else
                    {
                        removalCounters[i] = 0;
                        Debug("Did not delete Horiz Seg " + i);
                        Debug("Horiz randNumber " + randNumber);
                    }

                }
                Debug("CW Grid ID after" + cglH.Id);
            }
        }

        public void RandDeteleVertGrids(Document doc, Wall curtainWall, int probabilityVert, double vertCounterMax)
        {
            CurtainGrid grid = curtainWall.CurtainGrid;
            ICollection<ElementId> vertGrids = grid.GetVGridLineIds();
            List<int> removalCounters = null;

            foreach (ElementId eIdV in vertGrids)
            {
                CurtainGridLine cglV = (CurtainGridLine)doc.GetElement(eIdV);
                Debug("CW Grid ID" + cglV.Id);
                List<Curve> curvesV = cglV.AllSegmentCurves.Cast<Curve>().ToList();

                if (removalCounters == null)
                {
                    removalCounters = curvesV.Select(g => 0).ToList();
                }

                for (int i = 0; i < curvesV.Count; i++)
                {
                    Curve cV = curvesV[i];
                    int randNumber = rand.Next(0, 99);

                    if (randNumber < probabilityVert && removalCounters[i] < vertCounterMax)
                    {
                        Debug("removal counters value" + removalCounters[i]);
                        cglV.RemoveSegment(cV);
                        removalCounters[i]++;
                        Debug("Deleted Vert Seg " + i);
                        Debug("Vert randNumber " + randNumber);
                        Debug("curve list length" + curvesV.Count);

                    }

                    else
                    {
                        removalCounters[i] = 0;
                        Debug("Did not delete Vert Seg " + i);
                        Debug("Vert randNumber " + randNumber);
                    }
                }
                Debug("CW Grid ID after" + cglV.Id);
            }
        }

        public void RandomizePanelMaterial(Document doc, UIDocument UIdoc, Wall curtainWall, 
            int mtl1Max, ElementId mtl1, 
            int mtl2Max, ElementId mtl2, 
            int mtl3Max, ElementId mtl3, 
            int mtl4Max, ElementId mtl4, 
            int mtl5Max, ElementId mtl5)
        {

            List<ElementId> curtainPanelsList = curtainWall.CurtainGrid.GetPanelIds().ToList();
            Debug("curtainPanelsList count )" + curtainPanelsList.Count);



            foreach (ElementId id in curtainPanelsList)
            {
                Element el = UIdoc.Document.GetElement(id);
                int randNumber = rand.Next(0, 99);
                Debug("CW Panel ID)" + el.Id);
                Debug("CW Panel Type)" + el.GetType());
                Debug("CW Panel Type Id)" + el.GetTypeId());
                Debug("CW Panel Category)" + el.Category);

                if (el.Pinned)
                {
                    el.Pinned = false;
                    Debug("CW Panel Pinned " + el.Pinned);
                }

                if (randNumber < mtl1Max)
                {
                    el.ChangeTypeId(mtl1);
                    Debug("CW Type Id After" + el.GetTypeId());
                }

                else if (randNumber > mtl1Max && randNumber <= mtl2Max && mtl2.IntegerValue != -1)
                {
                    el.ChangeTypeId(mtl2);
                    Debug("CW Type Id After" + el.GetTypeId());
                }

                else if (randNumber > mtl2Max && randNumber <= mtl3Max && mtl3.IntegerValue != -1)
                {
                    el.ChangeTypeId(mtl3); 
                    Debug("CW Type Id After" + el.GetTypeId());
                }

                else if (randNumber > mtl3Max && randNumber <= mtl4Max && mtl4.IntegerValue != -1)
                {
                    el.ChangeTypeId(mtl4);
                    Debug("CW Type Id After" + el.GetTypeId());
                }

                else if (randNumber > mtl4Max && randNumber <= mtl5Max && mtl5.IntegerValue != -1)
                {
                    el.ChangeTypeId(mtl5);
                    Debug("CW Type Id After" + el.GetTypeId());
                }
            }
        }

        public void DeleteMullions(Document doc, UIDocument UIdoc, Wall curtainWall)
        {
            List<ElementId> mullionList = curtainWall.CurtainGrid.GetMullionIds().ToList();

            foreach (ElementId id in mullionList)
            {
                Element el = UIdoc.Document.GetElement(id);
                if (el.Pinned)
                {
                    el.Pinned = false;
                    Debug("CW Panel Pinner " + el.Pinned);
                }

                doc.Delete(id);
            }
        }
    }
}
