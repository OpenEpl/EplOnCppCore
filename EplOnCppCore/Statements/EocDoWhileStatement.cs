using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

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
            Writer.NewLine();
            Writer.Write("do");
            Writer.AddComment(CommentOnStart);
            using (Writer.NewBlock())
            {
                Block.WriteTo();
            }
            Writer.Write("while (");
            Condition.WriteToWithCast(ProjectConverter.CppTypeName_Bool);
            Writer.Write(")");
            Writer.AddComment(CommentOnEnd);
        }
    }
}