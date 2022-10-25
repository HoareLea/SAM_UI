namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        /// <summary>
        /// Convert Drawing Color (WPF) to Media Color (WinForm)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static System.Windows.Media.Color ToMedia(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
