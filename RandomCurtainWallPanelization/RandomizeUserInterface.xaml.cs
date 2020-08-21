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
    /// Interaction logic for RandomizeUserInterface.xaml
    /// </summary>
    public partial class RandomizeUserInterface : Window
    {
        public bool? HorizontalRandom => chk_horizRandom.IsChecked;
        public bool? VerticalRandom => chk_vertRandom.IsChecked;

        public int WPFHorizProbab = 25;
        public int WPFVertProbab = 25;

        public bool? DeleteMullions => chk_deleteMullions.IsChecked;

        public bool? UseOwn => rb_UseOwn.IsChecked;
        public int WPFMaxHorizDelete = 3;
        public int WPFMaxVertDelete = 3;

        public bool? CreateForMe => rb_CreateForMe.IsChecked;
        public double WPFMinWidth = 2;
        public double WPFMaxWidth = 4;
        public double WPFMinHeight = 2;
        public double WPFMaxHeight = 4;

        private bool manualSetting = true;

        //sliders
        List<int> sliderValues = new List<int>() { 100, 0, 0, 0, 0 };

        List<Tuple<string, string, string, string>> materialSelections => new List<Tuple<string, string, string, string>>
        {
            new Tuple<string, string, string, string>("cwPanelType1", "sl1", "sl1lock", "sl1text"),
            new Tuple<string, string, string, string>("cwPanelType2", "sl2", "sl2lock", "sl2text"),
            new Tuple<string, string, string, string>("cwPanelType3", "sl3", "sl3lock", "sl3text"),
            new Tuple<string, string, string, string>("cwPanelType4", "sl4", "sl4lock", "sl4text"),
            new Tuple<string, string, string, string>("cwPanelType5", "sl5", "sl5lock", "sl5text")
        };

        List<KeyValuePair<string, Int32>> materialOptions { get; set; }

        public RandomizeUserInterface(List<KeyValuePair<string, Int32>> materialOptionsInput)
        {
            materialOptions = materialOptionsInput;

            InitializeComponent();
            setSliderValues();

            bool isfirst = true;
            foreach (var matselec in materialSelections)
            {
                ComboBox selector = (ComboBox)this.FindName(matselec.Item1);
                selector.ItemsSource = materialOptions.Select(o => o);
                selector.DisplayMemberPath = "Key";
                selector.SelectedValuePath = "Value";
                selector.SelectedIndex = 0;

                if (isfirst)
                {
                    isfirst = false;
                    materialOptions.Insert(0, new KeyValuePair<string, int>("(none)", -1));
                    continue;
                }

                Slider disableSlider = (Slider)this.FindName(matselec.Item2);
                disableSlider.IsEnabled = false;

                CheckBox disableCheckBox = (CheckBox)this.FindName(matselec.Item3);
                disableCheckBox.IsEnabled = false;

                TextBox disableTextBox = (TextBox)this.FindName(matselec.Item4);
                disableTextBox.IsEnabled = false;
            }
        }

        private void equalizeSliders(int sliderIndex, int amountChanged, int fromIndex = -1, int iteration = 0)
        {
            if (iteration == 0)
            {
                sliderValues[sliderIndex] += amountChanged;
            }

            List<int> assignableSliderIndexes = sliderValues.
                Select((v, i) => new { index = i, value = v }).
                Where(v =>
                    v.index != sliderIndex
                    && isSliderEnabled(v.index)
                    && (amountChanged > 0 && v.value > 0 || amountChanged < 0 && v.value < 100)
                ).
                Select(v => v.index).
                ToList();

            if (fromIndex >= 0)
            {
                sliderValues[fromIndex] = 0;
            }

            if (assignableSliderIndexes.Count == 0 || iteration > 10)
            {
                sliderValues[sliderIndex] -= amountChanged;
                return;
            }

            int perSliderChange = (int)(amountChanged / assignableSliderIndexes.Count);

            foreach (var i in assignableSliderIndexes)
            {
                sliderValues[i] -= perSliderChange;
            }

            int roundingError = amountChanged - perSliderChange * assignableSliderIndexes.Count;
            sliderValues[assignableSliderIndexes.First()] -= roundingError;

            List<int> negativeSliderIndexes = sliderValues.
                Select((v, i) => new { index = i, value = v }).
                Where(v => v.value < 0).
                Select(v => v.index).
                ToList();

            foreach (var i in negativeSliderIndexes)
            {
                int changeValue = -sliderValues[i];
                this.equalizeSliders(sliderIndex, changeValue, i, iteration + 1);
            }
        }


        private void setSliderValues()
        {
            this.manualSetting = true;

            for (int i = 0; i < materialSelections.Count(); i++)
            {
                var matselec = materialSelections[i];

                Slider updateslider = (Slider)this.FindName(matselec.Item2);
                updateslider.Value = sliderValues[i];

                TextBox updateTextbox = (TextBox)this.FindName(matselec.Item4);
                updateTextbox.Text = sliderValues[i].ToString();
            }

            this.manualSetting = false;

        }

        private bool isSliderEnabled(int index)
        {
            string sliderName = materialSelections[index].Item2;
            Slider lockedSlider = (Slider)this.FindName(sliderName);
            return lockedSlider.IsEnabled;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            WPFHorizProbab = string.IsNullOrEmpty(tbx_ChofRemHoriz.Text) ? WPFHorizProbab : Int32.Parse(tbx_ChofRemHoriz.Text);
            WPFVertProbab = string.IsNullOrEmpty(tbx_ChofRemVert.Text) ? WPFVertProbab : Int32.Parse(tbx_ChofRemVert.Text);

            WPFMaxHorizDelete = string.IsNullOrEmpty(txtbx_MaxConsecHoriz.Text) ? WPFMaxHorizDelete : Int32.Parse(txtbx_MaxConsecHoriz.Text);
            WPFMaxVertDelete = string.IsNullOrEmpty(txtbx_MaxConsecVert.Text) ? WPFMaxVertDelete : Int32.Parse(txtbx_MaxConsecVert.Text);

            WPFMinWidth = string.IsNullOrEmpty(txtbx_minWidth.Text) ? WPFMinWidth : Double.Parse(txtbx_minWidth.Text);
            WPFMaxWidth = string.IsNullOrEmpty(txtbx_maxWidth.Text) ? WPFMaxWidth : Double.Parse(txtbx_maxWidth.Text);
            WPFMinHeight = string.IsNullOrEmpty(txtbx_minHeight.Text) ? WPFMinHeight : Double.Parse(txtbx_minHeight.Text);
            WPFMaxHeight = string.IsNullOrEmpty(txtbx_maxHeight.Text) ? WPFMaxHeight : Double.Parse(txtbx_maxHeight.Text);

            this.DialogResult = true;
            this.Close();

        }

        private void tbx_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox box = sender as TextBox;
            int num = 0;

            if (int.TryParse(e.Text, out num))
            {
                int totalNum = int.Parse(box.Text + e.Text);
                if (totalNum <= 100)
                {
                    //in range
                    e.Handled = false;
                }
                else
                {
                    // not in range
                    e.Handled = true;
                }
            }
            else
            {
                //invalid number
                e.Handled = true;
            }
        }

        private void txtbx_PreviewTextInputNumber(object sender, TextCompositionEventArgs e)
        {
            TextBox box = sender as TextBox;
            int num = 0;
            bool hasDecimal = box.Text.Contains(".");

            if ((e.Text == "." && !hasDecimal) || int.TryParse(e.Text, out num))
            {
                //valid number
                e.Handled = false;
            }

            else
            {
                //invalid number
                e.Handled = true;
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (txtbx_MaxConsecHoriz == null)
            {
                return;
            }
            var rb = (RadioButton)sender;
            if (HorizontalRandom == true)
            {
                txtbx_MaxConsecHoriz.IsEnabled = true;
            }

            if (VerticalRandom == true)
            {
                txtbx_MaxConsecVert.IsEnabled = true;
            }

            txtbx_minWidth.IsEnabled = false;
            txtbx_maxWidth.IsEnabled = false;
            txtbx_minHeight.IsEnabled = false;
            txtbx_maxHeight.IsEnabled = false;
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            if (txtbx_minWidth == null)
            {
                return;
            }
            var rb = (RadioButton)sender;
            txtbx_MaxConsecHoriz.IsEnabled = false;
            txtbx_MaxConsecVert.IsEnabled = false;

            txtbx_minWidth.IsEnabled = true;
            txtbx_maxWidth.IsEnabled = true;
            txtbx_minHeight.IsEnabled = true;
            txtbx_maxHeight.IsEnabled = true;
        }

        private void chk_horizRandom_Checked(object sender, RoutedEventArgs e)
        {
            tbx_ChofRemHoriz.IsEnabled = true;
            if (UseOwn == true)
            {
                txtbx_MaxConsecHoriz.IsEnabled = true;
            }
        }

        private void chk_vertRandom_Checked(object sender, RoutedEventArgs e)
        {
            tbx_ChofRemVert.IsEnabled = true;
            if (UseOwn == true)
            {
                txtbx_MaxConsecVert.IsEnabled = true;
            }
        }

        private void chk_horizRandom_Unchecked(object sender, RoutedEventArgs e)
        {
            tbx_ChofRemHoriz.IsEnabled = false;
            if (UseOwn == true)
            {
                txtbx_MaxConsecHoriz.IsEnabled = false;
            }
        }

        private void chk_vertRandom_Unchecked(object sender, RoutedEventArgs e)
        {
            tbx_ChofRemVert.IsEnabled = false;
            if (UseOwn == true)
            {
                txtbx_MaxConsecVert.IsEnabled = false;
            }
        }

        private void sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (manualSetting) return;

            Slider changedSlider = (Slider)sender;
            var materialSelection = materialSelections.FirstOrDefault(s => s.Item2 == changedSlider.Name);
            int index = materialSelections.IndexOf(materialSelection);

            equalizeSliders(index, (int)(e.NewValue - e.OldValue));
            setSliderValues();
        }

        private void chk_sliderLock_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox lockCheck = (CheckBox)sender;
            var materialSelection = materialSelections.FirstOrDefault(s => s.Item3 == lockCheck.Name);
            string sliderName = materialSelection?.Item2;
            string textBoxName = materialSelection?.Item4;

            Slider lockedSlider = (Slider)this.FindName(sliderName);
            lockedSlider.IsEnabled = false;

            TextBox lockTextBox = (TextBox)this.FindName(textBoxName);
            lockTextBox.IsEnabled = false;
        }

        private void chk_sliderLock_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox lockCheck = (CheckBox)sender;
            var materialSelection = materialSelections.FirstOrDefault(s => s.Item3 == lockCheck.Name);
            string sliderName = materialSelection?.Item2;
            string textBoxName = materialSelection?.Item4;

            Slider lockedSlider = (Slider)this.FindName(sliderName);
            lockedSlider.IsEnabled = true;

            TextBox lockTextBox = (TextBox)this.FindName(textBoxName);
            lockTextBox.IsEnabled = true;
        }

        private void sltext_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textInput = (TextBox)sender;
            string textValue = textInput.Text;

            var materialSelection = materialSelections.FirstOrDefault(s => s.Item4 == textInput.Name);
            string sliderName = materialSelection?.Item2;
            Slider slider = (Slider)this.FindName(sliderName);

            int originalValue = (int)slider.Value;

            if (!Int32.TryParse(textValue, out int numValue))
            {
                textInput.Text = originalValue.ToString();
                return;
            }

            int index = materialSelections.IndexOf(materialSelection);
            equalizeSliders(index, (int)(numValue - originalValue));
            setSliderValues();

        }

        private void cwPanelType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox materialCombo = (ComboBox)sender;
            var materialSelection = materialSelections.FirstOrDefault(s => s.Item1 == materialCombo.Name);

            if ((int)materialCombo.SelectedValue == -1)
            {
                CheckBox disableCheckBox = (CheckBox)this.FindName(materialSelection.Item3);
                disableCheckBox.IsChecked = false;
                disableCheckBox.IsEnabled = false;

                Slider disableSlider = (Slider)this.FindName(materialSelection.Item2);
                disableSlider.IsEnabled = false;

                TextBox disableTextBox = (TextBox)this.FindName(materialSelection.Item4);
                disableTextBox.IsEnabled = false;

                int totalActive = materialSelections.Where(s => ((Slider)this.FindName(s.Item2)).IsEnabled).Count();
                if (totalActive == 0)
                {
                    var firstSelection = materialSelections.First();
                    CheckBox firstCheckBox = (CheckBox)this.FindName(firstSelection.Item3);
                    firstCheckBox.IsChecked = false;

                }

                int index = materialSelections.IndexOf(materialSelection);
                equalizeSliders(index, -(int)(disableSlider.Value));
                setSliderValues();
            }

            else
            {

                CheckBox enableCheckbox = (CheckBox)this.FindName(materialSelection.Item3);
                enableCheckbox.IsEnabled = true;

                Slider enableSlider = (Slider)this.FindName(materialSelection.Item2);
                enableSlider.IsEnabled = !enableCheckbox.IsChecked ?? false;

                TextBox enableTextBox = (TextBox)this.FindName(materialSelection.Item4);
                enableTextBox.IsEnabled = !enableCheckbox.IsChecked ?? false;
            }
        }
    }
}
