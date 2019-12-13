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
    public class InteriorElevationsGoal3Ribbon : IExternalApplication
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

                if (rp.Name == "Interior Elevations")
                {
                    AddPushButton(rp);
                    return Result.Succeeded;
                }

            }
            RibbonPanel intElevPanel = application.CreateRibbonPanel(tabName, "Interior Elevations");
            AddPushButton(intElevPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel intElevPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData intElevButtonData = new PushButtonData("Place Elevations", "Place Elevations", assembly.Location, "LM2.Revit.InteriorElevationsGoal2");
            PushButton placeIntElevButton = intElevPanel.AddItem(intElevButtonData) as PushButton;

            placeIntElevButton.ToolTip = "Automatically places interior elevations into all bound rooms";
            var imgUri = assemblyDir + @"\01-PlaceIntElev_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            placeIntElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
