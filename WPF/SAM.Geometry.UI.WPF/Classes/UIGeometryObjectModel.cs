using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI.WPF
{
    public class UIGeometryObjectModel : Core.UI.UIJSAMObject<GeometryObjectModel>
    {
        public UIGeometryObjectModel(string path)
       : base(path)
        {

        }

        public UIGeometryObjectModel(GeometryObjectModel geometryObjectModel)
            : base(geometryObjectModel)
        {

        }

        public UIGeometryObjectModel()
            : base()
        {

        }

        public override bool Open()
        {
            OnOpening();

            bool result = false;
            if (!string.IsNullOrEmpty(Path) && System.IO.File.Exists(Path))
            {
                List<IJSAMObject> jSAMObjects = null;
                try
                {
                    jSAMObjects = Core.Convert.ToSAM<IJSAMObject>(Path);
                }
                catch (Exception exception)
                {
                    return false;
                }



                if (jSAMObjects != null && jSAMObjects.Count != 0)
                {
                    GeometryObjectModel geometryObjectModel_Result = jSAMObjects.Find(x => x is GeometryObjectModel) as GeometryObjectModel;
                    if (geometryObjectModel_Result == null)
                    {
                        geometryObjectModel_Result = new GeometryObjectModel();
                    }
                    else
                    {
                        jSAMObjects.Remove(geometryObjectModel_Result);
                    }

                    List<ISAMGeometryObject> sAMGeometryObjects = jSAMObjects.FindAll(x => x is ISAMGeometryObject).ConvertAll(x => (ISAMGeometryObject)x);
                    if (sAMGeometryObjects != null)
                    {
                        sAMGeometryObjects.ForEach(x => geometryObjectModel_Result.Add(x));
                    }

                    List<GeometryObjectModel> geometryObjectModels = jSAMObjects.FindAll(x => x is GeometryObjectModel).ConvertAll(x => (GeometryObjectModel)x);
                    if(geometryObjectModels != null && geometryObjectModels.Count != 0)
                    {
                        foreach(GeometryObjectModel geometryObjectModel in geometryObjectModels)
                        {
                            sAMGeometryObjects = geometryObjectModel.GetSAMGeometryObjects<ISAMGeometryObject>();
                            if(sAMGeometryObjects != null)
                            {
                                sAMGeometryObjects.ForEach(x => geometryObjectModel_Result.Add(x));
                            }
                        }
                    }

                    List<ISAMGeometry> sAMGeometries = jSAMObjects.FindAll(x => x is ISAMGeometry).ConvertAll(x => (ISAMGeometry)x);
                    if(sAMGeometries != null && sAMGeometries.Count != 0)
                    {
                        foreach(ISAMGeometry sAMGeometry in sAMGeometries)
                        {
                            ISAMGeometryObject sAMGeometryObject = UI.Convert.ToSAM_ISAMGeometryObject(sAMGeometry);
                            if(sAMGeometryObject == null)
                            {
                                continue;
                            }

                            geometryObjectModel_Result.Add(sAMGeometryObject);
                        }
                    }

                    jSAMObject = geometryObjectModel_Result;
                    result = jSAMObject != null;
                }
            }

            if (result)
            {
                OnOpened();
                modified = false;
            }

            return result;
        }
    }
}
