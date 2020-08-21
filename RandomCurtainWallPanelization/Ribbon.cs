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
    public class RandomCurtainWallPanelizationRibbon : IExternalApplication
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

                if (rp.Name == "Curtain Walls")
                {
                    AddPushButton(rp);
                    return Result.Succeeded;
                }

            }
            RibbonPanel curtainWallPanel = application.CreateRibbonPanel(tabName, "Curtain Walls");
            AddPushButton(curtainWallPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel curtainWallPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData cWRandomizerButtonData = new PushButtonData("Randomizer", "Randomizer", assembly.Location, "LM2.Revit.RandomCurtainWallPanelization");
            PushButton cWRandomizerButton = curtainWallPanel.AddItem(cWRandomizerButtonData) as PushButton;

            cWRandomizerButton.ToolTip = "Randomizes Curtain Wall Panels";
            var imgUri = assemblyDir + @"\04-Randomize_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            cWRandomizerButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
 