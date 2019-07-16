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

        public override void WriteTo()
        {
            foreach (var item in Case)
            {
                if (item.Mask)
                {
                    using (Writer.NewBlock())
                    {
                        Writer.AddCommentLine(item.Comment);
                        item.Block.WriteTo();
                    }
                    return;
                }
            }

            for (int i = 0; i < Case.Count; i++)
            {
                Writer.NewLine();
                Writer.Write(i == 0 ? "if" : "else if");
                Writer.Write(" (");
                Case[i].Condition.WriteToWithCast(ProjectConverter.CppTypeName_Bool);
                Writer.Write(")");
                using (Writer.NewBlock())
                {
                    Writer.AddCommentLine(Case[i].Comment);
                    Case[i].Block.WriteTo();
                }
            }
            Writer.NewLine();
            Writer.Write("else");
            using (Writer.NewBlock())
            {
                DefaultBlock.WriteTo();
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