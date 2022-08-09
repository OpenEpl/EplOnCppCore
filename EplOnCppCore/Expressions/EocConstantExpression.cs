using QIQI.EProjectFile.Expressions;
using QuikGraph;
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

        public override void WriteTo(CodeWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(EocConstantInfo.Getter))
            {
                writer.Write(EocConstantInfo.Getter);
                writer.Write("()");
            }
            else if(!string.IsNullOrWhiteSpace(EocConstantInfo.CppName))
            {
                writer.Write(EocConstantInfo.CppName);
            }
            else
            {
                writer.WriteLiteral(EocConstantInfo.Value);
            }
        }

        public override bool TryGetConstValue(out object value)
        {
            value = EocConstantInfo.Value;
            return EocConstantInfo.Value != null;
        }

        public override void AnalyzeDependencies(AdjacencyGraph<string, IEdge<string>> graph)
        {
            base.AnalyzeDependencies(graph);
            if (EocConstantInfo?.RefId != null)
            {
                graph.AddVerticesAndEdge(new Edge<string>(C.RefId, EocConstantInfo.RefId));
            }
        }
    }
}