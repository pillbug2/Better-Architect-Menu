using System.Collections.Generic;
using System.Text.RegularExpressions;
using Verse;

namespace BetterArchitect
{

    public static class ExtensionMethods
    {
        public static void SortBy<T, K>(this List<T> list, System.Func<T, K> keySelector, bool ascending) where K : System.IComparable
        {
            list.Sort((a, b) =>
            {
                var keyA = keySelector(a);
                var keyB = keySelector(b);
                int comparison = keyA.CompareTo(keyB);
                return ascending ? comparison : -comparison;
            });
        }
        
        public static string ToStringTranslated(this SortBy sortBy)
        {
            return ("BA." + sortBy.ToString()).Translate();
        }
    }
}
