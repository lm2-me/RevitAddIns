using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LM2.Revit
{
    [Transaction(TransactionMode.Manual)]
    public class TextFormattingRibbon : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            String tabName = "LM2";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            { }

            List<RibbonPanel> allRibbonPanels = application.GetRibbonPanels(tabName);
            foreach (RibbonPanel rp in allRibbonPanels)
            {

                if (rp.Name == "Text Formatting")
                {
                    AddPushButton(rp);
                    AddPushButton2(rp);
                    AddPushButton3(rp);
                    return Result.Succeeded;
                }

            }
            RibbonPanel textPanel = application.CreateRibbonPanel(tabName, "Text Formatting");
            AddPushButton(textPanel);
            AddPushButton2(textPanel);
            AddPushButton3(textPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel textPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData textButtonData = new PushButtonData("TextCase", "TextCase", assembly.Location, "LM2.Revit.TextCase");
            PushButton renameIntElevButton = textPanel.AddItem(textButtonData) as PushButton;

            renameIntElevButton.ToolTip = "Change text to uppercase";
            var imgUri = assemblyDir + @"\05_TextCase_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            renameIntElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

        private void AddPushButton2(RibbonPanel textPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData textButtonData = new PushButtonData("FormatSelector", "FormatSelector", assembly.Location, "LM2.Revit.FormatSelector");
            PushButton renameIntElevButton = textPanel.AddItem(textButtonData) as PushButton;

            renameIntElevButton.ToolTip = "Select Text Formatting";
            var imgUri = assemblyDir + @"\06_FormatSelect_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            renameIntElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

        private void AddPushButton3(RibbonPanel textPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData textButtonData = new PushButtonData("FormatPainter", "FormatPainter", assembly.Location, "LM2.Revit.FormatPaint");
            PushButton renameIntElevButton = textPanel.AddItem(textButtonData) as PushButton;

            renameIntElevButton.ToolTip = "Paint Text Formatting";
            var imgUri = assemblyDir + @"\07_FormatPaint_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            renameIntElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
 