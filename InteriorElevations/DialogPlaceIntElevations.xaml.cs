using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class DialogPlaceIntElevations : Window
    {
        public ElementId SelectedViewFamilyType
        {
            get
            {
                return (ElementId)viewFamilyTypeBox.SelectedValue;
            }
        }

        public int SelectedScale
        {
            get
            {
                return (int)scaleTypeBox.SelectedValue;
            }
        }

        public ElementId SelectedViewTemplate
        {
            get
            {
                return (ElementId)viewTemplateTypeBox.SelectedValue;
            }
        }

        public ElementId SelectedFilledRegion
        {
            get
            {
                return (ElementId)filledRegionTypeBox.SelectedValue;
            }
        }

        public ElementId SelectedLineStyle
        {
            get
            {
                return (ElementId)lineStyleTypeBox.SelectedValue;
            }
        }

        public DialogPlaceIntElevations(
            Autodesk.Revit.ApplicationServices.Application application,
            List<ViewFamilyType> viewFamilyTypeList,
            List<Autodesk.Revit.DB.View> viewTemplateList,
            List<FilledRegionType> filledRegionList,
            List<Element> lineStyleList
        )
        {
            InitializeComponent();

            //View Family
            viewFamilyTypeBox.ItemsSource = viewFamilyTypeList.OrderBy(l => l.Name).ToList();
            viewFamilyTypeBox.DisplayMemberPath = "Name";
            viewFamilyTypeBox.SelectedValuePath = "Id";

            //View Scale
            scaleTypeBox.ItemsSource = ScaleHelpers.ScaleList;
            scaleTypeBox.DisplayMemberPath = "Key";
            scaleTypeBox.SelectedValuePath = "Value";

            //View Template
            viewTemplateTypeBox.ItemsSource = viewTemplateList.OrderBy(l => l.Name).ToList();
            viewTemplateTypeBox.DisplayMemberPath = "Name";
            viewTemplateTypeBox.SelectedValuePath = "Id";

            //Filled Region
            filledRegionTypeBox.ItemsSource = filledRegionList.OrderBy(l => l.Name).ToList();
            filledRegionTypeBox.DisplayMemberPath = "Name";
            filledRegionTypeBox.SelectedValuePath = "Id";

            //Line Style
            lineStyleTypeBox.ItemsSource = lineStyleList.OrderBy(l => l.Name).ToList();
            lineStyleTypeBox.DisplayMemberPath = "Name";
            lineStyleTypeBox.SelectedValuePath = "Id";


        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
