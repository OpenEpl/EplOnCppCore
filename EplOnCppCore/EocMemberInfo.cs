namespace QIQI.EplOnCpp.Core
{
    public class EocMemberInfo
    {
        public string CppName { get; set; }
        public CppTypeName DataType { get; set; }
        public string Getter { get; set; }
        public string Setter { get; set; }
        public bool Referencable { get; set; } = true;
    }
}