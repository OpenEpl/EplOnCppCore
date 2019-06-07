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
            else if (Condition.TryGetConstValueWithCast(ProjectConverter.CppTypeName_Bool, out var x))
            {
                if ((bool)x == false)
                {
                    Writer.AddComment(Comment);
                    return;
                }
            }
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