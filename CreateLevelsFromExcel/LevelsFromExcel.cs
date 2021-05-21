/*
LM2.Revit.LevelsFromExcel reads an Excel file and creates levels in your Revit file based on the height and level name information.
Copyright(C) 2021  Lisa-Marie Mueller

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
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]

    public class LevelsFromExcel : IExternalCommand
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

            String pluginName = "CreateLevelsFromExcel";

            string fileLocation = @"";
            string sheetName = "Sheet1";

            List<string> levelNames = new List<string>();
            List<string> levelHeights = new List<string>();

            try
            {
                ReadExcel(fileLocation, sheetName, levelNames, levelHeights);

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Create Levels");
                    CreateLevels(doc, levelNames, levelHeights);
                    tx.Commit();
                }
            }

            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        //gets data from excel and updates the lists that are storing the level names and the level heights
        private void ReadExcel(string fileLocation, string sheetName, List<string> levelNames, List<string>levelHeights)
        {
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fileLocation);
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[sheetName];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            int rowCount = xlRange.Rows.Count;

            //add level names to list
            //start a i=2 to skip the first row which will be the header, excel ranges are not zero based
            for (int i = 2; i <= rowCount; i++)
            {
                if (xlRange.Cells[i, "A"] != null && !string.IsNullOrEmpty(xlRange.Cells[i, "A"].Text as String))
                {
                    this.Debug($"Read level name cell A{i}");
                    levelNames.Add(xlRange.Cells[i, "A"].Text as String);
                }
            }

            //add level heights to list
            for (int i = 2; i <= rowCount; i++)
            {
                if (xlRange.Cells[i, "B"] != null && !string.IsNullOrEmpty(xlRange.Cells[i, "B"].Text as String))
                {
                    this.Debug($"Read level height cell B{i}");
                    levelHeights.Add(xlRange.Cells[i, "B"].Text as String);
                }
            }

            //cleanup to deallocate memory
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //release com objects to fully kill excel process from running in the background
            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            //close and release
            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            //quit and release
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
        }

        public void CreateLevels (Document doc, List<string> levelNames, List<string>levelHeights)
        {
            for (int i = 0; i < levelHeights.Count; i++)
            {
                this.Debug($"Current line {i}");
                if (double.TryParse(levelHeights[i], out double h))
                {
                    this.Debug($"Parsed value {h}");
                    Level level = Level.Create(doc, h);
                    level.Name = levelNames[i];
                }
            }
        }
    }
}
