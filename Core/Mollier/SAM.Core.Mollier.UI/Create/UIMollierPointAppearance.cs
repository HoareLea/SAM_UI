using SAM.Geometry.Mollier;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static UIMollierPointAppearance UIMollierPointAppearance(this DisplayPointType displayPointType, string label = null)
        {
            if(displayPointType == DisplayPointType.Undefined)
            {
                return null;
            }

            switch (displayPointType)
            {
                case DisplayPointType.Undefined:
                    return null;

                case DisplayPointType.Default:
                    return new UIMollierPointAppearance(System.Drawing.Color.Blue, System.Drawing.Color.Empty, label)
                    {
                        Size = -1,
                        BorderSize = -1,
                        BorderColor = System.Drawing.Color.Empty
                    };

                case DisplayPointType.Process:
                    return new UIMollierPointAppearance(System.Drawing.Color.Gray, System.Drawing.Color.Empty, label)
                    {
                        Size = 8,
                        BorderSize = 2,
                        BorderColor = System.Drawing.Color.Orange
                    };

                case DisplayPointType.Dew:
                    return new UIMollierPointAppearance(System.Drawing.Color.Gray, System.Drawing.Color.Empty, label)
                    {
                        Size = 8,
                        BorderSize = -1,
                        BorderColor = System.Drawing.Color.Empty
                    };

                case DisplayPointType.CoolingSaturation:
                    return new UIMollierPointAppearance(System.Drawing.Color.Gray, System.Drawing.Color.Empty, label)
                    {
                        Size = 5,
                        BorderSize = -1,
                        BorderColor = System.Drawing.Color.Empty
                    };

                case DisplayPointType.Room:
                    return new UIMollierPointAppearance(System.Drawing.Color.Gray, System.Drawing.Color.Empty, label)
                    {
                        Size = 8,
                        BorderSize = 2,
                        BorderColor = System.Drawing.Color.Orange
                    };
            }

            throw new System.NotImplementedException();

        }
    }
}
