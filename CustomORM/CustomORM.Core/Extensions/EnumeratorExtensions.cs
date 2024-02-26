using System.Collections;

namespace CustomORM.Core.Extensions
{
    public static class EnumeratorExtensions
    {
        public static IEnumerable ToIEnumerable(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}