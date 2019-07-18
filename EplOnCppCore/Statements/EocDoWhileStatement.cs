using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;
using System;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocDoWhileStatement : EocStatement
    {
        public static EocDoWhileStatement Translate(CodeConverter C, DoWhileStatement stat)
        {
            if (stat == null) return null;
            return new EocDoWhileStatement(
                C,
                EocExpression.Translate(C, stat.Condition),
                EocStatementBlock.Translate(C, stat.Block),
                stat.MaskOnStart || stat.MaskOnEnd,
                stat.CommentOnStart,
                stat.CommentOnEnd);
        }

        public EocDoWhileStatement(CodeConverter c, EocExpression condition, EocStatementBlock block, bool mask, string commentOnStart, string commentOnEnd) : base(c)
        {
            Condition = condition;
            Block = block;
            Mask = mask;
            CommentOnStart = commentOnStart;
            CommentOnEnd = commentOnEnd;
        }

        public EocExpression Condition { get; set; }
        public EocStatementBlock Block { get; set; }
        public bool Mask { get; set; }
        public string CommentOnStart { get; set; }
        public string CommentOnEnd { get; set; }

        public override EocStatement Optimize()
        {
            Condition = Condition?.Optimize();
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
            writer.NewLine();
            writer.Write("do");
            writer.AddComment(CommentOnStart);
            using (writer.NewBlock())
            {
                Block.WriteTo(writer);
            }
            writer.Write("while (");
            Condition.WriteToWithCast(writer, ProjectConverter.CppTypeName_Bool);
            writer.Write(")");
            writer.AddComment(CommentOnEnd);
        }
    }
}