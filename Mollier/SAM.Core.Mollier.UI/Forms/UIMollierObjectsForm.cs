using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class UIMollierObjectsForm : Form
    {
        private MollierModel mollierModel;
        private MollierControlSettings mollierControlSettings;

        public event MollierModelEditedEventHandler MollierModelEdited;

        private double airflow = 0;
        private Units.UnitType airFlowUnit = Units.UnitType.CubicMeterPerSecond;

        public UIMollierObjectsForm()
        {
            InitializeComponent();

            DataGridView_MollierProcesses.AutoGenerateColumns = false;
            DataGridView_MollierPoints.AutoGenerateColumns = false;
        }
        public UIMollierObjectsForm(MollierModel mollierModel, MollierControlSettings mollierControlSettings)   
        {
            this.mollierControlSettings = mollierControlSettings;

            InitializeComponent();
            DataGridView_MollierProcesses.AutoGenerateColumns = false;
            DataGridView_MollierPoints.AutoGenerateColumns = false;

            Refresh(mollierModel);
        }
        public void Refresh(MollierModel mollierModel = null)
        {
            this.mollierModel = mollierModel;

            generateDataGridViews();
        }

        private void ManageMollierObjectsForm_Load(object sender, EventArgs e)
        {
            SupplyAirflow_ComboBox.Text = "m3/s";
            ExhaustAirflow_Combobox.Text = "m3/s";
        }
        private void SupplyAirFlow_TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!Core.Query.TryConvert(SupplyAirFlow_TextBox.Text, out double supplyAirFlow))
            {
                return;
            }

            airflow = supplyAirFlow;
            generateDataGridViews();
        }
        private void DataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;
            if(dataGridView == null)
            {
                return;
            }
            DataGridViewButtonColumn dataGridViewButtonColumn = dataGridView.Columns[e.ColumnIndex] as DataGridViewButtonColumn;
            DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn = dataGridView.Columns[e.ColumnIndex] as DataGridViewCheckBoxColumn;
            DisplayUIMollierObject displayUIMollierObject = dataGridView?.Rows[e.RowIndex]?.DataBoundItem as DisplayUIMollierObject;


            if (dataGridViewButtonColumn == null || e.RowIndex < 0)
            {
                DataGridViewRow row = dataGridView.Rows[e.RowIndex];

                if (dataGridViewCheckBoxColumn == null || dataGridViewCheckBoxColumn.HeaderText != "Visible")
                {
                    return;
                }

                if(displayUIMollierObject.UIMollierObject is UIMollierPoint)
                {
                    DataGridViewCheckBoxCell cell = row.Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;
                    if(cell.Value != null)
                    {
                        cell.Value = !(bool)cell.Value;
                    }
                }
                else
                {
                    DataGridViewRow row2 = null;
                    if (e.RowIndex % 2 == 0)
                    {
                        row2 = dataGridView.Rows[e.RowIndex + 1];
                    }
                    else
                    {
                        row2 = dataGridView.Rows[e.RowIndex - 1];
                    }
                 
                    DataGridViewCheckBoxCell cell = row.Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;
                    DataGridViewCheckBoxCell cell2 = row2.Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;

                    if (cell.Value == null || cell2.Value == null)
                    {
                        return;
                    }
                    cell.Value = !(bool)cell.Value;
                    cell2.Value = !(bool)cell2.Value;
                }
                displayUIMollierObject.UIMollierObject.UIMollierAppearance.Visible = !displayUIMollierObject.UIMollierObject.UIMollierAppearance.Visible;
                editObject(displayUIMollierObject.UIMollierObject, displayUIMollierObject.UIMollierObject);

                return;
            }


            bool remove = dataGridViewButtonColumn.HeaderText == "Remove";
            bool edit = dataGridViewButtonColumn.HeaderText == "Edit";

            if(remove)
            {
                removeObject(displayUIMollierObject.UIMollierObject);
            }
            else if(edit)
            {
                IUIMollierObject newMollierObject = getCustomObject(displayUIMollierObject.UIMollierObject);
                modifyRow(dataGridView.Rows[e.RowIndex], newMollierObject);
                editObject(displayUIMollierObject.UIMollierObject, newMollierObject);
            }
        }
        private void SupplyAirFlow_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            //For now there is no exhaust airflow but there'll be imlemented switching between ariflows
            SupplyAirFlow_CheckBox.Checked = true;
            return;
        }
        private void SupplyAirflow_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(SupplyAirFlow_CheckBox.Checked)
            {
                switch (SupplyAirFlow_CheckBox.Text)
                {
                    case "m3/s":
                        airFlowUnit = Units.UnitType.CubicMeterPerSecond;
                        break;
                    case "m3/h":
                        airFlowUnit = Units.UnitType.CubicMeterPerHour;
                        break;
                }
            }
        }
        private void ToolStripMenuItem_Edit_Click(object sender, EventArgs e)
        {
            DataGridViewRow dataGridViewRow = selectedRows()?.FirstOrDefault();
            if (dataGridViewRow == null)
            {
                return;
            }

            DisplayUIMollierObject displayUIMollierObject = dataGridViewRow?.DataBoundItem as DisplayUIMollierObject;

            IUIMollierObject newMollierObject = getCustomObject(displayUIMollierObject.UIMollierObject);
           // modifyRow(dataGridViewRow, newMollierObject);
            editObject(displayUIMollierObject.UIMollierObject, newMollierObject);
        }
        private void ToolStripMenuItem_Remove_Click(object sender, EventArgs e)
        {
            DataGridViewRow dataGridViewRow = selectedRows()?.FirstOrDefault();
            if(dataGridViewRow == null)
            {
                return;
            }

            DisplayUIMollierObject displayUIMollierObject = dataGridViewRow?.DataBoundItem as DisplayUIMollierObject;
            removeObject(displayUIMollierObject.UIMollierObject);
        }

        private List<DataGridViewRow> selectedRows()
        {
            TabPage tabPage = customizeMollierObjectsTabControl.SelectedTab;
            return (tabPage.Controls.Cast<Control>().ToList().Find(x => x is DataGridView) as DataGridView)?.SelectedRows?.Cast<DataGridViewRow>().ToList();
        }

        private void modifyRow(DataGridViewRow dataGridViewRow, IUIMollierObject mollierObject)
        {
            if(mollierObject == null)
            {
                return;
            }

            if(mollierObject is UIMollierPoint)
            {
                DataGridViewTextBoxCell dataGridViewTextBoxCell = (DataGridViewTextBoxCell)dataGridViewRow.Cells[1];
                dataGridViewTextBoxCell.Value = mollierObject.UIMollierAppearance.Label;
            }
            else if(mollierObject is UIMollierProcess)
            {
                DataGridViewTextBoxCell dataGridViewTextBoxCell1 = (DataGridViewTextBoxCell)dataGridViewRow.Cells[2];
                DataGridViewTextBoxCell dataGridViewTextBoxCell2 = (DataGridViewTextBoxCell)dataGridViewRow.Cells[3];
                dataGridViewTextBoxCell1.Value = ((UIMollierProcess)mollierObject).UIMollierAppearance_Start.Label;
                dataGridViewTextBoxCell2.Value = ((UIMollierProcess)mollierObject).UIMollierAppearance_End.Label;
            }
        }
        private IUIMollierObject getCustomObject(IUIMollierObject mollierObject)
        {
            IUIMollierObject newMollierObject = null;

            if (mollierObject is UIMollierPoint)
            {
                UIMollierPoint uIMollierPoint = (UIMollierPoint)mollierObject;
                UIMollierPoint newUIMollierPoint = new UIMollierPoint(uIMollierPoint);

                using (CustomizePointForm customizePointForm = new CustomizePointForm(uIMollierPoint))
                {
                    if (customizePointForm.ShowDialog() != DialogResult.OK)
                    {
                        return mollierObject;
                    }
                    newUIMollierPoint = customizePointForm.UIMollierPoint;
                }
                newMollierObject = newUIMollierPoint;
            }
            else
            {
                UIMollierProcess uIMollierProcess = (UIMollierProcess)mollierObject;
                UIMollierProcess newUIMollierProcess = new UIMollierProcess(uIMollierProcess);

                using (CustomizeProcessForm customizeProcessForm = new CustomizeProcessForm(uIMollierProcess))
                {
                    if (customizeProcessForm.ShowDialog() != DialogResult.OK)
                    {
                        return mollierObject;
                    }
                    newUIMollierProcess = customizeProcessForm.UIMollierProcess;
                }
                newMollierObject = newUIMollierProcess;
            }

            return newMollierObject;
        }
        private void generateDataGridViews()
        {
            Pressure_TextBox.Text = mollierControlSettings.Pressure.ToString();
            //Column_MollierProcess_MassFlow.HeaderText += " [" + airFlowUnit + "]";

            List<UIMollierPoint> uIMollierPoints = mollierModel.GetMollierObjects<UIMollierPoint>();
            List<UIMollierProcess> uIMollierProcesses = mollierModel.GetMollierObjects<UIMollierProcess>();

            if (uIMollierPoints != null)
            {
                DataGridView_MollierPoints.DataSource = uIMollierPoints.ConvertAll(x => new DisplayUIMollierObject(x));
            }
            if (uIMollierProcesses != null)
            {
                List<DisplayUIMollierObject> displayAnalyticalObjects = new List<DisplayUIMollierObject>();
                foreach (UIMollierProcess uIMollierProcess in uIMollierProcesses)
                {
                    displayAnalyticalObjects.Add(new DisplayUIMollierObject(uIMollierProcess, 0, airflow, airFlowUnit));
                    displayAnalyticalObjects.Add(new DisplayUIMollierObject(uIMollierProcess, 1, airflow, airFlowUnit));
                }
                DataGridView_MollierProcesses.DataSource = displayAnalyticalObjects;
            }
            DataGridViewColumn test = DataGridView_MollierProcesses.Columns[0];
        }
        private void editObject(IUIMollierObject mollierObject, IUIMollierObject newMollierObject)
        {
            if (mollierObject == null || newMollierObject == null)
            {
                return;
            }

            mollierModel.Update(mollierObject, newMollierObject);
            MollierModelEditedEventArgs mollierModelEditedEventArgs = new MollierModelEditedEventArgs(mollierModel);
            MollierModelEdited.Invoke(this, mollierModelEditedEventArgs);
            generateDataGridViews();
        }
        private void removeObject(IUIMollierObject mollierObject)
        {
            if(mollierObject == null)
            {
                return;
            }

            var confirmResult = MessageBox.Show("Are you sure to delete this item ?", "Delete Confirmation",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.No)
            {
                return;
            }

            mollierModel.Remove(mollierObject);
            MollierModelEditedEventArgs mollierModelEditedEventArgs = new MollierModelEditedEventArgs(mollierModel);
            MollierModelEdited.Invoke(this, mollierModelEditedEventArgs);
            generateDataGridViews();
        }
    }
}
