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

        public override EocStatement Optimize()
        {
            Condition = Condition?.Optimize();
            if (Condition.TryGetConstValueWithCast(ProjectConverter.CppTypeName_Bool, out var x))
            {
                if ((bool)x == true)
                {
                    BlockOnFalse = new EocStatementBlock(C);
                }
                else
                {
                    BlockOnTrue = new EocStatementBlock(C);
                }
            }
            BlockOnTrue = BlockOnTrue?.Optimize();
            BlockOnFalse = BlockOnFalse?.Optimize();
            return this;
        }

        public override void WriteTo(CodeWriter writer)
        {
            writer.NewLine();
            if (Mask)
                writer.Write("// ");

            writer.Write("if (");
            Condition.WriteToWithCast(writer, ProjectConverter.CppTypeName_Bool);
            writer.Write(")");
            writer.AddComment(Comment);
            using (writer.NewBlock())
            {
                BlockOnTrue.WriteTo(writer);
            }
            writer.NewLine();
            if (Mask)
                writer.Write("// ");
            writer.Write("else");
            using (writer.NewBlock())
            {
                BlockOnFalse.WriteTo(writer);
            }
        }
    }
}