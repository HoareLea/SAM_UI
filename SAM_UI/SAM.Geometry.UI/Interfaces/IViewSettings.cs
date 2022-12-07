using System;

namespace SAM.Geometry.UI
{
    public interface IViewSettings : Core.IJSAMObject
    {
        Guid Guid { get; }
        string Name { get; }
    }
}
