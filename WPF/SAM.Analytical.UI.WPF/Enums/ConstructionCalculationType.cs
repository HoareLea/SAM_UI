using System.ComponentModel;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Construction Calculation Type
    /// </summary>
    [Description("Construction Calculation Type")]
    public enum ConstructionCalculationType
    {
        /// <summary>
        /// Undefined
        /// </summary>
        [Description("Undefined")] Undefined,

        /// <summary>
        /// Layer Thickness
        /// </summary>
        [Description("Layer Thickness")] LayerThickness,

        /// <summary>
        /// Construction
        /// </summary>
        [Description("Constrcution")] Constrcution,
    }
}
