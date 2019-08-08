using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using System;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocCounterStatement : EocStatement
    {
        public static EocCounterStatement Translate(CodeConverter C, CounterStatement stat)
        {
            if (stat == null) return null;
            return new EocCounterStatement(
                C,
                EocExpression.Translate(C, stat.Count),
                EocExpression.Translate(C, stat.Var),
                EocStatementBlock.Translate(C, stat.Block),
                stat.MaskOnStart || stat.MaskOnEnd,
                stat.CommentOnStart,
                stat.CommentOnEnd);
        }

        public EocCounterStatement(CodeConverter c, EocExpression count, EocExpression var, EocStatementBlock block, bool mask, string commentOnStart, string commentOnEnd) : base(c)
        {
            Count = count;
            Var = var;
            Block = block;
            Mask = mask;
            CommentOnStart = commentOnStart;
            CommentOnEnd = commentOnEnd;
        }

        public EocExpression Count { get; set; }
        public EocExpression Var { get; set; }
        public EocStatementBlock Block { get; set; }
        public bool Mask { get; set; }
        public string CommentOnStart { get; set; }
        public string CommentOnEnd { get; set; }

        public override EocStatement Optimize()
        {
            Count = Count?.Optimize();
            Var = Var?.Optimize();
            Block = Block?.Optimize() as EocStatementBlock;
            return this;
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            if (Count != null)
            {
                if (deep)
                    Count.ProcessSubExpression(processor, deep);
                Count = processor(Count);
            }
            if (Var != null)
            {
                if (deep)
                    Var.ProcessSubExpression(processor, deep);
                Var = processor(Var);
            }
            Block?.ProcessSubExpression(processor, deep);
        }

        public override void WriteTo(CodeWriter writer)
        {
            if (Mask)
            {
                writer.AddCommentLine(CommentOnStart);
                using (writer.NewBlock())
                {
                    Block.WriteTo(writer);
                }
                writer.AddCommentLine(CommentOnEnd);
                return;
            }
            var hasVar = Var != null;
            var varPrefix = C.AllocTempVar();
            var varForCount = $"{varPrefix}_count";
            var varForIndex = $"{varPrefix}_index";
            var typeName = $"decltype({varForCount})";
            CppTypeName expectedType = null;

            writer.AddCommentLine(CommentOnStart);

            writer.NewLine();
            if (hasVar)
            {
                expectedType = Var.GetResultType();
                typeName = expectedType.ToString();
                writer.Write(typeName);
                writer.Write(" ");
            }
            else
            {
                writer.Write("auto ");
            }
            writer.Write(varForCount);
            writer.Write(" = ");
            Count.WriteToWithCast(writer, expectedType);
            writer.Write(";");

            writer.NewLine();
            writer.Write(typeName);
            writer.Write(" ");
            writer.Write(varForIndex);
            writer.Write(" = 1");
            writer.Write(";");

            writer.NewLine();
            writer.Write("for (");

            if (hasVar)
            {
                C.WriteLetExpression(writer, Var, () =>
                {
                    writer.Write(varForIndex);
                });
            }

            writer.Write("; ");

            writer.Write(varForIndex);
            writer.Write(" <= ");
            writer.Write(varForCount);

            writer.Write("; ");

            if (hasVar)
            {
                C.WriteLetExpression(writer, Var, () =>
                {
                    writer.Write("++");
                    writer.Write(varForIndex);
                });
            }
            else
            {
                writer.Write(varForIndex);
                writer.Write("++");
            }

            writer.Write(")");
            using (writer.NewBlock())
            {
                Block.WriteTo(writer);
            }

            writer.AddCommentLine(CommentOnEnd);
        }
    }
}