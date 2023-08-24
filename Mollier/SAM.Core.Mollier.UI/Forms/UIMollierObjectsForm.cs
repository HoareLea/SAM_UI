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
        private List<UIMollierPoint> uIMollierPoints;
        private List<UIMollierProcess> uIMollierProcesses;
        private List<UIMollierZone> uIMollierZones;
        private MollierControlSettings mollierControlSettings;

        public event MollierProcessRemovedEventHandler MollierProcessRemoved;
        public event MollierPointRemovedEventHandler MollierPointRemoved;
        public event MollierProcessEditedEventHandler MollierProcessEdited;
        public event MollierPointEditedEventHandler MollierPointEdited;

        private double airflow = 0;
        private Units.UnitType airFlowUnit = Units.UnitType.CubicMeterPerSecond;

        public UIMollierObjectsForm()
        {
            InitializeComponent();

            DataGridView_MollierProcesses.AutoGenerateColumns = false;
            DataGridView_MollierPoints.AutoGenerateColumns = false;
        }
        public UIMollierObjectsForm(List<UIMollierPoint> mollierPoints, List<UIMollierProcess> mollierProcesses, List<UIMollierZone> mollierZones, MollierControlSettings mollierControlSettings)   
        {
            this.mollierControlSettings = mollierControlSettings;

            InitializeComponent();
            DataGridView_MollierProcesses.AutoGenerateColumns = false;
            DataGridView_MollierPoints.AutoGenerateColumns = false;

            Refresh(mollierPoints, mollierProcesses, mollierZones);
        }
        public void Refresh(List<UIMollierPoint> mollierPoints = null, List<UIMollierProcess> mollierProcesses = null, List<UIMollierZone> mollierZones = null)
        {
            this.uIMollierPoints = mollierPoints;
            this.uIMollierProcesses = mollierProcesses;
            this.uIMollierZones = mollierZones;

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
            if(dataGridViewButtonColumn == null || e.RowIndex < 0)
            {
                return;
            }

            DisplayUIMollierObject displayUIMollierObject = dataGridView?.Rows[e.RowIndex]?.DataBoundItem as DisplayUIMollierObject;

            bool remove = dataGridViewButtonColumn.HeaderText == "Remove";
            bool edit = dataGridViewButtonColumn.HeaderText == "Edit";

            if(remove)
            {
                removeObject(displayUIMollierObject);
            }
            else if(edit)
            {
                editObject(displayUIMollierObject);
            }
        }
        private void DataGridView_MollierProcesses_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

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

            editObject(displayUIMollierObject);
        }

        private List<DataGridViewRow> selectedRows()
        {
            TabPage tabPage = customizeMollierObjectsTabControl.SelectedTab;
            return (tabPage.Controls.Cast<Control>().ToList().Find(x => x is DataGridView) as DataGridView)?.SelectedRows?.Cast<DataGridViewRow>().ToList();
        }
        private void ToolStripMenuItem_Remove_Click(object sender, EventArgs e)
        {
            DataGridViewRow dataGridViewRow = selectedRows()?.FirstOrDefault();
            if(dataGridViewRow == null)
            {
                return;
            }


            DisplayUIMollierObject displayUIMollierObject = dataGridViewRow?.DataBoundItem as DisplayUIMollierObject;

            removeObject(displayUIMollierObject);
        }


        private void generateDataGridViews()
        {
            Pressure_TextBox.Text = mollierControlSettings.Pressure.ToString();
            //Column_MollierProcess_MassFlow.HeaderText += " [" + airFlowUnit + "]";

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
        }
        private void editObject(DisplayUIMollierObject displayUIMollierObject)
        {
            if (displayUIMollierObject == null)
            {
                return;
            }

            if (displayUIMollierObject.UIMollierObject is UIMollierProcess)
            {
                UIMollierProcess uIMollierProcess = (UIMollierProcess)displayUIMollierObject.UIMollierObject;
                UIMollierProcess newUIMollierProcess = new UIMollierProcess(uIMollierProcess);

                using (CustomizeProcessForm customizeProcessForm = new CustomizeProcessForm(uIMollierProcess))
                {
                    if (customizeProcessForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    newUIMollierProcess = customizeProcessForm.UIMollierProcess; 
                }

                MollierProcessEditedEventArgs mollierProcessEditedEventArgs = new MollierProcessEditedEventArgs(uIMollierProcess, newUIMollierProcess);
                MollierProcessEdited.Invoke(this, mollierProcessEditedEventArgs);
                uIMollierProcesses = mollierProcessEditedEventArgs.UIMollierProcesses;

                generateDataGridViews();
            }
            else if (displayUIMollierObject.UIMollierObject is UIMollierPoint)
            {
                UIMollierPoint uIMollierPoint = (UIMollierPoint)displayUIMollierObject.UIMollierObject;
                UIMollierPoint newUIMollierPoint = new UIMollierPoint(uIMollierPoint);

                using (CustomizePointForm customizePointForm = new CustomizePointForm(uIMollierPoint))
                {
                    if (customizePointForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    newUIMollierPoint = customizePointForm.UIMollierPoint;
                }

                MollierPointEdited.Invoke(this, new MollierPointEditedEventArgs(uIMollierPoint, newUIMollierPoint));
                uIMollierPoints.Remove(uIMollierPoint);
                uIMollierPoints.Add(newUIMollierPoint);

                generateDataGridViews();
            }
        }
        private void removeObject(DisplayUIMollierObject displayUIMollierObject)
        {
            if(displayUIMollierObject == null)
            {
                return;
            }

            var confirmResult = MessageBox.Show("Are you sure to delete this item ?", "Delete Confirmation",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.No)
            {
                return;
            }

            if (displayUIMollierObject.UIMollierObject is UIMollierProcess)
            {
                UIMollierProcess uIMollierProcess = (UIMollierProcess)displayUIMollierObject.UIMollierObject;
                MollierProcessRemovedEventArgs mollierProcessRemovedEventArgs = new MollierProcessRemovedEventArgs(uIMollierProcess);
                MollierProcessRemoved.Invoke(this, mollierProcessRemovedEventArgs);

                uIMollierProcesses = mollierProcessRemovedEventArgs.UIMollierProcesses;
                generateDataGridViews();
            }
            else if (displayUIMollierObject.UIMollierObject is UIMollierPoint)
            {
                UIMollierPoint uIMollierPoint = (UIMollierPoint)displayUIMollierObject.UIMollierObject;
                MollierPointRemoved.Invoke(this, new MollierPointRemovedEventArgs(uIMollierPoint));

                uIMollierPoints.Remove(uIMollierPoint);
                generateDataGridViews();
            }
        }

    }
}
