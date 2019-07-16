using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocForStatement : EocStatement
    {
        public EocForStatement(CodeConverter c, EocExpression start, EocExpression end, EocExpression step, EocExpression var, EocStatementBlock block, bool mask, string commentOnStart, string commentOnEnd) : base(c)
        {
            Start = start;
            End = end;
            Step = step;
            Var = var;
            Block = block;
            Mask = mask;
            CommentOnStart = commentOnStart;
            CommentOnEnd = commentOnEnd;
        }

        public static EocForStatement Translate(CodeConverter C, ForStatement stat)
        {
            if (stat == null) return null;
            return new EocForStatement(
                C,
                EocExpression.Translate(C, stat.Start),
                EocExpression.Translate(C, stat.End),
                EocExpression.Translate(C, stat.Step),
                EocExpression.Translate(C, stat.Var),
                EocStatementBlock.Translate(C, stat.Block),
                stat.MaskOnStart || stat.MaskOnEnd,
                stat.CommentOnStart,
                stat.CommentOnEnd);
        }

        public EocExpression Start { get; set; }
        public EocExpression End { get; set; }
        public EocExpression Step { get; set; }
        public EocExpression Var { get; set; }
        public EocStatementBlock Block { get; set; }
        public bool Mask { get; set; }
        public string CommentOnStart { get; set; }
        public string CommentOnEnd { get; set; }

        public override EocStatement Optimize()
        {
            Start = Start?.Optimize();
            End = End?.Optimize();
            Step = Step?.Optimize();
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
            var varForIndex = $"{varPrefix}_index";
            var varForEnd = $"{varPrefix}_end";
            var varForStep = $"{varPrefix}_step";
            var varForIsNegStep = $"{varPrefix}_isNegStep";
            var typeName = $"decltype({varForIndex})";

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
            Writer.Write(varForIndex);
            Writer.Write(" = ");
            Start.WriteTo();
            Writer.Write(";");

            Writer.NewLine();
            Writer.Write(typeName);
            Writer.Write(" ");
            Writer.Write(varForEnd);
            Writer.Write(" = ");
            End.WriteTo();
            Writer.Write(";");

            Writer.NewLine();
            Writer.Write(typeName);
            Writer.Write(" ");
            Writer.Write(varForStep);
            Writer.Write(" = ");
            Step.WriteTo();
            Writer.Write(";");

            Writer.NewLine();
            Writer.Write("bool ");
            Writer.Write(varForIsNegStep);
            Writer.Write(" = ");
            Writer.Write(varForStep);
            Writer.Write(" <= 0;");

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

            Writer.Write(varForIsNegStep);
            Writer.Write(" ? ");
            Writer.Write(varForIndex);
            Writer.Write(" >= ");
            Writer.Write(varForEnd);
            Writer.Write(" : ");
            Writer.Write(varForIndex);
            Writer.Write(" <= ");
            Writer.Write(varForEnd);

            Writer.Write("; ");

            if (hasVar)
            {
                C.WriteLetExpression(Var, () =>
                {
                    Writer.Write(varForIndex);
                    Writer.Write(" += ");
                    Writer.Write(varForStep);
                });
            }
            else
            {
                Writer.Write(varForIndex);
                Writer.Write(" += ");
                Writer.Write(varForStep);
            }

            Writer.Write(")");
            using (Writer.NewBlock())
            {
                Block.WriteTo();
            }
        }
    }
}