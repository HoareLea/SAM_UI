// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The outcome of one <see cref="UValueEvaluationRequest"/>. Thicknesses are in metres, U-values in W/m²K;
    /// a value that was not calculated is NaN.
    /// </summary>
    public sealed class UValueEvaluation
    {
        public UValueEvaluation(UValueEvaluationRequest request, UValueCalculationFailure failure, string message, int layerIndex, double initialThermalTransmittance, double thickness, double calculatedThermalTransmittance, double minThicknessThermalTransmittance, double maxThicknessThermalTransmittance, long elapsedMilliseconds)
        {
            Request = request;
            Failure = failure;
            Message = message;
            LayerIndex = layerIndex;
            InitialThermalTransmittance = initialThermalTransmittance;
            Thickness = thickness;
            CalculatedThermalTransmittance = calculatedThermalTransmittance;
            MinThicknessThermalTransmittance = minThicknessThermalTransmittance;
            MaxThicknessThermalTransmittance = maxThicknessThermalTransmittance;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        public UValueEvaluationRequest Request { get; }

        /// <summary>The PR1 classification (<c>Query.UValueCalculationFailure</c>); <see cref="UValueCalculationFailure.None"/> on success.</summary>
        public UValueCalculationFailure Failure { get; }

        /// <summary>The user-facing reason when <see cref="Failure"/> is not None (PR1 wording, plus the best achievable U when unreachable).</summary>
        public string Message { get; }

        /// <summary>The layer that was (or would be) varied; -1 when none.</summary>
        public int LayerIndex { get; }

        /// <summary>U-value of the construction as it is.</summary>
        public double InitialThermalTransmittance { get; }

        /// <summary>Calculated thickness of the varied layer.</summary>
        public double Thickness { get; }

        /// <summary>U-value achieved with <see cref="Thickness"/>.</summary>
        public double CalculatedThermalTransmittance { get; }

        /// <summary>U-value with the layer at the minimum thickness (the highest reachable U).</summary>
        public double MinThicknessThermalTransmittance { get; }

        /// <summary>U-value with the layer at the maximum thickness (the lowest reachable U).</summary>
        public double MaxThicknessThermalTransmittance { get; }

        /// <summary>Wall-clock time of the evaluation on the worker.</summary>
        public long ElapsedMilliseconds { get; }

        /// <summary>True when the target was reached within tolerance with a real thickness.</summary>
        public bool Reached => Failure == UValueCalculationFailure.None && !double.IsNaN(Thickness) && !double.IsNaN(CalculatedThermalTransmittance);

        /// <summary>
        /// The closest U-value the range allows for an unreachable target: the U at the maximum thickness when
        /// the target is below it, the U at the minimum thickness when the target is above it; NaN otherwise.
        /// </summary>
        public double BestAchievableThermalTransmittance
        {
            get
            {
                double target = Request?.TargetThermalTransmittance ?? double.NaN;
                if (double.IsNaN(target))
                {
                    return double.NaN;
                }

                if (!double.IsNaN(MaxThicknessThermalTransmittance) && target < MaxThicknessThermalTransmittance)
                {
                    return MaxThicknessThermalTransmittance;
                }

                if (!double.IsNaN(MinThicknessThermalTransmittance) && target > MinThicknessThermalTransmittance)
                {
                    return MinThicknessThermalTransmittance;
                }

                return double.NaN;
            }
        }

        /// <summary>The thickness at which <see cref="BestAchievableThermalTransmittance"/> is achieved [m]; NaN when that is NaN.</summary>
        public double BestAchievableThickness
        {
            get
            {
                double best = BestAchievableThermalTransmittance;
                if (double.IsNaN(best) || Request == null)
                {
                    return double.NaN;
                }

                return best == MaxThicknessThermalTransmittance ? Request.MaxThickness : Request.MinThickness;
            }
        }
    }
}
