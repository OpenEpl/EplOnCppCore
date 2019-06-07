using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocIfElseStatement : EocStatement
    {
        public static EocIfElseStatement Translate(CodeConverter C, IfElseStatement stat)
        {
            if (stat == null) return null;
            return new EocIfElseStatement(
                C,
                EocExpression.Translate(C, stat.Condition),
                EocStatementBlock.Translate(C, stat.BlockOnTrue),
                EocStatementBlock.Translate(C, stat.BlockOnFalse),
                stat.Mask,
                stat.Comment);
        }

        public EocExpression Condition { get; set; }
        public EocStatementBlock BlockOnTrue { get; set; }
        public EocStatementBlock BlockOnFalse { get; set; }
        public string Comment { get; set; }
        public bool Mask { get; set; }

        public EocIfElseStatement(CodeConverter c, EocExpression condition, EocStatementBlock blockOnTrue, EocStatementBlock blockOnFalse, bool mask, string comment) : base(c)
        {
            Condition = condition;
            BlockOnTrue = blockOnTrue;
            BlockOnFalse = blockOnFalse;
            Comment = comment;
            Mask = mask;
        }

        public override void WriteTo()
        {
            Writer.NewLine();
            if (Mask)
                Writer.Write("// ");
            else if (Condition.TryGetConstValueWithCast(ProjectConverter.CppTypeName_Bool, out var x))
            {
                if ((bool)x == true)
                {
                    Writer.AddComment(Comment);
                    using (Writer.NewBlock())
                    {
                        BlockOnTrue.WriteTo();
                    }
                    return;
                }
                else
                {
                    Writer.AddComment(Comment);
                    using (Writer.NewBlock())
                    {
                        BlockOnFalse.WriteTo();
                    }
                    return;
                }
            }

            Writer.Write("if (");
            Condition.WriteToWithCast(ProjectConverter.CppTypeName_Bool);
            Writer.Write(")");
            Writer.AddComment(Comment);
            using (Writer.NewBlock())
            {
                BlockOnTrue.WriteTo();
            }
            Writer.NewLine();
            if (Mask)
                Writer.Write("// ");
            Writer.Write("else");
            using (Writer.NewBlock())
            {
                BlockOnFalse.WriteTo();
            }
        }
    }
}