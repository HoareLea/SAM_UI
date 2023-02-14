namespace SAM.Analytical.UI.WPF
{
    public class ApertureData
    {
        private Aperture aperture;

        public ApertureData(Aperture aperture)
        {
            this.aperture = aperture == null ? null : new Aperture(aperture);
        }

        public IOpeningProperties OpeningProperties
        {
            get
            {
                Aperture aperture = Aperture;
                if(aperture == null)
                {
                    return null;
                }

                if(!aperture.TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
                {
                    return null;
                }

                return openingProperties;
            }

            set
            {
                Aperture aperture = Aperture;
                if (aperture == null)
                {
                    return;
                }

                if(value == null)
                {
                    aperture.RemoveValue(ApertureParameter.OpeningProperties);
                }
                else
                {
                    aperture.SetValue(ApertureParameter.OpeningProperties, value);
                }
            }
        }

        public Aperture Aperture
        {
            get
            {
                return aperture;
            }
        }

        public System.Drawing.Color Color
        {
            get
            {
                Aperture aperture = Aperture;
                if(aperture == null)
                {
                    return System.Drawing.Color.Empty;
                }

                if(aperture.TryGetValue(ApertureParameter.Color, out System.Drawing.Color result))
                {
                    return result;
                }

                return System.Drawing.Color.Empty;
            }

            set
            {
                Aperture aperture = Aperture;
                if (aperture == null)
                {
                    return;
                }

                if (value == null || value == System.Drawing.Color.Empty)
                {
                    aperture.RemoveValue(ApertureParameter.Color);
                }
                else
                {
                    aperture.SetValue(ApertureParameter.Color, value);
                }
            }
        }

        public ApertureConstruction ApertureConstruction
        {
            get
            {
                return Aperture?.ApertureConstruction;
            }

            set
            {
                aperture = new Aperture(aperture, value);
            }
        }

        public double Area
        {
            get
            {
                Aperture aperture = Aperture;
                if(aperture == null)
                {
                    return double.NaN;
                }

                return aperture.GetArea();
            }
        }

        public string Function
        {
            get
            {
                IOpeningProperties openingProperties = OpeningProperties;
                if(openingProperties == null)
                {
                    return null;
                }

                if (openingProperties.TryGetValue(OpeningPropertiesParameter.Function, out string result))
                {
                    return result;
                }

                return null;
            }

            set
            {
                IOpeningProperties openingProperties = OpeningProperties;
                if(openingProperties == null)
                {
                    if(value == null)
                    {
                        return;
                    }

                    openingProperties = new OpeningProperties(double.NaN);
                }

                openingProperties.SetValue(OpeningPropertiesParameter.Function, value);

                Aperture aperture = Aperture;
                if(aperture != null)
                {
                    aperture.SetValue(ApertureParameter.OpeningProperties, openingProperties);
                }
            }
        }

        public double DischargeCoefficient
        {
            get
            {
                IOpeningProperties openingProperties = OpeningProperties;
                if (openingProperties == null)
                {
                    return double.NaN;
                }

                return openingProperties.GetDischargeCoefficient();
            }

            set
            {
                IOpeningProperties openingProperties = OpeningProperties;
                if (openingProperties == null)
                {
                    if(double.IsNaN(value))
                    {
                        return;
                    }

                    openingProperties = new OpeningProperties(value);
                }
                else
                {
                    openingProperties = new OpeningProperties(openingProperties, openingProperties.GetDischargeCoefficient());
                }


                Aperture aperture = Aperture;
                if (aperture != null)
                {
                    aperture.SetValue(ApertureParameter.OpeningProperties, openingProperties);
                }
            }
        }

        public string Description
        {
            get
            {
                IOpeningProperties openingProperties = OpeningProperties;
                if (openingProperties == null)
                {
                    return null;
                }

                if (openingProperties.TryGetValue(OpeningPropertiesParameter.Description, out string result))
                {
                    return result;
                }

                return null;
            }

            set
            {
                IOpeningProperties openingProperties = OpeningProperties;
                if (openingProperties == null)
                {
                    if (value == null)
                    {
                        return;
                    }

                    openingProperties = new OpeningProperties(double.NaN);
                }

                openingProperties.SetValue(OpeningPropertiesParameter.Description, value);

                Aperture aperture = Aperture;
                if (aperture != null)
                {
                    aperture.SetValue(ApertureParameter.OpeningProperties, openingProperties);
                }
            }
        }

        public string Name
        {
            get
            {
                return ApertureConstruction?.Name;
            }
        }

    }
}
