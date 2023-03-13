using System;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static void Reload<T>(this ComboBox comboBox, bool includeUndefined = false) where T: Enum
        {
            Reload(comboBox, typeof(T), includeUndefined);
        }

        public static void Reload(this ComboBox comboBox, Type type, bool includeUndefined = false)
        {
            if (comboBox == null || type == null || !type.IsEnum)
            {
                return;
            }

            object @object = comboBox.SelectedItem;
            comboBox.Items.Clear();

            foreach (Enum @enum in Enum.GetValues(type))
            {
                if (!includeUndefined && @enum.ToString() == "Undefined")
                {
                    continue;
                }

                comboBox.Items.Add(Core.Query.Description(@enum));
            }

            if (@object != null)
            {
                comboBox.SelectedItem = @object;
            }
        }
    }
}