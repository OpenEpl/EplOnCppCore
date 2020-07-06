namespace QIQI.EplOnCpp.Core
{
    public class EocMemberInfo : EocVariableInfo
    {
        public bool Static { get; set; } = false;
        public string Getter { get; set; }
        public string Setter { get; set; }
        public bool Referencable { get; set; } = true;
    }
}