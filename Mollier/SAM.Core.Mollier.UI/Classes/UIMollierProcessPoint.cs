using Newtonsoft.Json.Linq;
using SAM.Geometry.Mollier;
//using static iText.Svg.SvgConstants;

namespace SAM.Core.Mollier.UI
{
    public class UIMollierProcessPoint : UIMollierPoint
    {
        private UIMollierProcess uIMollierProcess;
        private ProcessReferenceType processReferenceType = ProcessReferenceType.Undefined;

        public UIMollierProcessPoint(UIMollierProcess uIMollierProcess, ProcessReferenceType processReferenceType)
            :base(uIMollierProcess, processReferenceType)
        {
            this.uIMollierProcess = uIMollierProcess == null ? null : new UIMollierProcess(uIMollierProcess);
            this.processReferenceType = processReferenceType;
        }

        public UIMollierProcessPoint(JObject jObject)
            : base(jObject)
        {

        }

        public UIMollierProcessPoint(UIMollierProcessPoint uIMollierProcessPoint)
            :base(uIMollierProcessPoint)
        {
            if(uIMollierProcessPoint != null)
            {
                uIMollierProcess = uIMollierProcessPoint.uIMollierProcess == null ? null : new UIMollierProcess(uIMollierProcessPoint.uIMollierProcess);
                processReferenceType = uIMollierProcessPoint.processReferenceType;
            }
        }

        public IReference Reference
        {
            get
            {
                return Geometry.Mollier.Create.Reference(uIMollierProcess, ProcessReferenceType.Process);
            }
        }

        public UIMollierProcess UIMollierProcess
        {
            get
            {
                return uIMollierProcess;
            }
        }

        public override IUIMollierAppearance UIMollierAppearance
        {
            get
            {
                switch (processReferenceType)
                {
                    case ProcessReferenceType.Process:
                        return new UIMollierPointAppearance(this.uIMollierProcess.UIMollierAppearance as UIMollierAppearance, (base.UIMollierAppearance as UIMollierPointAppearance).BorderSize, (base.UIMollierAppearance as UIMollierPointAppearance).BorderColor);

                    case ProcessReferenceType.Start:
                        return uIMollierProcess.UIMollierPointAppearance_Start;

                    case ProcessReferenceType.End:
                        return uIMollierProcess.UIMollierPointAppearance_End;
                }

                return null;
            }
            set
            {
                UIMollierPointAppearance uIMollierPointAppearance = null;
                switch (processReferenceType)
                {
                    case ProcessReferenceType.Start:
                        uIMollierPointAppearance = uIMollierProcess.UIMollierPointAppearance_Start;
                        break;

                    case ProcessReferenceType.End:
                        uIMollierPointAppearance = uIMollierProcess.UIMollierPointAppearance_End;
                        break;
                }

                if(uIMollierPointAppearance != null)
                {
                    if (value == null)
                    {
                        uIMollierPointAppearance = null;
                    }
                    else if (value is UIMollierPointAppearance)
                    {
                        uIMollierPointAppearance = new UIMollierPointAppearance((UIMollierPointAppearance)value);
                    }
                    else if (value is UIMollierAppearance)
                    {
                        if (uIMollierPointAppearance == null)
                        {
                            uIMollierPointAppearance = new UIMollierPointAppearance();
                        }

                        uIMollierPointAppearance = new UIMollierPointAppearance((UIMollierAppearance)value, uIMollierPointAppearance.BorderSize, uIMollierPointAppearance.BorderColor);
                    }

                    return;
                }

                UIMollierAppearance uIMollierAppearance = uIMollierProcess.UIMollierAppearance as UIMollierAppearance;
                if(uIMollierAppearance != null)
                {
                    if (value == null)
                    {
                        uIMollierAppearance = null;
                    }
                    else if (value is UIMollierPointAppearance)
                    {
                        uIMollierProcess.UIMollierAppearance = new UIMollierAppearance((UIMollierPointAppearance)value);
                    }
                    else if (value is UIMollierAppearance)
                    {

                        uIMollierProcess.UIMollierAppearance = new UIMollierAppearance((UIMollierAppearance)value);
                    }
                    else if (value is UIMollierLabelAppearance)
                    {
                        uIMollierAppearance.UIMollierLabelAppearance = (UIMollierLabelAppearance)value;

                        uIMollierProcess.UIMollierAppearance  = uIMollierAppearance;
                    }

                    return;
                }
            }
        }

        public UIMollierLabelAppearance UIMollierLabelAppearance
        {
            get
            {
                switch (processReferenceType)
                {
                    case ProcessReferenceType.Process:
                        return (uIMollierProcess.UIMollierAppearance as UIMollierAppearance)?.UIMollierLabelAppearance;

                    case ProcessReferenceType.Start:
                        return uIMollierProcess.UIMollierPointAppearance_Start?.UIMollierLabelAppearance;

                    case ProcessReferenceType.End:
                        return uIMollierProcess.UIMollierPointAppearance_End?.UIMollierLabelAppearance;
                }

                return null;

            }
            set
            {
                switch (processReferenceType)
                {
                    case ProcessReferenceType.Process:
                        (uIMollierProcess.UIMollierAppearance as UIMollierAppearance).UIMollierLabelAppearance = value;
                        return;

                    case ProcessReferenceType.Start:
                        uIMollierProcess.UIMollierPointAppearance_Start.UIMollierLabelAppearance = value;
                        return;

                    case ProcessReferenceType.End:
                        uIMollierProcess.UIMollierPointAppearance_End.UIMollierLabelAppearance = value;
                        return;
                }
            }

        }

        public ProcessReferenceType ProcessReferenceType
        {
            get
            {
                return processReferenceType;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if(jObject.ContainsKey("UIMollierProcess"))
            {
                uIMollierProcess = new UIMollierProcess(jObject.Value<JObject>("UIMollierProcess"));
            }

            if (jObject.ContainsKey("ProcessReferenceType"))
            {
                processReferenceType = Core.Query.Enum<ProcessReferenceType>(jObject.Value<string>("ProcessReferenceType"));
            }

            return result;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();

            if(uIMollierProcess != null)
            {
                result.Add("UIMollierProcess", uIMollierProcess.ToJObject());
            }

            if(ProcessReferenceType != ProcessReferenceType.Undefined)
            {
                result.Add("ProcessReferenceType", processReferenceType.ToString());
            }

            return result;
        }
    }
}
