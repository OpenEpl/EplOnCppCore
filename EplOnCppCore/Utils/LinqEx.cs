using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace QIQI.EplOnCpp.Core.Utils
{
    internal static class LinqEx
    {
        public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            var result = new SortedDictionary<TKey, TValue>();
            foreach (var item in source)
            {
                result.Add(item.Key, item.Value);
            }
            return result;
        }

        public static SortedDictionary<TKey, TElement> ToSortedDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector, 
            Func<TSource, TElement> elementSelector)
        {
            var result = new SortedDictionary<TKey, TElement>();
            foreach (var item in source)
            {
                result.Add(keySelector(item), elementSelector(item));
            }
            return result;
        }

        public static SortedDictionary<TKey, TValue> FilterSortedDictionary<TKey, TValue>(
            this IDictionary<TKey, TValue> source,
            Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            var result = new SortedDictionary<TKey, TValue>();
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result.Add(item.Key, item.Value);
                }
            }
            return result;
        }
    }
}
