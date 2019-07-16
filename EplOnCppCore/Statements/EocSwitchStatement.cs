using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocSwitchStatement : EocStatement
    {
        public EocSwitchStatement(CodeConverter c, List<CaseInfo> @case, EocStatementBlock defaultBlock) : base(c)
        {
            Case = @case;
            DefaultBlock = defaultBlock;
        }

        public static EocSwitchStatement Translate(CodeConverter C, SwitchStatement stat)
        {
            return new EocSwitchStatement(
                C,
                stat.Case.Select(x => CaseInfo.Translate(C, x)).ToList(),
                EocStatementBlock.Translate(C, stat.DefaultBlock));
        }

        public override EocStatement Optimize()
        {
            Case.ForEach(x =>
            {
                x.Condition = x.Condition?.Optimize();
                x.Block = x.Block?.Optimize();
            });
            DefaultBlock = DefaultBlock?.Optimize();
            return this;
        }

        public override void WriteTo(CodeWriter writer)
        {
            foreach (var item in Case)
            {
                if (item.Mask)
                {
                    using (writer.NewBlock())
                    {
                        writer.AddCommentLine(item.Comment);
                        item.Block.WriteTo(writer);
                    }
                    return;
                }
            }

            for (int i = 0; i < Case.Count; i++)
            {
                writer.NewLine();
                writer.Write(i == 0 ? "if" : "else if");
                writer.Write(" (");
                Case[i].Condition.WriteToWithCast(writer, ProjectConverter.CppTypeName_Bool);
                writer.Write(")");
                using (writer.NewBlock())
                {
                    writer.AddCommentLine(Case[i].Comment);
                    Case[i].Block.WriteTo(writer);
                }
            }
            writer.NewLine();
            writer.Write("else");
            using (writer.NewBlock())
            {
                DefaultBlock.WriteTo(writer);
            }
        }

        public List<CaseInfo> Case { get; }
        public EocStatementBlock DefaultBlock { get; set; }

        public class CaseInfo
        {
            public EocExpression Condition { get; set; }
            public string UnexaminedCode { get; set; }
            public EocStatementBlock Block { get; set; }
            public string Comment { get; set; }
            public bool Mask { get; set; }

            public static CaseInfo Translate(CodeConverter C, SwitchStatement.CaseInfo info)
            {
                return new CaseInfo()
                {
                    Condition = EocExpression.Translate(C, info.Condition),
                    UnexaminedCode = info.UnexaminedCode,
                    Block = EocStatementBlock.Translate(C, info.Block),
                    Comment = info.Comment,
                    Mask = info.Mask
                };
            }
        }
    }
}