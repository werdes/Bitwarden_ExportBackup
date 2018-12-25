using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> oSource, Func<TSource, TKey> oKeySelector)
        {
            HashSet<TKey> oSeenKeys = new HashSet<TKey>();
            foreach (TSource oElement in oSource)
            {
                if (oSeenKeys.Add(oKeySelector(oElement)))
                {
                    yield return oElement;
                }
            }
        }

        public static string Join(this IEnumerable<string> oSource, string cSeparator)
        {
            return string.Join(cSeparator, oSource);
        }

        public static void AddIfNotNull<T>(this List<T> lst, T obj)
        {
            if (obj != null)
                lst.Add(obj);
        }
        public static void InsertIfNotNull<T>(this List<T> lst, int index, T obj)
        {
            if (obj != null)
                lst.Insert(index, obj);
        }
    }
}
