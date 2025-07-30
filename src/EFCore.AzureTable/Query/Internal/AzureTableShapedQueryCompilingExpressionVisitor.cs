// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    private readonly Type _contextType;
    private readonly bool _threadSafetyChecksEnabled;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
        _contextType = queryCompilationContext.ContextType;
        _threadSafetyChecksEnabled = dependencies.CoreSingletonOptions.AreThreadSafetyChecksEnabled;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitExtension(Expression extensionExpression)
    {
        System.Console.WriteLine($"[DEBUG] VisitExtension called with: {extensionExpression.GetType().Name}");
        System.Console.WriteLine($"[DEBUG] Expression: {extensionExpression}");
        System.Console.WriteLine($"[DEBUG] NodeType: {extensionExpression.NodeType}");
        System.Console.WriteLine($"[DEBUG] CanReduce: {extensionExpression.CanReduce}");

        switch (extensionExpression)
        {
            case AzureTableExpression azureTableExpression:
                System.Console.WriteLine($"[DEBUG] Processing AzureTableExpression: {azureTableExpression.TableName}");
                var tableCall = Expression.Call(
                    TableMethodInfo,
                    QueryCompilationContext.QueryContextParameter,
                    Expression.Constant(azureTableExpression.EntityType),
                    Expression.Constant(azureTableExpression.TableName));
                System.Console.WriteLine($"[DEBUG] Generated table call: {tableCall}");
                return tableCall;

            case AzureTableQueryExpression azureTableQueryExpression:
                System.Console.WriteLine($"[DEBUG] Processing AzureTableQueryExpression: {azureTableQueryExpression.TableExpression.TableName}");
                System.Console.WriteLine($"[DEBUG] Pre-resolved filter: {azureTableQueryExpression.GetCombinedODataFilter()}");
                
                // CRITICAL FIX: Pass the full query expression so we can resolve parameters at execution time
                var queryCall = Expression.Call(
                    QueryWithExpressionMethodInfo,
                    QueryCompilationContext.QueryContextParameter,
                    Expression.Constant(azureTableQueryExpression));
                System.Console.WriteLine($"[DEBUG] Generated query call with expression: {queryCall}");
                return queryCall;

            case ConstantExpression constantExpression when constantExpression.Value is AzureTableQueryTranslationPreprocessor.QueryIdWrapper wrapper:
                System.Console.WriteLine($"[DEBUG] Processing QueryIdWrapper with ID: {wrapper.QueryId}");
                System.Console.WriteLine($"[DEBUG] Wrapped expression type: {wrapper.OriginalExpression.GetType().Name}");
                
                // Unwrap the original expression and process it with query ID context
                if (wrapper.OriginalExpression is AzureTableQueryExpression wrappedQueryExpression)
                {
                    // Create a call that includes the query ID for custom parameter resolution
                    var wrappedQueryCall = Expression.Call(
                        QueryWithQueryIdMethodInfo,
                        QueryCompilationContext.QueryContextParameter,
                        Expression.Constant(wrappedQueryExpression),
                        Expression.Constant(wrapper.QueryId));
                    System.Console.WriteLine($"[DEBUG] Generated wrapped query call with query ID: {wrappedQueryCall}");
                    return wrappedQueryCall;
                }
                else
                {
                    // Fall back to visiting the original expression
                    return Visit(wrapper.OriginalExpression);
                }
                
            case ConstantExpression constantExpression when constantExpression.Value != null:
                System.Console.WriteLine($"[DEBUG] Processing ConstantExpression with value type: {constantExpression.Value.GetType().Name}");
                System.Console.WriteLine($"[DEBUG] Constant value: {constantExpression.Value}");
                break;
        }

        System.Console.WriteLine($"[DEBUG] Delegating to base.VisitExtension for: {extensionExpression.GetType().Name}");
        return base.VisitExtension(extensionExpression);
    }

    private static int? GetTakeValue(Expression? takeExpression)
    {
        if (takeExpression is ConstantExpression constantExpression && constantExpression.Value is int takeValue)
        {
            return takeValue;
        }
        return null;
    }

    private static int? GetSkipValue(Expression? skipExpression)
    {
        if (skipExpression is ConstantExpression constantExpression && constantExpression.Value is int skipValue)
        {
            return skipValue;
        }
        return null;
    }

    private static string? GetOrderByColumn((Expression KeySelector, bool Ascending)? ordering)
    {
        if (ordering?.KeySelector is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        return null;
    }

    private static bool? GetOrderByAscending((Expression KeySelector, bool Ascending)? ordering)
    {
        return ordering?.Ascending;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        System.Console.WriteLine($"[DEBUG] VisitShapedQuery called");
        System.Console.WriteLine($"[DEBUG] Query expression type: {shapedQueryExpression.QueryExpression.GetType().Name}");
        System.Console.WriteLine($"[DEBUG] Shaper expression type: {shapedQueryExpression.ShaperExpression.GetType().Name}");
        System.Console.WriteLine($"[DEBUG] Shaper expression: {shapedQueryExpression.ShaperExpression}");

        var azureTableQueryExpression = (AzureTableQueryExpression)shapedQueryExpression.QueryExpression;
        
        // Process the shaper expression to reduce all extension expressions
        System.Console.WriteLine($"[DEBUG] Creating ShaperExpressionProcessingExpressionVisitor");
        var shaperExpressionProcessor = new ShaperExpressionProcessingExpressionVisitor(
            this, azureTableQueryExpression, QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll, QueryCompilationContext.QueryContextParameter);
        
        System.Console.WriteLine($"[DEBUG] Processing shaper expression");
        var shaperLambda = shaperExpressionProcessor.ProcessShaper(shapedQueryExpression.ShaperExpression);
        System.Console.WriteLine($"[DEBUG] Shaper lambda: {shaperLambda}");
        System.Console.WriteLine($"[DEBUG] Shaper lambda return type: {shaperLambda.ReturnType}");
        
        // Visit the query expression to get the data source
        System.Console.WriteLine($"[DEBUG] Visiting query expression");
        var innerEnumerable = Visit(azureTableQueryExpression);
        System.Console.WriteLine($"[DEBUG] Inner enumerable: {innerEnumerable}");

        // The shaper lambda takes a ValueBuffer and returns the shaped entity
        System.Console.WriteLine($"[DEBUG] Creating AzureTableQueryingEnumerable");
        try
        {
            System.Console.WriteLine($"[DEBUG] About to compile shaper lambda");
            var compiledShaper = shaperLambda.Compile();
            System.Console.WriteLine($"[DEBUG] Shaper lambda compiled successfully");
            System.Console.WriteLine($"[DEBUG] Compiled shaper type: {compiledShaper.GetType()}");
            
            // CRITICAL FIX: The constructor expects Func<ValueBuffer, T>, but our lambda is Func<QueryContext, ValueBuffer, T>
            // We need to create a wrapper that adapts the signature
            System.Console.WriteLine($"[DEBUG] Creating shaper wrapper to match constructor signature");
            
            // The AzureTableQueryingEnumerable will provide the QueryContext at runtime,
            // so we need to create a wrapper that can be called with just the ValueBuffer
            // and will get the QueryContext from the enumerable itself
            
            System.Console.WriteLine($"[DEBUG] Creating AzureTableQueryingEnumerable with:");
            System.Console.WriteLine($"[DEBUG] - QueryContextParameter: {QueryCompilationContext.QueryContextParameter}");
            System.Console.WriteLine($"[DEBUG] - InnerEnumerable: {innerEnumerable}");
            System.Console.WriteLine($"[DEBUG] - CompiledShaper: {compiledShaper}");
            System.Console.WriteLine($"[DEBUG] - ContextType: {_contextType}");
            System.Console.WriteLine($"[DEBUG] - ThreadSafetyChecksEnabled: {_threadSafetyChecksEnabled}");
            
            // Pass the full 2-parameter shaper (QueryContext, ValueBuffer) -> T
            // The AzureTableQueryingEnumerable will handle calling it with the correct parameters
            return Expression.New(
                typeof(AzureTableQueryingEnumerable<>).MakeGenericType(shaperLambda.ReturnType).GetConstructors()[0],
                QueryCompilationContext.QueryContextParameter,
                innerEnumerable,
                Expression.Constant(compiledShaper),
                Expression.Constant(_contextType),
                Expression.Constant(QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution),
                Expression.Constant(_threadSafetyChecksEnabled));
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] Failed to compile shaper lambda: {ex.Message}");
            System.Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
            System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            System.Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException?.Message}");
            throw;
        }
    }

    private static readonly MethodInfo TableMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(Table))!;

    private static readonly MethodInfo QueryMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(Query))!;

    private static readonly MethodInfo QueryWithExpressionMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(QueryWithExpression))!;

    private static readonly MethodInfo QueryWithQueryIdMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(QueryWithQueryId))!;

    private static IEnumerable<ValueBuffer> Table(
        QueryContext queryContext,
        IEntityType entityType,
        string tableName)
        => Query(queryContext, entityType, tableName, null, null, null, null, null);

    /// <summary>
    /// CRITICAL FIX: New method that accepts the full query expression and resolves parameters at execution time
    /// Now supports both standard EF Core parameters and custom-extracted parameters
    /// </summary>
    private static IEnumerable<ValueBuffer> QueryWithExpression(
        QueryContext queryContext,
        AzureTableQueryExpression queryExpression)
    {
        System.Console.WriteLine($"[EXECUTION] QueryWithExpression called");
        System.Console.WriteLine($"[EXECUTION] QueryContext available: {queryContext != null}");
        System.Console.WriteLine($"[EXECUTION] ParameterValues count: {queryContext?.ParameterValues?.Count ?? 0}");
        
        if (queryContext?.ParameterValues != null)
        {
            foreach (var param in queryContext.ParameterValues)
            {
                System.Console.WriteLine($"[EXECUTION] Parameter: {param.Key} = {param.Value} (Type: {param.Value?.GetType().Name ?? "null"})");
            }
        }

        // Check if this query has a wrapped query ID for custom parameters
        int? queryId = null;
        var actualQueryExpression = queryExpression;
        
        // Check if the query expression might contain a QueryIdWrapper
        if (queryExpression.ToString().Contains("QueryId("))
        {
            System.Console.WriteLine($"[EXECUTION] Potential QueryIdWrapper detected in expression");
            // We'll need to extract this information during compilation, for now log it
        }

        // CRITICAL FIX: Resolve parameters in filter expressions using AzureTableParameterInliner
        var resolvedFilters = new List<string>();
        
        foreach (var (filterExpression, odataFilter) in actualQueryExpression.Filters)
        {
            System.Console.WriteLine($"[EXECUTION] Processing filter expression: {filterExpression}");
            System.Console.WriteLine($"[EXECUTION] Pre-resolved OData filter: '{odataFilter}'");
            
            // If OData filter is empty, it means we need to resolve parameters and generate it
            if (string.IsNullOrEmpty(odataFilter))
            {
                System.Console.WriteLine($"[EXECUTION] Empty OData filter - resolving parameters and generating OData");
                
                // Step 1: Resolve parameters in the expression with both standard and custom parameters
                var parameterInliner = new AzureTableParameterInliner(
                    queryContext?.ParameterValues ?? new Dictionary<string, object?>(), 
                    queryId);
                var resolvedExpression = parameterInliner.Visit(filterExpression);
                
                System.Console.WriteLine($"[EXECUTION] After parameter resolution: {resolvedExpression}");
                
                // Step 2: Generate OData filter from resolved expression
                var translator = new ODataFilterTranslator(actualQueryExpression.TableExpression.EntityType.Model);
                var resolvedODataFilter = translator.Translate(resolvedExpression);
                
                System.Console.WriteLine($"[EXECUTION] Generated OData filter: '{resolvedODataFilter}'");
                
                if (!string.IsNullOrEmpty(resolvedODataFilter))
                {
                    resolvedFilters.Add(resolvedODataFilter);
                }
            }
            else
            {
                // Use pre-resolved filter
                resolvedFilters.Add(odataFilter);
            }
        }
        
        // Combine all resolved filters
        string? finalFilter = null;
        if (resolvedFilters.Count == 1)
        {
            finalFilter = resolvedFilters[0];
        }
        else if (resolvedFilters.Count > 1)
        {
            finalFilter = string.Join(" and ", resolvedFilters.Select(f => $"({f})"));
        }
        
        System.Console.WriteLine($"[EXECUTION] Final resolved filter: '{finalFilter}'");
        
        // Call the original Query method with resolved parameters
        if (queryContext == null)
        {
            throw new ArgumentNullException(nameof(queryContext), "QueryContext cannot be null during parameter resolution");
        }
        
        return Query(
            queryContext!,
            queryExpression.TableExpression.EntityType,
            queryExpression.TableExpression.TableName,
            finalFilter,
            GetTakeValue(queryExpression.Take),
            GetSkipValue(queryExpression.Skip),
            GetOrderByColumn(queryExpression.Ordering),
            GetOrderByAscending(queryExpression.Ordering));
    }

    /// <summary>
    /// CRITICAL FIX: Query method that includes query ID for custom parameter resolution
    /// </summary>
    private static IEnumerable<ValueBuffer> QueryWithQueryId(
        QueryContext queryContext,
        AzureTableQueryExpression queryExpression,
        int queryId)
    {
        System.Console.WriteLine($"[EXECUTION] QueryWithQueryId called with query ID: {queryId}");
        System.Console.WriteLine($"[EXECUTION] QueryContext available: {queryContext != null}");
        System.Console.WriteLine($"[EXECUTION] ParameterValues count: {queryContext?.ParameterValues?.Count ?? 0}");
        
        if (queryContext?.ParameterValues != null)
        {
            foreach (var param in queryContext.ParameterValues)
            {
                System.Console.WriteLine($"[EXECUTION] Parameter: {param.Key} = {param.Value} (Type: {param.Value?.GetType().Name ?? "null"})");
            }
        }

        // CRITICAL FIX: Resolve parameters in filter expressions using both standard and custom parameters
        var resolvedFilters = new List<string>();
        
        foreach (var (filterExpression, odataFilter) in queryExpression.Filters)
        {
            System.Console.WriteLine($"[EXECUTION] Processing filter expression: {filterExpression}");
            System.Console.WriteLine($"[EXECUTION] Pre-resolved OData filter: '{odataFilter}'");
            
            // If OData filter is empty, it means we need to resolve parameters and generate it
            if (string.IsNullOrEmpty(odataFilter))
            {
                System.Console.WriteLine($"[EXECUTION] Empty OData filter - resolving parameters and generating OData");
                
                // Step 1: Resolve parameters in the expression with custom parameter support
                var parameterInliner = new AzureTableParameterInliner(
                    queryContext?.ParameterValues ?? new Dictionary<string, object?>(), 
                    queryId);
                var resolvedExpression = parameterInliner.Visit(filterExpression);
                
                System.Console.WriteLine($"[EXECUTION] After parameter resolution: {resolvedExpression}");
                
                // Step 2: Generate OData filter from resolved expression
                var translator = new ODataFilterTranslator(queryExpression.TableExpression.EntityType.Model);
                var resolvedODataFilter = translator.Translate(resolvedExpression);
                
                System.Console.WriteLine($"[EXECUTION] Generated OData filter: '{resolvedODataFilter}'");
                
                if (!string.IsNullOrEmpty(resolvedODataFilter))
                {
                    resolvedFilters.Add(resolvedODataFilter);
                }
            }
            else
            {
                // Use pre-resolved filter
                resolvedFilters.Add(odataFilter);
            }
        }
        
        // Combine all resolved filters
        string? finalFilter = null;
        if (resolvedFilters.Count == 1)
        {
            finalFilter = resolvedFilters[0];
        }
        else if (resolvedFilters.Count > 1)
        {
            finalFilter = string.Join(" and ", resolvedFilters.Select(f => $"({f})"));
        }
        
        System.Console.WriteLine($"[EXECUTION] Final resolved filter: '{finalFilter}'");
        
        // Call the original Query method with resolved parameters
        if (queryContext == null)
        {
            throw new ArgumentNullException(nameof(queryContext), "QueryContext cannot be null during parameter resolution");
        }
        
        return Query(
            queryContext!,
            queryExpression.TableExpression.EntityType,
            queryExpression.TableExpression.TableName,
            finalFilter,
            GetTakeValue(queryExpression.Take),
            GetSkipValue(queryExpression.Skip),
            GetOrderByColumn(queryExpression.Ordering),
            GetOrderByAscending(queryExpression.Ordering));
    }

    private static IEnumerable<ValueBuffer> Query(
        QueryContext queryContext,
        IEntityType entityType,
        string tableName,
        string? filter,
        int? take,
        int? skip,
        string? orderBy,
        bool? ascending)
    {
        System.Console.WriteLine($"[DEBUG] Query method called");
        System.Console.WriteLine($"[DEBUG] queryContext: {queryContext}");
        System.Console.WriteLine($"[DEBUG] entityType: {entityType}");
        System.Console.WriteLine($"[DEBUG] tableName: {tableName}");
        System.Console.WriteLine($"[DEBUG] filter: {filter}");
        System.Console.WriteLine($"[DEBUG] take: {take}");
        System.Console.WriteLine($"[DEBUG] skip: {skip}");
        System.Console.WriteLine($"[DEBUG] orderBy: {orderBy}");
        System.Console.WriteLine($"[DEBUG] ascending: {ascending}");

        if (queryContext == null) throw new ArgumentNullException(nameof(queryContext));
        if (entityType == null) throw new ArgumentNullException(nameof(entityType));
        if (tableName == null) throw new ArgumentNullException(nameof(tableName));

        var azureTableQueryContext = (AzureTableQueryContext)queryContext;
        var clientWrapper = queryContext.Context.GetService<IAzureTableClientWrapper>();
        if (clientWrapper == null) throw new InvalidOperationException("IAzureTableClientWrapper service not found");
        
        var tableClient = clientWrapper.GetTableClient(tableName);
        if (tableClient == null) throw new InvalidOperationException($"Failed to get table client for table: {tableName}");

        // Execute the query using Azure Table SDK
        IEnumerable<TableEntity> entities;
        
        if (!string.IsNullOrEmpty(filter))
        {
            // Use the filter directly - Azure SDK handles the OData formatting
            entities = tableClient.Query<TableEntity>(filter: filter, maxPerPage: take);
        }
        else
        {
            // Query all entities if no filter
            entities = tableClient.Query<TableEntity>(maxPerPage: take);
        }

        var properties = entityType.GetProperties().ToList();
        var valueBuffers = new List<ValueBuffer>();

        var entityList = entities.ToList();

        // Handle skip on client side since Azure Table doesn't support it natively in all cases
        if (skip.HasValue && skip.Value > 0)
        {
            entityList = entityList.Skip(skip.Value).ToList();
        }

        foreach (var entity in entityList)
        {
            var values = new object?[properties.Count];
            
            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                var propertyName = property.Name;
                
                if (entity.TryGetValue(propertyName, out var value))
                {
                    values[i] = ConvertValue(value, property.ClrType);
                }
                else
                {
                    values[i] = property.ClrType.GetDefaultValue();
                }
            }

            valueBuffers.Add(new ValueBuffer(values));
        }

        return valueBuffers;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.GetDefaultValue();
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            if (value == null)
            {
                return null;
            }
            targetType = underlyingType;
        }

        // Convert value to target type
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return targetType.GetDefaultValue();
        }
    }
}

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableQueryingEnumerable<T> : IEnumerable<T>, IAsyncEnumerable<T>
{
    private readonly QueryContext _queryContext;
    private readonly IEnumerable<ValueBuffer> _innerEnumerable;
    private readonly Func<QueryContext, ValueBuffer, T> _shaper;
    private readonly Type _contextType;
    private readonly bool _standAloneStateManager;
    private readonly bool _threadSafetyChecksEnabled;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableQueryingEnumerable(
        QueryContext queryContext,
        IEnumerable<ValueBuffer> innerEnumerable,
        Func<QueryContext, ValueBuffer, T> shaper,
        Type contextType,
        bool standAloneStateManager,
        bool threadSafetyChecksEnabled)
    {
        _queryContext = queryContext;
        _innerEnumerable = innerEnumerable;
        _shaper = shaper;
        _contextType = contextType;
        _standAloneStateManager = standAloneStateManager;
        _threadSafetyChecksEnabled = threadSafetyChecksEnabled;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator(this, cancellationToken);

    private sealed class Enumerator : IEnumerator<T>
    {
        private readonly AzureTableQueryingEnumerable<T> _queryingEnumerable;
        private IEnumerator<ValueBuffer> _enumerator;

        public Enumerator(AzureTableQueryingEnumerable<T> queryingEnumerable)
        {
            _queryingEnumerable = queryingEnumerable;
            _enumerator = _queryingEnumerable._innerEnumerable.GetEnumerator();
        }

        public T Current { get; private set; } = default!;

        object? System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_enumerator.MoveNext())
            {
                System.Console.WriteLine($"[DEBUG] Enumerator.MoveNext - About to call shaper with ValueBuffer: {_enumerator.Current}");
                System.Console.WriteLine($"[DEBUG] ValueBuffer count: {_enumerator.Current.Count}");
                
                try
                {
                    Current = _queryingEnumerable._shaper(_queryingEnumerable._queryContext, _enumerator.Current);
                    System.Console.WriteLine($"[DEBUG] Shaper call succeeded, result: {Current}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ERROR] Shaper call failed: {ex.Message}");
                    System.Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                    System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                    throw;
                }
                return true;
            }

            return false;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
            _enumerator?.Dispose();
        }
    }

    private sealed class AsyncEnumerator : IAsyncEnumerator<T>
    {
        private readonly AzureTableQueryingEnumerable<T> _queryingEnumerable;
        private readonly CancellationToken _cancellationToken;
        private IEnumerator<ValueBuffer> _enumerator;

        public AsyncEnumerator(AzureTableQueryingEnumerable<T> queryingEnumerable, CancellationToken cancellationToken)
        {
            _queryingEnumerable = queryingEnumerable;
            _cancellationToken = cancellationToken;
            _enumerator = _queryingEnumerable._innerEnumerable.GetEnumerator();
        }

        public T Current { get; private set; } = default!;

        public ValueTask<bool> MoveNextAsync()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_enumerator.MoveNext())
            {
                System.Console.WriteLine($"[DEBUG] AsyncEnumerator.MoveNextAsync - About to call shaper with ValueBuffer: {_enumerator.Current}");
                System.Console.WriteLine($"[DEBUG] ValueBuffer count: {_enumerator.Current.Count}");
                
                try
                {
                    Current = _queryingEnumerable._shaper(_queryingEnumerable._queryContext, _enumerator.Current);
                    System.Console.WriteLine($"[DEBUG] Async shaper call succeeded, result: {Current}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ERROR] Async shaper call failed: {ex.Message}");
                    System.Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                    System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                    throw;
                }
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public ValueTask DisposeAsync()
        {
            _enumerator?.Dispose();
            return default;
        }
    }
}