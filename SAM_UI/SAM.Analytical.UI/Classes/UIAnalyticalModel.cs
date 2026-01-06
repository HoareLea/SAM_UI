namespace SAM.Analytical.UI
{
    public class UIAnalyticalModel : Core.UI.UIJSAMObject<AnalyticalModel>
    {
        public UIAnalyticalModel(string path)
            : base(path)
        {

        }

        public UIAnalyticalModel(AnalyticalModel analyticalModel)
            : base(analyticalModel)
        {

        }

        public UIAnalyticalModel()
            : base()
        {

        }
    }
}
