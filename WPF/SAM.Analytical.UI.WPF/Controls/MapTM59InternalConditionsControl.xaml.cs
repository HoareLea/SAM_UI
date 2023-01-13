using SAM.Core;
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

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MapTM59InternalConditionsControl.xaml
    /// </summary>
    public partial class MapTM59InternalConditionsControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

        public MapTM59InternalConditionsControl()
        {
            InitializeComponent();

            Load();
        }

        public MapTM59InternalConditionsControl(IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster, TextMap textMap = null, InternalConditionLibrary internalConditionLibrary = null)
        {
            InitializeComponent();

            this.adjacencyCluster = adjacencyCluster;

            mapInternalConditionsControl.TextMap = textMap;
            mapInternalConditionsControl.InternalConditionLibrary = internalConditionLibrary;

            mapInternalConditionsControl.Spaces = spaces?.ToList();

            Load();
        }

        private void Load()
        {
            LoadZones();
            SetMapFunc();
        }

        private void LoadZones()
        {
            string value = comboBox_ZoneType.Text;

            comboBox_ZoneType.Items.Clear();

            List<Zone> zones = adjacencyCluster?.GetZones();
            if(zones == null || zones.Count == 0)
            {
                return;
            }

            HashSet<string> categories = new HashSet<string>();
            foreach(Zone zone in zones)
            {
                if(zone.TryGetValue(ZoneParameter.ZoneCategory, out string category) && !string.IsNullOrWhiteSpace(category))
                {
                    categories.Add(category);
                }
            }

            foreach(string category in categories)
            {
                comboBox_ZoneType.Items.Add(category);
            }

            if(!string.IsNullOrWhiteSpace(value))
            {
                comboBox_ZoneType.Text = value;
            }

        }

        public TextMap TextMap
        {
            get
            {
                return mapInternalConditionsControl.TextMap;
            }

            set
            {
                mapInternalConditionsControl.TextMap = value;
                SetMapFunc();
            }
        }

        public InternalConditionLibrary InternalConditionLibrary
        {
            get
            {
                return mapInternalConditionsControl.InternalConditionLibrary;
            }

            set
            {
                mapInternalConditionsControl.InternalConditionLibrary = value;
                SetMapFunc();
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return mapInternalConditionsControl.Spaces;
            }

            set
            {
                mapInternalConditionsControl.Spaces = value;
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }

            set
            {
                SetAdjacencyCluster(value);
            }
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
            Load();
        }

        private void comboBox_ZoneType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Space> spaces = Spaces;

            mapInternalConditionsControl.GroupFunc = new Func<Space, string>(x => 
            {
                Zone zone = adjacencyCluster?.GetZones(x, comboBox_ZoneType.SelectedItem as string)?.FirstOrDefault();
                if(zone == null)
                {
                    return null;
                }

                return zone.Name;
            });

            SetMapFunc();
        }

        private static bool IsSleeping(Space space, TextMap textMap)
        {
            return Is(space, textMap, "Sleeping");
        }

        private static bool IsLiving(Space space, TextMap textMap)
        {
            return Is(space, textMap, "Living");
        }

        private static bool IsCooking(Space space, TextMap textMap)
        {
            return Is(space, textMap, "Cooking");
        }

        private enum Application
        {
            Sleeping,
            Living,
            Cooking
        }

        private static List<Application> Applications(Space space, TextMap textMap)
        {
            if(space == null || textMap == null)
            {
                return null;
            }

            List<Application> result = new List<Application>();
            if(IsSleeping(space, textMap))
            {
                result.Add(Application.Sleeping);
            }

            if (IsLiving(space, textMap))
            {
                result.Add(Application.Living);
            }

            if (IsCooking(space, textMap))
            {
                result.Add(Application.Cooking);
            }

            return result;

        }

        private static bool Is(Space space, TextMap textMap, string key)
        {
            if (space == null || textMap == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            HashSet<string> values = textMap.GetSortedKeys(space.Name);
            return values != null && values.Contains(key);
        }

        private static int Count(Space space, TextMap textMap)
        {
            if (space == null || textMap == null)
            {
                return 0;
            }

            HashSet<string> values = textMap.GetSortedKeys(space.Name);
            foreach(string value in values)
            {
                if(Core.Query.TryConvert<int>(value, out int result))
                {
                    return result;
                }
            }

            return 0;
        }

        public void SetMapFunc()
        {
            TextMap textMap = TextMap;
            InternalConditionLibrary internalConditionLibrary = InternalConditionLibrary;
            string zoneType = comboBox_ZoneType.SelectedItem as string;

            mapInternalConditionsControl.MapFunc = new Func<Space, InternalCondition>(x => 
            {
                Zone zone = adjacencyCluster?.GetZones(x, zoneType)?.FirstOrDefault();
                if(zone == null)
                {
                    return null;
                }

                List<Space> spaces = adjacencyCluster.GetSpaces(zone);
                if(spaces == null || spaces.Count == 0)
                {
                    return null;
                }

                List<Application> applications = Applications(x, textMap);
                if (applications.Contains(Application.Sleeping) && applications.Contains(Application.Cooking) && applications.Contains(Application.Living))
                {
                    return internalConditionLibrary.GetInternalConditions("Studio").FirstOrDefault();
                }

                int count = 0;

                if(applications.Contains(Application.Sleeping))
                {
                    count = Count(x, textMap);
                    count = count == 0 ? 1 : count;

                    switch(count)
                    {
                        case 1:
                            return internalConditionLibrary.GetInternalConditions("Single Bedroom").FirstOrDefault();

                        case 2:
                            return internalConditionLibrary.GetInternalConditions("Double Bedroom").FirstOrDefault();
                    }

                    return null;
                }

                List<Space> spaces_Sleeping = spaces.FindAll(y => IsSleeping(y, textMap));

                count = spaces_Sleeping.ConvertAll(y => Count(y, textMap)).Sum();
                count = count == 0 ? 1 : count;
                count = count > 3 ? 3 : count;

                string name = null;
                if (applications.Contains(Application.Cooking) && applications.Contains(Application.Living))
                {
                    name = "Bed Apt. Living Room/Kitchen";
                }
                else if(applications.Contains(Application.Cooking))
                {
                    name = "Bed Apt. Kitchen";
                }
                else if(applications.Contains(Application.Living))
                {
                    name = "Bed Apt. Living Room";
                }
                else
                {
                    return null;
                }

                name = string.Format("{0} {1}", count, name);
                return internalConditionLibrary.GetInternalConditions(name).FirstOrDefault();
            });
        }

        public List<Space> GetSpaces(bool selected = false)
        {
            return mapInternalConditionsControl.GetSpaces(selected);
        }
    }
}
