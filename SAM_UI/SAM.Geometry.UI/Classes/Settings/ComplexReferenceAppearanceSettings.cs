using System.Text.Json.Nodes;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public class ComplexReferenceAppearanceSettings : RelationClusterAppearanceSettings
    {
        public IComplexReference ComplexReference { get; set; }

        public ComplexReferenceAppearanceSettings()
            :base()
        {

        }
        
        public ComplexReferenceAppearanceSettings(ComplexReferenceAppearanceSettings complexReferenceAppearanceSettings)
            :base(complexReferenceAppearanceSettings)
        {
            if(complexReferenceAppearanceSettings != null)
            {
                ComplexReference = complexReferenceAppearanceSettings.ComplexReference;
            }
        }

        public ComplexReferenceAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {

        }

        public override bool FromJsonObject(JsonObject jObject)
        {
            if(!base.FromJsonObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("ComplexReference"))
            {
                ComplexReference = Core.Query.IJSAMObject<IComplexReference>(jObject["ComplexReference"] as JsonObject);
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if(result == null)
            {
                return null;
            }

            if(ComplexReference != null)
            {
                result.Add("ComplexReference", ComplexReference.ToJsonObject());
            }

            return result;
        }

        public override bool TryGetValue<T>(IJSAMObject sAMObject, out T value)
        {
            value = default(T);

            IComplexReference complexReference = ComplexReference;
            if (complexReference == null)
            {
                return false;
            }

            RelationCluster relationCluster = RelationCluster;
            if(relationCluster == null)
            {
                return false;
            }

            if(!relationCluster.TryGetValues(sAMObject, ComplexReference, out List<object> values) || values == null || values.Count == 0)
            {
                return false;
            }

            int index = values.FindIndex(x => x is T);
            if (index != -1)
            {
                value = (T)values[index];
                return true;
            }

            foreach (object @object in values)
            {
                if(Core.Query.TryConvert(@object, out value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
