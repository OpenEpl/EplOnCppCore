using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

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
            Block = Block?.Optimize();
            return this;
        }

        public override void WriteTo()
        {
            if (Mask)
            {
                Writer.AddCommentLine(CommentOnStart);
                using (Writer.NewBlock())
                {
                    Block.WriteTo();
                }
                Writer.AddCommentLine(CommentOnEnd);
                return;
            }
            var hasVar = Var != null;
            var varPrefix = C.AllocTempVar();
            var varForCount = $"{varPrefix}_count";
            var varForIndex = $"{varPrefix}_index";
            var typeName = $"decltype({varForCount})";

            Writer.AddCommentLine(CommentOnStart);

            Writer.NewLine();
            if (hasVar)
            {
                typeName = Var.GetResultType().ToString();
                Writer.Write(typeName);
                Writer.Write(" ");
            }
            else
            {
                Writer.Write("auto ");
            }
            Writer.Write(varForCount);
            Writer.Write(" = ");
            Count.WriteTo();
            Writer.Write(";");

            Writer.NewLine();
            Writer.Write(typeName);
            Writer.Write(" ");
            Writer.Write(varForIndex);
            Writer.Write(" = 1");
            Writer.Write(";");

            Writer.NewLine();
            Writer.Write("for (");

            if (hasVar)
            {
                C.WriteLetExpression(Var, () =>
                {
                    Writer.Write(varForIndex);
                });
            }

            Writer.Write("; ");

            Writer.Write(varForIndex);
            Writer.Write(" <= ");
            Writer.Write(varForCount);

            Writer.Write("; ");

            if (hasVar)
            {
                C.WriteLetExpression(Var, () =>
                {
                    Writer.Write("++");
                    Writer.Write(varForIndex);
                });
            }
            else
            {
                Writer.Write(varForIndex);
                Writer.Write("++");
            }

            Writer.Write(")");
            using (Writer.NewBlock())
            {
                Block.WriteTo();
            }

            Writer.AddCommentLine(CommentOnEnd);
        }
    }
}