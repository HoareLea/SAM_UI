using SAM.Analytical.Mollier;
using SAM.Core.Mollier;
using SAM.Core.Mollier.UI;
using SAM.Core.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static void AirHandlingUnitDiagram(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            AirHandlingUnit airHandlingUnit = null;

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster.GetObjects<AirHandlingUnit>()?.FindAll(x => x != null);
            if(airHandlingUnits == null || airHandlingUnits.Count == 0)
            {
                MessageBox.Show("Cound not find any Air Handling Unit");
                return;
            }

            airHandlingUnits.Sort((x, y) => x.Name.CompareTo(y.Name));
            using (ComboBoxForm<AirHandlingUnit> comboBoxForm = new ComboBoxForm<AirHandlingUnit>("Air Handling Units", airHandlingUnits, (AirHandlingUnit x) => x?.Name))
            {
                comboBoxForm.SelectedItem = airHandlingUnits.FirstOrDefault();

                if (comboBoxForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                airHandlingUnit = comboBoxForm.SelectedItem;
            }

            if(airHandlingUnit == null)
            {
                return;
            }

            MollierGroup mollierGroup = null;

            AirHandlingUnitResult airHandlingUnitResult = Mollier.Create.AirHandlingUnitResult(analyticalModel, airHandlingUnit.Name);
            if (airHandlingUnitResult != null)
            {
                adjacencyCluster.GetObjects<AirHandlingUnitResult>()?.FindAll(x => x.Name == airHandlingUnit.Name).ForEach(x => adjacencyCluster.RemoveObject<AirHandlingUnitResult>(x.Guid));
                adjacencyCluster.AddObject(airHandlingUnitResult);
                adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitResult);
            }

            airHandlingUnitResult = adjacencyCluster?.GetObjects<AirHandlingUnitResult>()?.Find(x => x.Name == airHandlingUnit.Name);
            if (airHandlingUnitResult != null)
            {
                mollierGroup = airHandlingUnitResult.GetValue<MollierGroup>(AirHandlingUnitResultParameter.Processes);
            }

            List<IMollierProcess> mollierProcesses = mollierGroup?.GetObjects<IMollierProcess>();

            double pressure = Core.Mollier.UI.Query.DefaultPressure(null, mollierProcesses);

            using (MollierForm mollierForm = new MollierForm() { ReadOnly = true, WindowState = FormWindowState.Normal })
            {
                mollierForm.Name = string.IsNullOrWhiteSpace(airHandlingUnit.Name) ? mollierForm.Name : airHandlingUnit.Name;
                mollierForm.MollierControlSettings = Core.Mollier.UI.Query.DefaultMollierControlSettings();
                mollierForm.Pressure = pressure;
                //mollierForm.AddProcesses(mollierProcesses, false);
                mollierForm.AddMollierObjects(mollierProcesses, false);

                if (mollierForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
            }

            return;
        }
    }
}