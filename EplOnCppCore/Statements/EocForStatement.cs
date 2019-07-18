using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using System;

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
            Block = Block?.Optimize() as EocStatementBlock;
            return this;
        }
        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            if (Start != null)
            {
                if (deep)
                    Start.ProcessSubExpression(processor, deep);
                Start = processor(Start);
            }
            if (End != null)
            {
                if (deep)
                    End.ProcessSubExpression(processor, deep);
                End = processor(End);
            }
            if (Step != null)
            {
                if (deep)
                    Step.ProcessSubExpression(processor, deep);
                Step = processor(Step);
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
            var varForIndex = $"{varPrefix}_index";
            var varForEnd = $"{varPrefix}_end";
            var varForStep = $"{varPrefix}_step";
            var varForIsNegStep = $"{varPrefix}_isNegStep";
            var typeName = $"decltype({varForIndex})";

            writer.NewLine();
            if (hasVar)
            {
                typeName = Var.GetResultType().ToString();
                writer.Write(typeName);
                writer.Write(" ");
            }
            else
            {
                writer.Write("auto ");
            }
            writer.Write(varForIndex);
            writer.Write(" = ");
            Start.WriteTo(writer);
            writer.Write(";");

            writer.NewLine();
            writer.Write(typeName);
            writer.Write(" ");
            writer.Write(varForEnd);
            writer.Write(" = ");
            End.WriteTo(writer);
            writer.Write(";");

            writer.NewLine();
            writer.Write(typeName);
            writer.Write(" ");
            writer.Write(varForStep);
            writer.Write(" = ");
            Step.WriteTo(writer);
            writer.Write(";");

            writer.NewLine();
            writer.Write("bool ");
            writer.Write(varForIsNegStep);
            writer.Write(" = ");
            writer.Write(varForStep);
            writer.Write(" <= 0;");

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

            writer.Write(varForIsNegStep);
            writer.Write(" ? ");
            writer.Write(varForIndex);
            writer.Write(" >= ");
            writer.Write(varForEnd);
            writer.Write(" : ");
            writer.Write(varForIndex);
            writer.Write(" <= ");
            writer.Write(varForEnd);

            writer.Write("; ");

            if (hasVar)
            {
                C.WriteLetExpression(writer, Var, () =>
                {
                    writer.Write(varForIndex);
                    writer.Write(" += ");
                    writer.Write(varForStep);
                });
            }
            else
            {
                writer.Write(varForIndex);
                writer.Write(" += ");
                writer.Write(varForStep);
            }

            writer.Write(")");
            using (writer.NewBlock())
            {
                Block.WriteTo(writer);
            }
        }
    }
}