using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile.Statements;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocExpressionStatement : EocStatement
    {
        public static EocExpressionStatement Translate(CodeConverter C, ExpressionStatement stat)
        {
            if (stat == null) return null;
            return new EocExpressionStatement(
                C,
                EocExpression.Translate(C, stat.Expression),
                stat.Mask,
                stat.Comment);
        }

        public bool Mark { get; set; }
        public EocExpression Expr { get; set; }
        public string Comment { get; set; }

        public EocExpressionStatement(CodeConverter c, EocExpression expr, bool mark, string comment) : base(c)
        {
            Expr = expr;
            Mark = mark;
            Comment = comment;
        }

        public override void WriteTo()
        {
            Writer.NewLine();
            if (Mark)
                Writer.Write("// ");
            if (Expr != null)
            {
                Expr.WriteTo();
                Writer.Write("; ");
            }
            Writer.AddComment(Comment);
        }
    }
}