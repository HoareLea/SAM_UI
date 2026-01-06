// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Saves short process name value
        /// </summary>
        /// <param name="mollierProcess">Mollier process</param>
        /// <returns>Process name</returns>
        public static string ProcessName(IMollierProcess mollierProcess)
        {
            if(mollierProcess is UIMollierProcess)
            {
                mollierProcess = ((UIMollierProcess)mollierProcess).MollierProcess;
            }
            string processName = "";
            if (mollierProcess is HeatingProcess)
            {
                processName = "HTG";
            }
            if(mollierProcess is FanProcess)
            {
                processName = "FAN";
            }
            if (mollierProcess is CoolingProcess)
            {
                processName = "CLG";
            }
            if (mollierProcess is MixingProcess)
            {
                processName = "MX";
            }
            if (mollierProcess is SteamHumidificationProcess || mollierProcess is AdiabaticHumidificationProcess || mollierProcess is IsothermalHumidificationProcess)
            {
                processName = "HUM";
            }
            if (mollierProcess is HeatRecoveryProcess)
            {
                processName = "HR";
            }
            if(mollierProcess is RoomProcess)
            {
                processName = "ROOM";
            }
            return processName;
        }
    }
}
