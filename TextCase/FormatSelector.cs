/*
LM2.Revit.FormatSelector saves text formatting to a text file for application with the FormatPainter.
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
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]
    public class FormatSelector : IExternalCommand
    {
        Autodesk.Revit.ApplicationServices.Application application;

        private static Config pluginConfig;

        private void Debug(string msg)
        {
            this.application.WriteJournalComment(msg, true);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;
            string pluginName = "TextFormatSelector";

            this.application = commandData.Application.Application;

            pluginConfig = Utility.ReadConfig();

            TextNote userSelectedTextNote = SelectText(UIdoc);
            if (userSelectedTextNote == null)
            {
                return Result.Cancelled;
            }

            String slackMessage = ">Started";
            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage);

            SaveTextProperties(userSelectedTextNote);

            int ROI = 2;
            string ROIString;
            ROIString = string.Format("{0:0.00} hours", ROI / 60f);


            String slackMessage2 = $">Completed\n>Format Selection\n>ROI for run: {ROI} minutes ({ROIString}) saved\nData\t{ROI}";

            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage2);

            return Result.Succeeded;
        }

        public TextNote SelectText(UIDocument UIdoc)
        {
            ISelectionFilter selecFilter = new TextNoteSelectionFilter();

            Reference selectedElement = UIdoc.Selection.PickObject(ObjectType.Element, selecFilter);

            Element e = UIdoc.Document.GetElement(selectedElement.ElementId);
            TextNote t = e as TextNote;
            if (t != null)
            {
                TextNote selectedTextNote = t;
                Debug("selected elements" + selectedTextNote.ToString());
                return selectedTextNote;
            }

            return null;
        }

        public void SaveTextProperties (TextNote t)
        {
            FormattedText tFormatted = t.GetFormattedText();
            TextRange tr = new TextRange(0, 1);

            int textNoteTypeID = t.TextNoteType.Id.IntegerValue;

            string lowercaseAlph = "abcdefghijklmnopqrstuvwxyz";

            string plaintext = tFormatted.GetPlainText();
            bool isUpper = true;

            foreach (char c in plaintext)
            {
                if(lowercaseAlph.Contains(c))
                {
                    isUpper = false;
                    break;
                }
            }

            bool textNoteAllCapsStatus = isUpper;
            Debug("textNoteAllCapsStatus" + textNoteAllCapsStatus.ToString());

            FormatStatus textNoteBoldStatus = tFormatted.GetBoldStatus(tr);
            Debug("textNoteBoldStatuss" + textNoteBoldStatus.ToString());
            FormatStatus textNoteItalicsStatus = tFormatted.GetItalicStatus(tr);
            Debug("textNoteItalicsStatus" + textNoteItalicsStatus.ToString());
            FormatStatus textNoteUnderlineStatus = tFormatted.GetUnderlineStatus(tr);
            Debug("textNoteUnderlineStatus" + textNoteUnderlineStatus.ToString());

            TextFormattingContainer container = new TextFormattingContainer();
            container.textNoteAllCapsStatus = textNoteAllCapsStatus;
            container.textNoteTypeID = textNoteTypeID;
            container.textNoteBoldStatus = textNoteBoldStatus;
            container.textNoteItalicsStatus = textNoteItalicsStatus;
            container.textNoteUnderlineStatus = textNoteUnderlineStatus;

            byte[] containerBytes;

            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, container);
                containerBytes= ms.ToArray();
            }
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;

            File.WriteAllBytes(assemblyDir + @"\LM2FormatSelectorSave.bin", containerBytes);
        }

    }

}
