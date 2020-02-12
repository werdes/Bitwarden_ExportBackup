using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void AddRangeIfNotNull<T>(this List<T> lst, IEnumerable<T> objects)
        {
            if (objects != null)
                lst.AddRange(objects);

        }

        public static bool ContainsAny<T>(this IEnumerable<T> haystack, IEnumerable<T> needles) where T : new()
        {
            return haystack.Any(x => needles.Contains(x));
        }
    }
}
