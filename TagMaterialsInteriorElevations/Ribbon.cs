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
    public class TagMaterialsIntElevRibbon : IExternalApplication
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

                if (rp.Name == "Materials")
                {
                    AddPushButton(rp);
                    return Result.Succeeded;
                }

            }
            RibbonPanel materialPanel = application.CreateRibbonPanel(tabName, "Materials");
            AddPushButton(materialPanel);
            return Result.Succeeded;

        }

        private void AddPushButton(RibbonPanel materialPanel)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assemblyDir = new FileInfo(assembly.Location).Directory.FullName;
            PushButtonData materialTagIntElevButtonData = new PushButtonData("Tag Materials", "Tag Materials", assembly.Location, "LM2.Revit.TagMaterialsInteriorElevations");
            PushButton renameIntElevButton = materialPanel.AddItem(materialTagIntElevButtonData) as PushButton;

            renameIntElevButton.ToolTip = "Tags materials in an interior elevation view";
            var imgUri = assemblyDir + @"\TagMaterials_16x16.png";
            System.Diagnostics.Debug.WriteLine(imgUri);
            renameIntElevButton.LargeImage = new BitmapImage(new Uri(imgUri, UriKind.Absolute));
        }

    }
}
 