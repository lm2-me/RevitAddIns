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
    public class RenameElevationsRibbon : IExternalApplication
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

                if (rp.Name == "Elevations")
                {
                    AddPushButton(rp);
                    return Result.Succeeded;
                }

            }
            RibbonPanel intElevPanel = application.CreateRibbonPanel(tabName, "Elevations");
            AddPushButton(intElevPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel intElevPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData intElevButtonData = new PushButtonData("Rename Elevations", "Rename Elevations", assembly.Location, "LM2.Revit.RenameElevations");
            PushButton renameIntElevButton = intElevPanel.AddItem(intElevButtonData) as PushButton;

            renameIntElevButton.ToolTip = "Renames interior elevations based on room name and number and elevation direction";
            var imgUri = assemblyDir + @"\02-RenameIntElev_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            renameIntElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
 