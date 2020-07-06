using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using System;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocIfStatement : EocStatement
    {
        public static EocIfStatement Translate(CodeConverter C, IfStatement stat)
        {
            if (stat == null) return null;
            return new EocIfStatement(
                C,
                EocExpression.Translate(C, stat.Condition),
                EocStatementBlock.Translate(C, stat.Block),
                stat.Mask,
                stat.Comment);
        }

        public EocExpression Condition { get; set; }
        public EocStatementBlock Block { get; set; }
        public string Comment { get; set; }
        public bool Mask { get; set; }

        public override EocStatement Optimize()
        {
            Condition = Condition?.Optimize();
            if (!Mask && Condition.TryGetConstValueWithCast(EocDataTypes.Bool, out var x))
            {
                if ((bool)x == false)
                {
                    return new EocExpressionStatement(C, null, Comment);
                }
            }
            Block = Block?.Optimize() as EocStatementBlock;
            return this;
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            if (deep)
            {
                Condition?.ProcessSubExpression(processor, deep);
            }
            if (Condition != null)
                Condition = processor(Condition);
            Block?.ProcessSubExpression(processor, deep);
        }

        public EocIfStatement(CodeConverter c, EocExpression condition, EocStatementBlock block, bool mask, string comment) : base(c)
        {
            Condition = condition;
            Block = block;
            Comment = comment;
            Mask = mask;
        }

        public override void WriteTo(CodeWriter writer)
        {
            writer.NewLine();
            if (Mask)
                writer.Write("// ");
            writer.Write("if (");
            Condition.WriteToWithCast(writer, EocDataTypes.Bool);
            writer.Write(")");
            writer.AddComment(Comment);
            using (writer.NewBlock())
            {
                Block.WriteTo(writer);
            }
        }
    }
}