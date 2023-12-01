using SAM.Core;

namespace SAM.Analytical.UI.WPF
{
    public class ThermalTransmittanceFilter
    {
        public double LightTransmittance = double.NaN;
        public Range<double> LightTransmittanceRange = new Range<double>(-0.2, 0.0);

        public double TotalSolarEnergyTransmittance = double.NaN;
        public Range<double> TotalSolarEnergyTransmittanceRange = new Range<double>(-0.2, 0.0);

        public double ThermalTransmittance = double.NaN;
        public Range<double> ThermalTransmittanceRange = new Range<double>(-0.2, 0.0);

        public ThermalTransmittanceFilter()
        {

        }

        public double ThermalTransmittance_Min
        {
            get
            {
                return GetValue_Min(ThermalTransmittance, ThermalTransmittanceRange);
            }
        }

        public double ThermalTransmittance_Max
        {
            get
            {
                return GetValue_Max(ThermalTransmittance, ThermalTransmittanceRange);
            }
        }

        public double LightTransmittance_Min
        {
            get
            {
                return GetValue_Min(LightTransmittance, LightTransmittanceRange);
            }
        }

        public double LightTransmittance_Max
        {
            get
            {
                return GetValue_Max(LightTransmittance, LightTransmittanceRange);
            }
        }

        public double TotalSolarEnergyTransmittance_Min
        {
            get
            {
                return GetValue_Min(TotalSolarEnergyTransmittance, TotalSolarEnergyTransmittanceRange);
            }
        }

        public double TotalSolarEnergyTransmittance_Max
        {
            get
            {
                return GetValue_Max(TotalSolarEnergyTransmittance, TotalSolarEnergyTransmittanceRange);
            }
        }

        private static double GetValue_Min(double value, Range<double> range)
        {
            if(double.IsNaN(value))
            {
                return double.NaN;
            }

            if(range == null || double.IsNaN(range.Min))
            {
                return value;
            }

            return value + range.Min;
        }

        private static double GetValue_Max(double value, Range<double> range)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            if (range == null || double.IsNaN(range.Max))
            {
                return value;
            }

            return value + range.Max;
        }
    }
}