namespace QIQI.EplOnCpp.Core
{
    public class EocConstantInfo
    {
        public string CppName { get; set; }
        public string Getter { get; set; }
        public CppTypeName DataType { get; set; } = ProjectConverter.CppTypeName_Int;
    }
}