/*
LM2.Revit.TextCase provides options for changing text from lowercase to uppercase in Revit.
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
using System.Collections.Generic;
using System.Linq;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]
    public class TextCase : IExternalCommand
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
            string pluginName = "TextCaseEditor";

            this.application = commandData.Application.Application;

            pluginConfig = Utility.ReadConfig();

            String slackMessage = ">Started";
            Utility.SendTelemetryData(doc, pluginConfig.telemetryURL, pluginName, slackMessage);

            IList<TextNote> userSelectedTextNote = SelectText(UIdoc);
            int textboxes = userSelectedTextNote.Count;
            if (userSelectedTextNote == null)
            {
                return Result.Cancelled;
            }

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Text Case Change");

                foreach (TextNote uSTN in userSelectedTextNote)
                {
                    TextToUPPERCASE(UIdoc, uSTN);
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

        public void TextToUPPERCASE(UIDocument UIdoc, TextNote text)
        {
            FormattedText currentFormattedText = text.GetFormattedText();
            string currentPlainText = text.GetFormattedText().GetPlainText();

            Debug("currentPlainText" + currentPlainText);

            string upperCase = currentPlainText.ToUpper();
            currentFormattedText.SetPlainText(upperCase);

            text.SetFormattedText(currentFormattedText);

            Debug("upperCase" + upperCase);
            Debug("text" + text.GetFormattedText().GetPlainText());
        }

    }

}
