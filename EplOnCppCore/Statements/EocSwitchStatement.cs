using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using System;
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
                x.Block = x.Block?.Optimize() as EocStatementBlock;
            });
            DefaultBlock = DefaultBlock?.Optimize() as EocStatementBlock;
            return this;
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            Case.ForEach(x =>
            {
                if(x.Condition != null)
                {
                    if (deep)
                        x.Condition.ProcessSubExpression(processor, deep);
                    x.Condition = processor(x.Condition);
                }
                x.Block?.ProcessSubExpression(processor, deep);
            });

            DefaultBlock?.ProcessSubExpression(processor, deep);
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
                Case[i].Condition.WriteToWithCast(writer, EocDataTypes.Bool);
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