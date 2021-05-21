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
    public class LevelsFromExcelRibbon : IExternalApplication
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

                if (rp.Name == "Import/Export")
                {
                    AddPushButton(rp);
                    return Result.Succeeded;
                }

            }
            RibbonPanel intElevPanel = application.CreateRibbonPanel(tabName, "Import/Export");
            AddPushButton(intElevPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel importPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData levelFromExcelButtonData = new PushButtonData("Levels From Excel", "Levels From Excel", assembly.Location, "LM2.Revit.LevelsFromExcel");
            PushButton levelFromExcelButton = importPanel.AddItem(levelFromExcelButtonData) as PushButton;

            levelFromExcelButton.ToolTip = "Automatically places interior elevations into all bound rooms";
            var imgUri = assemblyDir + @"\LevelsExcel_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            levelFromExcelButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
