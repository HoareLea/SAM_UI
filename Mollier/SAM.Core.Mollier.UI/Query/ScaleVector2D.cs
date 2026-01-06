// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{

    public static partial class Query
    {
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            HORZRES = 11,
            DESKTOPVERTRES = 117,
            DESKTOPHORZRES = 118,
        }
        /// <summary>
        /// Calculates the axis scales from the initial, default values
        /// </summary>
        /// <param name="control">Control</param>
        /// <param name="mollierControlSettings">Mollier Control Settings</param>
        /// <returns>Returns Vector which contains the scale</returns>
        public static Geometry.Planar.Vector2D ScaleVector2D(Control control, MollierControlSettings mollierControlSettings)
        {
            if(control == null)
            {
                return new Geometry.Planar.Vector2D(1, 1);
            }

            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            int PhysicalScrenWidth = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPHORZRES);
            int LocalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int LocalScreenWidth = GetDeviceCaps(desktop, (int)DeviceCap.HORZRES);

            double defaultScalingFactor = 1.5;
            double screenScalingFactor = (double)PhysicalScreenHeight / (double)LocalScreenHeight;
            double scaling = screenScalingFactor / defaultScalingFactor;
            
            Rectangle defaultResolution = new Rectangle(0, 0, 3840, 2160);
            Rectangle screenResolution = new Rectangle(0, 0, PhysicalScrenWidth, PhysicalScreenHeight);

            Form form = control.FindForm();
            if(form == null)
            {
                return new Geometry.Planar.Vector2D(1, 1);
            }


            double screenWidth = Screen.GetWorkingArea(form).Width;
            double screenHeight = Screen.GetWorkingArea(form).Height;

            double widthFactor = (screenWidth / (double)form.ClientSize.Width);
            double heightFactor = (screenHeight / (double)form.ClientSize.Height);

            widthFactor *= ((double)defaultResolution.Width / screenResolution.Width);
            heightFactor *= ((double)defaultResolution.Height / screenResolution.Height);

            //var defaultDPI = 96;
            //var programScale = 1.5;
            //var currentDPI = (int)Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop", "LogPixels", 96);
            //var currentScale = (double)currentDPI / defaultDPI;
            //var scaling = (double)currentScale / programScale;


            widthFactor *= scaling;
            heightFactor *= scaling;


            MollierControlSettings mollierControlSettings_Default = new MollierControlSettings();

            double humidityRatioProportion = (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) / (mollierControlSettings_Default.HumidityRatio_Max - mollierControlSettings_Default.HumidityRatio_Min);
            double temperatureProportion = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings_Default.Temperature_Max - mollierControlSettings_Default.Temperature_Min);

            double xFactor_Temp = humidityRatioProportion;
            double yFactor_Temp = temperatureProportion;
            if (mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                double temp = xFactor_Temp;
                xFactor_Temp = yFactor_Temp;
                yFactor_Temp = temp;
            }

            xFactor_Temp *= widthFactor;
            yFactor_Temp *= heightFactor;
            
            if(control.DeviceDpi == 120)
            {
                Math.LinearEquation linearEquation = Math.Create.LinearEquation(96.0, 1.0, 120.0, 1.2);

                double constant =  linearEquation.Evaluate(control.DeviceDpi);
                xFactor_Temp *= constant;
                yFactor_Temp *= constant;
            }

            return new Geometry.Planar.Vector2D(xFactor_Temp, yFactor_Temp);
        }
    }
}
