/*
LM2.Revit.FormatPainter paints text formatting from a text file for application with the FormatSelector.
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
    public class FormatPaint : IExternalCommand
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
            string pluginName = "TextFormatPainter";

            this.application = commandData.Application.Application;

            pluginConfig = Utility.ReadConfig();

            IList<TextNote> userSelectedTextNote = SelectText(UIdoc);
            if (userSelectedTextNote == null)
            {
                return Result.Cancelled;
            }

            String slackMessage = ">Started";
            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage);

            int textboxes = userSelectedTextNote.Count;

            TextFormattingContainer format = GetTextProperties();

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Text Case Change");

                foreach (TextNote txt in userSelectedTextNote)
                {
                    SetTextProperties(doc, txt, format);
                }

                tx.Commit();
            }

            int ROI = 2 * textboxes;
            string ROIString;
            ROIString = string.Format("{0:0.00} hours", ROI / 60f);


            String slackMessage2 = $">Completed\n>Number of TextNotes: {textboxes}\n>ROI for run: {ROI} minutes ({ROIString}) saved\nData\t{ROI}";

            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage2);


            return Result.Succeeded;
        }

        public IList<TextNote> SelectText(UIDocument UIdoc)
        {
            IList<Reference> selectedElements = new List<Reference>();
            IList<TextNote> selectedTextNotes = new List<TextNote>();

            ISelectionFilter selecFilter = new TextNoteSelectionFilter();

            selectedElements = UIdoc.Selection.PickObjects(ObjectType.Element, selecFilter);

            Debug("selected elements" + selectedElements.Count);

            foreach (Reference r in selectedElements)
            {
                Element e = UIdoc.Document.GetElement(r.ElementId);
                TextNote t = e as TextNote;
                if (t != null)
                {
                    selectedTextNotes.Add(t);
                    Debug("selected elements" + selectedTextNotes.Count);
                }
            }

            return selectedTextNotes;
        }

        public TextFormattingContainer GetTextProperties()
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;

            byte[] containerBytes = File.ReadAllBytes(assemblyDir + @"\LM2FormatSelectorSave.bin");

            TextFormattingContainer container = null;
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(containerBytes, 0, containerBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                container = binForm.Deserialize(memStream) as TextFormattingContainer;
            }

            return container;

        }

        public void SetTextProperties(Document doc, TextNote txt, TextFormattingContainer format)
        {
            FormattedText tFormatted = txt.GetFormattedText();

            TextNoteType type = doc.GetElement(new ElementId(format.textNoteTypeID)) as TextNoteType;

            txt.TextNoteType = type;

            Debug("textNoteAllCaps Paint " + format.textNoteAllCapsStatus.ToString());

            if (format.textNoteAllCapsStatus)
            {
                string currentPlainText = tFormatted.GetPlainText();
                Debug("text formatted Plain Text Paint " + currentPlainText);
                string upperCase = currentPlainText.ToUpper();
                tFormatted.SetPlainText(upperCase);
                Debug("text formatted Plain Text Updates Paint " + tFormatted.GetPlainText().ToString());

            }

            tFormatted.SetBoldStatus(format.textNoteBoldStatus != FormatStatus.None);
            Debug("textNoteBoldStatus Paint " + format.textNoteBoldStatus.ToString());
            Debug("get textNoteBoldStatus Paint " + tFormatted.GetBoldStatus().ToString());

            tFormatted.SetItalicStatus(format.textNoteItalicsStatus != FormatStatus.None);
            Debug("textNoteItalicsStatus Paint " + format.textNoteItalicsStatus.ToString());
            Debug("get textNoteItalicsStatus Paint " + tFormatted.GetItalicStatus().ToString());

            tFormatted.SetUnderlineStatus(format.textNoteUnderlineStatus != FormatStatus.None);
            Debug("textNoteUnderlineStatus Paint " + format.textNoteUnderlineStatus.ToString());
            Debug("get textNoteUnderlineStatus Paint " + tFormatted.GetUnderlineStatus().ToString());

            txt.SetFormattedText(tFormatted);

        }

    }
}
