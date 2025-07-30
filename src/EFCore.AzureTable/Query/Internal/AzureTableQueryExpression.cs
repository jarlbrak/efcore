// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableQueryExpression : Expression
{
    private readonly List<(Expression Expression, string ODataFilter)> _filters = new();
    private Expression? _skip;
    private Expression? _take;
    private (Expression KeySelector, bool Ascending)? _ordering;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableQueryExpression(AzureTableExpression tableExpression)
    {
        TableExpression = tableExpression;
    }

    /// <summary>
    ///     The table being queried.
    /// </summary>
    public virtual AzureTableExpression TableExpression { get; }

    /// <summary>
    ///     The filters applied to the query.
    /// </summary>
    public virtual IReadOnlyList<(Expression Expression, string ODataFilter)> Filters => _filters;

    /// <summary>
    ///     The skip expression if any.
    /// </summary>
    public virtual Expression? Skip => _skip;

    /// <summary>
    ///     The take expression if any.
    /// </summary>
    public virtual Expression? Take => _take;

    /// <summary>
    ///     The ordering if any.  
    /// </summary>
    public virtual (Expression KeySelector, bool Ascending)? Ordering => _ordering;

    /// <summary>
    ///     The current parameter for this query expression (for shaper compilation).
    /// </summary>
    public virtual ParameterExpression? CurrentParameter { get; set; }

    /// <inheritdoc />
    public override Type Type => typeof(IQueryable<>).MakeGenericType(TableExpression.EntityType.ClrType);

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <inheritdoc />
    public override bool CanReduce => true;

    /// <inheritdoc />
    public override Expression Reduce()
    {
        // Reduce to a method call that can be compiled
        // This should never be called if VisitExtension handles it properly, but it's a safety net
        var queryMethodInfo = typeof(AzureTableQueryExpression).GetMethod(nameof(CreateQuery), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return Expression.Call(
            queryMethodInfo, 
            Expression.Constant(TableExpression.EntityType),
            Expression.Constant(TableExpression.TableName),
            Expression.Constant(GetCombinedODataFilter()));
    }

    private static IEnumerable<object> CreateQuery(IEntityType entityType, string tableName, string? filter)
    {
        // This is a placeholder that should never be called at runtime
        // If this is called, it means the expression wasn't properly handled by VisitExtension
        throw new InvalidOperationException("AzureTableQueryExpression was not properly handled during query compilation.");
    }

    /// <summary>
    ///     Applies a filter to the query.
    /// </summary>
    public virtual void ApplyFilter(Expression filterExpression, string odataFilter)
    {
        _filters.Add((filterExpression, odataFilter));
    }

    /// <summary>
    ///     Applies skip to the query.
    /// </summary>
    public virtual void ApplySkip(Expression skip)
    {
        _skip = skip;
    }

    /// <summary>
    ///     Applies take to the query.
    /// </summary>
    public virtual void ApplyTake(Expression take)
    {
        _take = take;
    }

    /// <summary>
    ///     Applies ordering to the query.
    /// </summary>
    public virtual void ApplyOrdering(Expression keySelector, bool ascending)
    {
        _ordering = (keySelector, ascending);
    }

    /// <summary>
    ///     Gets the combined OData filter string.
    /// </summary>
    public virtual string? GetCombinedODataFilter()
    {
        if (_filters.Count == 0)
        {
            return null;
        }

        if (_filters.Count == 1)
        {
            return _filters[0].ODataFilter;
        }

        // Combine multiple filters with AND
        return string.Join(" and ", _filters.Select(f => $"({f.ODataFilter})"));
    }

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var tableExpression = (AzureTableExpression)visitor.Visit(TableExpression);

        if (tableExpression != TableExpression)
        {
            var newQuery = new AzureTableQueryExpression(tableExpression);
            
            foreach (var filter in _filters)
            {
                newQuery._filters.Add(filter);
            }
            
            newQuery._skip = _skip;
            newQuery._take = _take;
            newQuery._ordering = _ordering;
            
            return newQuery;
        }

        return this;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string> { $"AzureTableQuery({TableExpression.TableName})" };

        if (_filters.Any())
        {
            parts.Add($"Filter: {GetCombinedODataFilter()}");
        }

        if (_ordering.HasValue)
        {
            parts.Add($"OrderBy: {_ordering.Value.KeySelector} {(_ordering.Value.Ascending ? "ASC" : "DESC")}");
        }

        if (_skip != null)
        {
            parts.Add($"Skip: {_skip}");
        }

        if (_take != null)
        {
            parts.Add($"Take: {_take}");
        }

        return string.Join(" | ", parts);
    }
}