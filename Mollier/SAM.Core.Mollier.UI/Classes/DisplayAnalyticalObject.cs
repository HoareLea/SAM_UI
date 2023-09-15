using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public class DisplayUIMollierObject
    {
        public IUIMollierObject UIMollierObject { get; }
        public int Index { get; }
        private double airFlow;
        private Units.UnitType airFlowUnit;

        public DisplayUIMollierObject(UIMollierPoint uIMollierPoint)
        {
            UIMollierObject = uIMollierPoint;
        }

        public DisplayUIMollierObject(UIMollierProcess uIMollierProcess, int index, double airFlow, Units.UnitType airFlowUnit)
        {
            UIMollierObject = uIMollierProcess;
            this.airFlow = airFlow;
            this.airFlowUnit = airFlowUnit;
            Index = index;
        }
        public bool Visible
        { 
            get
            {
                return UIMollierObject.UIMollierAppearance.Visible;
            }
        }
        public string Name
        { 
            get
            {
                if(UIMollierObject is UIMollierProcess && Index == 0)
                {
                    return ((UIMollierProcess)UIMollierObject)?.MollierProcess?.FullProcessName();
                }
                return default;
            }
        }

        public string Label
        {
            get
            {
                if(UIMollierObject is UIMollierProcess)
                {
                    if(Index == 0)
                    {
                        return ((UIMollierProcess)UIMollierObject)?.UIMollierAppearance_Start?.Label;
                    }
                    else
                    {
                        return ((UIMollierProcess)UIMollierObject)?.UIMollierAppearance_End?.Label;
                    }
                }
                if(UIMollierObject is UIMollierPoint)
                {
                    return ((UIMollierPoint)UIMollierObject)?.UIMollierAppearance?.Label;
                }
                return default;
            }
        }

        public double? DryBulbTemperature
        { 
            get
            {
                if(UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].DryBulbTemperature, 0.01);
                }
                if(UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).DryBulbTemperature, 0.01);
                }
                return default;
            }
        }
        public double? HumidityRatio
        {
            get 
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].HumidityRatio * 1000, 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).HumidityRatio * 1000, 0.01);
                }
                return default;
            }
        }
        public double? RelativeHumidity
        {
            get
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].RelativeHumidity, 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).RelativeHumidity, 0.01);
                }
                return default;
            }
        }
        public double? Enthalpy
        {
            get
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].Enthalpy / 1000, 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).Enthalpy / 1000, 0.01);
                }
                return default;
            }
        }
        public double? Density
        {
            get
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].Density(), 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).Density(), 0.01);
                }
                return default;
            }
        }
        public double? SpecificVolume
        {
            get
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].SpecificVolume(), 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).SpecificVolume(), 0.01);
                }
                return default;
            }
        }
        public double? WetBulbTemperature
        {
            get
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].WetBulbTemperature(), 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).WetBulbTemperature(), 0.01);
                }
                return default;
            }
        }
        public double? DewPointTemperature
        {
            get
            {
                if (UIMollierObject is UIMollierProcess)
                {
                    return Core.Query.Round(((UIMollierProcess)UIMollierObject).MollierPoints[Index].DewPointTemperature(), 0.01);
                }
                if (UIMollierObject is UIMollierPoint)
                {
                    return Core.Query.Round(((UIMollierPoint)UIMollierObject).WetBulbTemperature(), 0.01);
                }
                return default;
            }
        }

        public double? MassFlow
        {
            get
            {
                if (UIMollierObject is UIMollierProcess && Index == 1)
                {
                    return Core.Query.Round(Mollier.Query.MassFlow(airFlow, ((UIMollierProcess)UIMollierObject).MollierPoints[Index].Density()), 0.01); 
                }   
                return default; 
            }
        }
        public double? TotalLoad
        {
            get
            {
                if (UIMollierObject is UIMollierProcess && Index == 1)
                {
                    UIMollierProcess uIMollierProcess = (UIMollierProcess)UIMollierObject;
                    double enthalpyDifference = uIMollierProcess.Start.Enthalpy - uIMollierProcess.End.Enthalpy;
                    return Core.Query.Round(Mollier.Query.TotalLoad(uIMollierProcess.End, enthalpyDifference, airFlow) / 1000, 0.01);
                }
                return default;
            }
        }
        public double? SensibleLoad
        {
            get
            {
                if (UIMollierObject is UIMollierProcess && Index == 1)
                {
                    UIMollierProcess uIMollierProcess = (UIMollierProcess)UIMollierObject;
                    double temperatureDifference = uIMollierProcess.Start.DryBulbTemperature - uIMollierProcess.End.DryBulbTemperature;
                    return Core.Query.Round(Mollier.Query.SensibleLoad(uIMollierProcess.End, temperatureDifference, airFlow) / 1000, 0.01);
                }
                return default;
            }
        }
        public double? LatentLoad
        {
            get
            {
                if (UIMollierObject is UIMollierProcess && Index == 1)
                {
                    UIMollierProcess uIMollierProcess = (UIMollierProcess)UIMollierObject;
                    double humidityRatioDifference = uIMollierProcess.Start.HumidityRatio - uIMollierProcess.End.HumidityRatio;
                    return Core.Query.Round(Mollier.Query.LatentLoad(uIMollierProcess.End, humidityRatioDifference, airFlow) / 1000, 0.01);
                }
                return default;
            }
        }

    }
}
