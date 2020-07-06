namespace QIQI.EplOnCpp.Core
{
    public class EocParameterInfo : EocVariableInfo
    {
        public bool Optional { get; set; } = false;
        public bool ByRef { get; set; } = false;
        public bool VarArgs { get; set; } = false;
    }
}