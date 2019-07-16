using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

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
            if (!Mask && Condition.TryGetConstValueWithCast(ProjectConverter.CppTypeName_Bool, out var x))
            {
                if ((bool)x == false)
                {
                    return new EocExpressionStatement(C, null, Comment);
                }
            }
            Block = Block?.Optimize();
            return this;
        }

        public EocIfStatement(CodeConverter c, EocExpression condition, EocStatementBlock block, bool mask, string comment) : base(c)
        {
            Condition = condition;
            Block = block;
            Comment = comment;
            Mask = mask;
        }

        public override void WriteTo()
        {
            Writer.NewLine();
            if (Mask)
                Writer.Write("// ");
            Writer.Write("if (");
            Condition.WriteToWithCast(ProjectConverter.CppTypeName_Bool);
            Writer.Write(")");
            Writer.AddComment(Comment);
            using (Writer.NewBlock())
            {
                Block.WriteTo();
            }
        }
    }
}