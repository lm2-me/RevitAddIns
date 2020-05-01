using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LM2.Revit
{
    /// <summary>
    /// Interaction logic for CreateExteriorElevationInput.xaml
    /// </summary>
    public partial class CreateExteriorElevationInput1 : Window
    {
        public List<Wall> UserWallSelection;

        private UIDocument activeDoc;
        public CreateExteriorElevationInput1(UIDocument currentDoc)
        {
            activeDoc = currentDoc;            
            InitializeComponent();

        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SelectWalls_Button_Click(object sender, RoutedEventArgs e)
        {

            DialogResult = true;
            this.Close();
        }
    }
}
