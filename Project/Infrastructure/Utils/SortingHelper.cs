using Core.Models.DTOs.Common;
using Core.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Utils
{
    public static class SortingHelper
    {
        public static IQueryable<T> ApplySorting<T>(IQueryable<T> query, IEnumerable<(string field, SortDirection direction)> sorts)
        {
            if (sorts == null || !sorts.Any())
                return query;

            IOrderedQueryable<T> orderedQuery = null;

            foreach (var sort in sorts)
            {
                if (string.IsNullOrWhiteSpace(sort.field))
                    continue;

                var propertyInfo = typeof(T).GetProperties().FirstOrDefault(p => string.Equals(p.Name, sort.field, StringComparison.OrdinalIgnoreCase));
                if (propertyInfo == null)
                    continue;

                var isDescending = sort.direction == SortDirection.Descending;

                if (orderedQuery == null)
                {
                    orderedQuery = isDescending
                        ? query.OrderByDescending(entity => EF.Property<object>(entity, propertyInfo.Name))
                        : query.OrderBy(entity => EF.Property<object>(entity, propertyInfo.Name));
                }
                else
                {
                    orderedQuery = isDescending
                        ? orderedQuery.ThenByDescending(entity => EF.Property<object>(entity, propertyInfo.Name))
                        : orderedQuery.ThenBy(entity => EF.Property<object>(entity, propertyInfo.Name));
                }
            }

            return orderedQuery ?? query;
        }
    }
}
