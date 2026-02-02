using System.Linq.Expressions;
using System.Reflection;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.Helpers;

/// <summary>
/// Extension methods for query operations (paging, sorting, searching, filtering)
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Applies paging to a query
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Max 100 items per page

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Applies sorting to a query
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, string? sortOrder = "desc")
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        var property = GetProperty<T>(sortBy);
        if (property == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);

        var isAscending = sortOrder?.ToLower() == "asc";
        var methodName = isAscending ? "OrderBy" : "OrderByDescending";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), property.PropertyType },
            query.Expression,
            Expression.Quote(orderByExpression)
        );

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    /// <summary>
    /// Applies search across multiple string properties
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string? searchTerm, params Expression<Func<T, object>>[] searchProperties)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchProperties == null || searchProperties.Length == 0)
            return query;

        var searchLower = searchTerm.ToLower();
        var parameter = Expression.Parameter(typeof(T), "x");

        Expression? combinedExpression = null;

        foreach (var propertyExpression in searchProperties)
        {
            // Get the actual property access expression
            var body = propertyExpression.Body;
            if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                body = unary.Operand;
            }

            // Create Contains expression for string properties
            var propertyType = ((MemberExpression)body).Type;
            if (propertyType == typeof(string))
            {
                var propertyAccess = Expression.Property(parameter, ((MemberExpression)body).Member.Name);
                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                var constant = Expression.Constant(searchLower);
                var containsCall = Expression.Call(toLowerCall, containsMethod!, constant);

                // Handle nullable properties
                var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                var conditional = Expression.AndAlso(nullCheck, containsCall);

                combinedExpression = combinedExpression == null
                    ? conditional
                    : Expression.OrElse(combinedExpression, conditional);
            }
        }

        if (combinedExpression == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Applies search using property names (simpler approach)
    /// </summary>
    public static IQueryable<T> ApplySearchByProperties<T>(this IQueryable<T> query, string? searchTerm, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || propertyNames == null || propertyNames.Length == 0)
            return query;

        var searchLower = searchTerm.ToLower();
        var parameter = Expression.Parameter(typeof(T), "x");

        Expression? combinedExpression = null;

        foreach (var propertyName in propertyNames)
        {
            var property = GetProperty<T>(propertyName);
            if (property == null || property.PropertyType != typeof(string))
                continue;

            var propertyAccess = Expression.Property(parameter, property);
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var constant = Expression.Constant(searchLower);
            var containsCall = Expression.Call(toLowerCall, containsMethod!, constant);

            // Handle nullable properties
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            var conditional = Expression.AndAlso(nullCheck, containsCall);

            combinedExpression = combinedExpression == null
                ? conditional
                : Expression.OrElse(combinedExpression, conditional);
        }

        if (combinedExpression == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Applies filters based on FilterCriteria list
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<FilterCriteria>? filters)
    {
        if (filters == null || !filters.Any())
            return query;

        foreach (var filter in filters)
        {
            query = query.ApplyFilter(filter);
        }

        return query;
    }

    /// <summary>
    /// Applies a single filter criteria
    /// </summary>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterCriteria filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Field) || string.IsNullOrWhiteSpace(filter.Value))
            return query;

        var property = GetProperty<T>(filter.Field);
        if (property == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var propertyType = property.PropertyType;

        Expression? filterExpression = null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isNullable = propertyType != underlyingType;

        switch (filter.Operator)
        {
            case FilterOperator.Equals:
                filterExpression = CreateEqualsExpression(propertyAccess, filter.Value, underlyingType, isNullable);
                break;
            case FilterOperator.NotEquals:
                filterExpression = CreateNotEqualsExpression(propertyAccess, filter.Value, underlyingType, isNullable);
                break;
            case FilterOperator.Contains:
                if (underlyingType == typeof(string))
                    filterExpression = CreateContainsExpression(propertyAccess, filter.Value, isNullable);
                break;
            case FilterOperator.NotContains:
                if (underlyingType == typeof(string))
                    filterExpression = CreateNotContainsExpression(propertyAccess, filter.Value, isNullable);
                break;
            case FilterOperator.StartsWith:
                if (underlyingType == typeof(string))
                    filterExpression = CreateStartsWithExpression(propertyAccess, filter.Value, isNullable);
                break;
            case FilterOperator.EndsWith:
                if (underlyingType == typeof(string))
                    filterExpression = CreateEndsWithExpression(propertyAccess, filter.Value, isNullable);
                break;
            case FilterOperator.GreaterThan:
                filterExpression = CreateComparisonExpression(propertyAccess, filter.Value, underlyingType, ExpressionType.GreaterThan, isNullable);
                break;
            case FilterOperator.GreaterThanOrEqual:
                filterExpression = CreateComparisonExpression(propertyAccess, filter.Value, underlyingType, ExpressionType.GreaterThanOrEqual, isNullable);
                break;
            case FilterOperator.LessThan:
                filterExpression = CreateComparisonExpression(propertyAccess, filter.Value, underlyingType, ExpressionType.LessThan, isNullable);
                break;
            case FilterOperator.LessThanOrEqual:
                filterExpression = CreateComparisonExpression(propertyAccess, filter.Value, underlyingType, ExpressionType.LessThanOrEqual, isNullable);
                break;
            case FilterOperator.In:
                filterExpression = CreateInExpression(propertyAccess, filter.Value, underlyingType, isNullable);
                break;
            case FilterOperator.NotIn:
                filterExpression = CreateNotInExpression(propertyAccess, filter.Value, underlyingType, isNullable);
                break;
            case FilterOperator.IsNull:
                filterExpression = Expression.Equal(propertyAccess, Expression.Constant(null));
                break;
            case FilterOperator.IsNotNull:
                filterExpression = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                break;
        }

        if (filterExpression == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
        return query.Where(lambda);
    }

    private static Expression? CreateEqualsExpression(Expression propertyAccess, string value, Type propertyType, bool isNullable)
    {
        var constant = ConvertValue(value, propertyType);
        if (constant == null) return null;

        var equalsExpression = Expression.Equal(propertyAccess, constant);
        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, equalsExpression);
        }
        return equalsExpression;
    }

    private static Expression? CreateNotEqualsExpression(Expression propertyAccess, string value, Type propertyType, bool isNullable)
    {
        var constant = ConvertValue(value, propertyType);
        if (constant == null) return null;

        var notEqualsExpression = Expression.NotEqual(propertyAccess, constant);
        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, notEqualsExpression);
        }
        return notEqualsExpression;
    }

    private static Expression? CreateContainsExpression(Expression propertyAccess, string value, bool isNullable)
    {
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var constant = Expression.Constant(value.ToLower());
        var containsCall = Expression.Call(toLowerCall, containsMethod!, constant);

        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, containsCall);
        }
        return containsCall;
    }

    private static Expression? CreateNotContainsExpression(Expression propertyAccess, string value, bool isNullable)
    {
        var containsExpression = CreateContainsExpression(propertyAccess, value, isNullable);
        if (containsExpression == null) return null;
        return Expression.Not(containsExpression);
    }

    private static Expression? CreateStartsWithExpression(Expression propertyAccess, string value, bool isNullable)
    {
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
        var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        var constant = Expression.Constant(value.ToLower());
        var startsWithCall = Expression.Call(toLowerCall, startsWithMethod!, constant);

        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, startsWithCall);
        }
        return startsWithCall;
    }

    private static Expression? CreateEndsWithExpression(Expression propertyAccess, string value, bool isNullable)
    {
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
        var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        var constant = Expression.Constant(value.ToLower());
        var endsWithCall = Expression.Call(toLowerCall, endsWithMethod!, constant);

        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, endsWithCall);
        }
        return endsWithCall;
    }

    private static Expression? CreateComparisonExpression(Expression propertyAccess, string value, Type propertyType, ExpressionType comparisonType, bool isNullable)
    {
        if (!IsNumericType(propertyType) && propertyType != typeof(DateTime) && propertyType != typeof(DateTime?))
            return null;

        var constant = ConvertValue(value, propertyType);
        if (constant == null) return null;

        var comparison = Expression.MakeBinary(comparisonType, propertyAccess, constant);
        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, comparison);
        }
        return comparison;
    }

    private static Expression? CreateInExpression(Expression propertyAccess, string value, Type propertyType, bool isNullable)
    {
        var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length == 0) return null;

        var convertedValues = values.Select(v => ConvertValue(v, propertyType)).Where(v => v != null).ToList();
        if (convertedValues.Count == 0) return null;

        var listType = typeof(List<>).MakeGenericType(propertyType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");

        foreach (var convertedValue in convertedValues)
        {
            addMethod!.Invoke(list, new[] { convertedValue });
        }

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(propertyType);

        var constant = Expression.Constant(list);
        var containsCall = Expression.Call(containsMethod, constant, propertyAccess);

        if (isNullable)
        {
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            return Expression.AndAlso(nullCheck, containsCall);
        }
        return containsCall;
    }

    private static Expression? CreateNotInExpression(Expression propertyAccess, string value, Type propertyType, bool isNullable)
    {
        var inExpression = CreateInExpression(propertyAccess, value, propertyType, isNullable);
        if (inExpression == null) return null;
        return Expression.Not(inExpression);
    }

    private static ConstantExpression? ConvertValue(string value, Type targetType)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(string))
                return Expression.Constant(value);

            if (underlyingType == typeof(int) && int.TryParse(value, out var intValue))
                return Expression.Constant(intValue, targetType);

            if (underlyingType == typeof(long) && long.TryParse(value, out var longValue))
                return Expression.Constant(longValue, targetType);

            if (underlyingType == typeof(decimal) && decimal.TryParse(value, out var decimalValue))
                return Expression.Constant(decimalValue, targetType);

            if (underlyingType == typeof(double) && double.TryParse(value, out var doubleValue))
                return Expression.Constant(doubleValue, targetType);

            if (underlyingType == typeof(float) && float.TryParse(value, out var floatValue))
                return Expression.Constant(floatValue, targetType);

            if (underlyingType == typeof(bool) && bool.TryParse(value, out var boolValue))
                return Expression.Constant(boolValue, targetType);

            if (underlyingType == typeof(DateTime) && DateTime.TryParse(value, out var dateValue))
                return Expression.Constant(dateValue, targetType);

            if (underlyingType.IsEnum && Enum.TryParse(underlyingType, value, true, out var enumValue))
                return Expression.Constant(enumValue, targetType);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(decimal) ||
               type == typeof(double) || type == typeof(float) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
               type == typeof(ushort) || type == typeof(sbyte);
    }

    private static PropertyInfo? GetProperty<T>(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return null;

        var type = typeof(T);
        var property = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        // Try with different casing if not found
        if (property == null)
        {
            property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        }

        return property;
    }
}
