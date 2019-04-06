using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

namespace QIQI.EplOnCpp.Core.Statements
{
    internal class EocWhileStatement : EocStatement
    {
        public static EocWhileStatement Translate(CodeConverter C, WhileStatement stat)
        {
            if (stat == null) return null;
            return new EocWhileStatement(
                C,
                EocExpression.Translate(C, stat.Condition),
                EocStatementBlock.Translate(C, stat.Block),
                stat.MaskOnStart || stat.MaskOnEnd,
                stat.CommentOnStart,
                stat.CommentOnEnd);
        }

        public EocWhileStatement(CodeConverter c, EocExpression condition, EocStatementBlock block, bool mask, string commentOnStart, string commentOnEnd) : base(c)
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
            Writer.Write("// ");
            Writer.Write("while (");
            Condition.WriteToWithCast(ProjectConverter.CppTypeName_Bool);
            Writer.Write(")");
            Writer.AddComment(CommentOnStart);
            using (Writer.NewBlock())
            {
                Block.WriteTo();
            }
            Writer.AddCommentLine(CommentOnEnd);
        }
    }
}