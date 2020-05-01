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
    public class ExteriorElevations : IExternalApplication
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
            RibbonPanel elevPanel = application.CreateRibbonPanel(tabName, "Elevations");
            AddPushButton(elevPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel elevPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData elevButtonData = new PushButtonData("Place Exterior", "Place Exterior", assembly.Location, "LM2.Revit.PlaceExteriorElevations");
            PushButton placeExtElevButton = elevPanel.AddItem(elevButtonData) as PushButton;

            placeExtElevButton.ToolTip = "Automatically places exterior elevations based on selected walls";
            var imgUri = assemblyDir + @"\03-PlaceExtElev_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            placeExtElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
