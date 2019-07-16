using QIQI.EProjectFile.Statements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QIQI.EplOnCpp.Core.Statements
{
    public class EocStatementBlock : IList<EocStatement>
    {
        public static EocStatementBlock Translate(CodeConverter C, StatementBlock block)
        {
            if (block == null) return null;
            return new EocStatementBlock(C, block?.Select(x => EocStatement.Translate(C, x)).ToList());
        }

        public CodeConverter C { get; }
        public ProjectConverter P => C.P;
        public CodeWriter Writer => C.Writer;
        public ILoggerWithContext Logger => P.Logger;

        private List<EocStatement> statements;

        public EocStatementBlock(CodeConverter c)
        {
            C = c ?? throw new ArgumentNullException(nameof(c));
            statements = new List<EocStatement>();
        }

        public EocStatementBlock(CodeConverter c, IEnumerable<EocStatement> block)
        {
            C = c ?? throw new ArgumentNullException(nameof(c));
            statements = block?.ToList();
        }

        public EocStatementBlock Optimize()
        {
            for (int i = 0; i < statements.Count; i++)
            {
                statements[i] = statements[i]?.Optimize();
            }
            return this;
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

        public void WriteTo()
        {
            foreach (var item in this)
            {
                item.WriteTo();
            }
        }
    }
}