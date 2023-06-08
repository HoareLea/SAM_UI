namespace SAM.Core.Mollier.UI
{
    public interface IMollierProcessControl
    {
        UIMollierProcess GetUIMollierProcess();

        MollierForm MollierForm { get; set; }
    }
}
