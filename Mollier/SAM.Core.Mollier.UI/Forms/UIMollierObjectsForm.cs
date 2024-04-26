using SAM.Geometry.Mollier;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class UIMollierObjectsForm : Form
    {
        private MollierModel mollierModel;
        private MollierControlSettings mollierControlSettings;

        public event MollierModelEditedEventHandler MollierModelEdited;
        public event MollierObjectSelectedEventHandler MollierObjectSelected;

        // Default private variables
        private double airflow = 0;
        private Units.UnitType airFlowUnit = Units.UnitType.CubicMeterPerSecond;
        private string defaultGroup = "All";

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
            initializeDataGridViews(mollierModel);
            Refresh(mollierModel);
        }
        public void Refresh(MollierModel mollierModel = null)
        {
            this.mollierModel = mollierModel;
            regenerateDataGridViews();
        }
        
        #region Initialization
        private void initializeDataGridViews(MollierModel mollierModel)
        {
            DataGridView_MollierProcesses.AutoGenerateColumns = false;
            DataGridView_MollierPoints.AutoGenerateColumns = false;
            PressurePoints_TextBox.Text = mollierControlSettings.Pressure.ToString();
            PressureProcesses_TextBox.Text = mollierControlSettings.Pressure.ToString();
           // initializeColumnsHeaders();

            // Initializing Air flow
            SupplyAirflow_ComboBox.Text = "m3/s";
            ExhaustAirflow_Combobox.Text = "m3/s";

            // Initializing groups selecting 
            GroupSelectionProcesses_ComboBox.Items.Add(defaultGroup);
            GroupSelectionProcesses_ComboBox.SelectedItem = GroupSelectionProcesses_ComboBox.Items[0];
            GroupSelectionPoints_ComboBox.Items.Add(defaultGroup);
            GroupSelectionPoints_ComboBox.SelectedItem = GroupSelectionPoints_ComboBox.Items[0];

            if(mollierModel == null)
            {
                return;
            }
            List<MollierGroup> mollierGroups = mollierModel.GetMollierObjects<MollierGroup>(false);
            mollierGroups?.ForEach(x =>
            {
                if (!string.IsNullOrEmpty(x.Name))
                {
                    GroupSelectionProcesses_ComboBox.Items.Add(x.Name);
                    GroupSelectionPoints_ComboBox.Items.Add(x.Name);
                }
            });
        }
        #endregion

        #region Regenerate Data Grid
        private void regenerateDataGridViews()
        {
            List<UIMollierPoint> uIMollierPoints = mollierModel.GetMollierObjects<UIMollierPoint>();
            List<UIMollierProcess> uIMollierProcesses = mollierModel.GetMollierObjects<UIMollierProcess>();
            List<MollierGroup> mollierGroups = mollierModel.GetMollierObjects<MollierGroup>();

            regenerateDataGridView_Points(uIMollierPoints, mollierGroups);
            regenerateDataGridView_Processes(uIMollierProcesses, mollierGroups); 

        }
        private void regenerateDataGridView_Points(List<UIMollierPoint> mollierPoints, List<MollierGroup> mollierGroups)
        {
            if(mollierPoints == null)
            {
                return;
            }
            string actualGroup = (string)GroupSelectionPoints_ComboBox?.SelectedItem;
            List<DisplayUIMollierObject> dataGridViewElements = new List<DisplayUIMollierObject>();

            foreach (UIMollierPoint uIMollierPoint in mollierPoints)
            {
                string name = Query.GroupName(uIMollierPoint, mollierGroups);
                if (actualGroup != defaultGroup && name != actualGroup)
                {
                    continue;
                }
                dataGridViewElements.Add(new DisplayUIMollierObject(uIMollierPoint));
            }

            DataGridView_MollierPoints.DataSource = dataGridViewElements;
        }
        private void regenerateDataGridView_Processes(List<UIMollierProcess> mollierProcesses, List<MollierGroup> mollierGroups)
        {
            if (mollierProcesses == null)
            {
                return;
            }

            mollierProcesses = mollierProcesses.SortByGroup().ConvertAll(x => (UIMollierProcess)x);
            string actualGroup = (string)GroupSelectionProcesses_ComboBox?.SelectedItem;
            List<DisplayUIMollierObject> dataGridViewElements = new List<DisplayUIMollierObject>();

            foreach (UIMollierProcess uIMollierProcess in mollierProcesses)
            {
                string name = Query.GroupName(uIMollierProcess, mollierGroups);
                if (actualGroup != defaultGroup && name != actualGroup)
                {
                    continue;
                }
                dataGridViewElements.Add(new DisplayUIMollierObject(uIMollierProcess, 0, airflow, airFlowUnit));
                dataGridViewElements.Add(new DisplayUIMollierObject(uIMollierProcess, 1 , airflow, airFlowUnit));
            }

            DataGridView_MollierProcesses.DataSource = dataGridViewElements;
        }
        #endregion 

        #region Air Flow Selection
        private void SupplyAirFlow_TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!Core.Query.TryConvert(SupplyAirFlow_TextBox.Text, out double supplyAirFlow))
            {
                return;
            }

            airflow = supplyAirFlow;
            regenerateDataGridViews();
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
        #endregion

        #region Group Selection
        private void GroupSelectionPoints_ComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            List<UIMollierPoint> uIMollierPoints = mollierModel?.GetMollierObjects<UIMollierPoint>();
            List<MollierGroup> mollierGroups = mollierModel?.GetMollierObjects<MollierGroup>();

            if (mollierModel == null || uIMollierPoints == null)
            {
                return;
            }
            string selectedGroup = GroupSelectionPoints_ComboBox.SelectedItem.ToString();

            // Change visibility of points from different groups
            foreach(UIMollierPoint uIMollierPoint in uIMollierPoints)
            {
                string groupName = Query.GroupName(uIMollierPoint, mollierGroups);
                if(selectedGroup == defaultGroup || selectedGroup == groupName)
                {
                    uIMollierPoint.UIMollierAppearance.Visible = true;
                }
                else
                {
                    uIMollierPoint.UIMollierAppearance.Visible = false;
                }
            }
            editObject();

            regenerateDataGridView_Points(uIMollierPoints, mollierGroups);
        }
        private void GroupSelectionProcesses_ComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            List<UIMollierProcess> uIMollierProcesses = mollierModel?.GetMollierObjects<UIMollierProcess>();
            List<MollierGroup> mollierGroups = mollierModel?.GetMollierObjects<MollierGroup>();

            if (mollierModel == null || uIMollierProcesses == null)
            {
                return;
            }
            string selectedGroup = GroupSelectionProcesses_ComboBox.SelectedItem.ToString();

            // Change visibility of processes from different groups
            foreach (UIMollierProcess uIMollierProcess in uIMollierProcesses)
            {
                string groupName = Query.GroupName(uIMollierProcess, mollierGroups);
                if (selectedGroup == defaultGroup || selectedGroup == groupName)
                {
                    uIMollierProcess.UIMollierAppearance.Visible = true;
                }
                else
                {
                    uIMollierProcess.UIMollierAppearance.Visible = false;
                }
            }

            editObject();
            regenerateDataGridView_Processes(uIMollierProcesses, mollierGroups);
        }   
        #endregion

        #region Object Edited
        private void ToolStripMenuItem_Edit_Click(object sender, EventArgs e)
        {
            DataGridViewRow dataGridViewRow = selectedRows()?.FirstOrDefault();
            if (dataGridViewRow == null)
            {
                return;
            }

            DisplayUIMollierObject displayUIMollierObject = dataGridViewRow?.DataBoundItem as DisplayUIMollierObject;

            displayUIMollierObject.UIMollierObject.Update();
            editObject();
        }
        private void editObject()
        {
            MollierModelEditedEventArgs mollierModelEditedEventArgs = new MollierModelEditedEventArgs(mollierModel);
            MollierModelEdited.Invoke(this, mollierModelEditedEventArgs);
            regenerateDataGridViews();
        }
        #endregion

        #region Object Removed
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
            regenerateDataGridViews();
        }
        #endregion

        #region Cells Selection
        private void DataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // TODO: refactoring here
            DataGridView dataGridView = sender as DataGridView;
            if(dataGridView == null || e.RowIndex < 0)
            {
                return;
            }
            DataGridViewButtonColumn dataGridViewButtonColumn = dataGridView.Columns[e.ColumnIndex] as DataGridViewButtonColumn;
            DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn = dataGridView.Columns[e.ColumnIndex] as DataGridViewCheckBoxColumn;
            DisplayUIMollierObject displayUIMollierObject = dataGridView?.Rows[e.RowIndex]?.DataBoundItem as DisplayUIMollierObject;

            if (dataGridViewButtonColumn == null)
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
                editObject();

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
                displayUIMollierObject.UIMollierObject.Update();
                editObject();
            }
        }
        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            DataGridView dataGridView = (DataGridView)sender;
            if (dataGridView.SelectedRows == null || dataGridView.SelectedRows.Count == 0)
            {
                return;
            }

            DataGridViewRow row = dataGridView.SelectedRows[0];
            DisplayUIMollierObject displayUIMollierObject = row?.DataBoundItem as DisplayUIMollierObject;

            if (displayUIMollierObject.UIMollierObject == null)
            {
                return;
            }

            MollierObjectSelectedArgs mollierObjectSelectedArgs = new MollierObjectSelectedArgs(displayUIMollierObject.UIMollierObject);
            MollierObjectSelected?.Invoke(this, mollierObjectSelectedArgs);
        }
        #endregion
        
        private List<DataGridViewRow> selectedRows()
        {
            TabPage tabPage = customizeMollierObjectsTabControl.SelectedTab;
            return (tabPage.Controls.Cast<Control>().ToList().Find(x => x is DataGridView) as DataGridView)?.SelectedRows?.Cast<DataGridViewRow>().ToList();
        }

    }
}
