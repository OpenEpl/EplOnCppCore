using QIQI.EProjectFile.Expressions;
using System;

namespace QIQI.EplOnCpp.Core.Expressions
{
    public class EocConstantExpression : EocExpression
    {
        public static EocConstantExpression Translate(CodeConverter C, ConstantExpression expr)
        {
            if (expr == null) return null;
            return new EocConstantExpression(C, C.P.GetEocConstantInfo(expr));
        }

        public static EocConstantExpression Translate(CodeConverter C, EmnuConstantExpression expr)
        {
            if (expr == null) return null;
            return new EocConstantExpression(C, C.P.GetEocConstantInfo(expr));
        }

        public EocConstantExpression(CodeConverter c, EocConstantInfo eocConstantInfo) : base(c)
        {
            EocConstantInfo = eocConstantInfo;
        }

        public EocConstantInfo EocConstantInfo { get; }

        public override CppTypeName GetResultType()
        {
            return EocConstantInfo.DataType;
        }

        public override void WriteTo()
        {
            if (!string.IsNullOrWhiteSpace(EocConstantInfo.Getter))
            {
                Writer.Write(EocConstantInfo.Getter);
                Writer.Write("()");
            }
            else if(!string.IsNullOrWhiteSpace(EocConstantInfo.CppName))
            {
                Writer.Write(EocConstantInfo.CppName);
            }
            else
            {
                Writer.WriteLiteral(EocConstantInfo.Value);
            }
        }
    }
}