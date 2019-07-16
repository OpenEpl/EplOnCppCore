namespace QIQI.EplOnCpp.Core
{
    public class EocParameterInfo
    {
        public bool Optional { get; set; } = false;
        public bool ByRef { get; set; } = false;
        public bool VarArgs { get; set; } = false;
        public CppTypeName DataType { get; set; }

        public string Name { get; set; } //可选
    }
}