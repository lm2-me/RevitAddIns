using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LM2.Revit
{
    public partial class RenameElevationsDialog : System.Windows.Forms.Form
    {
        public RenameElevationsDialog(Autodesk.Revit.ApplicationServices.Application application, List<KeyValuePair<ViewSection, String>> newNames)
        {
            InitializeComponent();

            titleLabel.Text = "LM2.Revit Rename Elevations"; 
            TextLabel.Text = "Please review the list of elevation view names in the list and accept to change the names.";

            var dataSource = new BindingSource();
            dataSource.DataSource = newNames.Select(ConvertRow).ToList();

            dataGridView.AutoGenerateColumns = false;
            dataGridView.DataSource = dataSource;
            elevationName.DataPropertyName = "OldName";
            RenameTo.DataPropertyName = "NewName";

            string nameList = String.Join(",", newNames.Select(ConvertRow).Select(r => r.ToString()));
            application.WriteJournalComment(nameList, true);

            dataGridView.Refresh();
        }

        private GridContent ConvertRow (KeyValuePair<ViewSection, String> row)
        {
            GridContent nameRow = new GridContent();
            nameRow.OldName = row.Key.Name;
            nameRow.NewName = row.Value;

            return nameRow;
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    public class GridContent
    {
        public string OldName { get; set; }
        public string NewName { get; set; }

        public override string ToString ()
        {
            return $"(${this.OldName}, ${this.NewName})";
        }

    }
}

