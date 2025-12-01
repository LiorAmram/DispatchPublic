using System.Linq.Expressions;

namespace DispatchPublic.Utils
{
    public static class Extensions
    {
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition
                ? source.Where(predicate)
                : source;
        }
    }
}
