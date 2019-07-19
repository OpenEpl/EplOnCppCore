using QIQI.EplOnCpp.Core.Expressions;
using QIQI.EProjectFile;
using QIQI.EProjectFile.Statements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocStatementBlock : EocStatement, IList<EocStatement>
    {
        public static EocStatementBlock Translate(CodeConverter C, StatementBlock block)
        {
            if (block == null) return null;
            return new EocStatementBlock(C, block?.Select(x =>
            {
                try
                {
                    return EocStatement.Translate(C, x);
                }
                catch (Exception exception)
                {
                    return new EocErrorStatement(C, exception, x.ToTextCode(C.P.IdToNameMap));
                }
            }));
        }

        private List<EocStatement> statements;

        public EocStatementBlock(CodeConverter c) : base(c)
        {
            statements = new List<EocStatement>();
        }

        public EocStatementBlock(CodeConverter c, IEnumerable<EocStatement> block) : base(c)
        {
            statements = block?.ToList();
        }

        public override EocStatement Optimize()
        {
            for (int i = 0; i < statements.Count; i++)
            {
                statements[i] = statements[i]?.Optimize();
            }
            return this;
        }

        public override void ProcessSubExpression(Func<EocExpression, EocExpression> processor, bool deep = true)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                statements[i].ProcessSubExpression(processor, deep);
            }
        }

        public EocStatement this[int index] { get => ((IList<EocStatement>)statements)[index]; set => ((IList<EocStatement>)statements)[index] = value; }

        public int Count => ((IList<EocStatement>)statements).Count;

        public bool IsReadOnly => ((IList<EocStatement>)statements).IsReadOnly;

        public void Add(EocStatement item)
        {
            ((IList<EocStatement>)statements).Add(item);
        }

        public void Clear()
        {
            ((IList<EocStatement>)statements).Clear();
        }

        public bool Contains(EocStatement item)
        {
            return ((IList<EocStatement>)statements).Contains(item);
        }

        public void CopyTo(EocStatement[] array, int arrayIndex)
        {
            ((IList<EocStatement>)statements).CopyTo(array, arrayIndex);
        }

        public IEnumerator<EocStatement> GetEnumerator()
        {
            return ((IList<EocStatement>)statements).GetEnumerator();
        }

        public int IndexOf(EocStatement item)
        {
            return ((IList<EocStatement>)statements).IndexOf(item);
        }

        public void Insert(int index, EocStatement item)
        {
            ((IList<EocStatement>)statements).Insert(index, item);
        }

        public bool Remove(EocStatement item)
        {
            return ((IList<EocStatement>)statements).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<EocStatement>)statements).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<EocStatement>)statements).GetEnumerator();
        }

        public override void WriteTo(CodeWriter writer)
        {
            foreach (var item in this)
            {
                item.WriteTo(writer);
            }
        }
    }
}