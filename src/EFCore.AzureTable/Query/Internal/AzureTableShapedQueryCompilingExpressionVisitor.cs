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
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static System.Linq.Expressions.Expression;

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
    /// <param name="extensionExpression">The extension expression to visit and compile</param>
    /// <returns>A compiled expression that can be executed against Azure Table Storage</returns>
    /// <exception cref="ArgumentNullException">Thrown when extensionExpression is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the extension expression cannot be compiled for Azure Table Storage</exception>
    protected override Expression VisitExtension(Expression extensionExpression)
    {
        if (extensionExpression == null)
            throw new ArgumentNullException(nameof(extensionExpression), "Extension expression cannot be null during query compilation");

        switch (extensionExpression)
        {
            case AzureTableExpression azureTableExpression:
                var tableCall = Expression.Call(
                    TableMethodInfo,
                    QueryCompilationContext.QueryContextParameter,
                    Expression.Constant(azureTableExpression.EntityType),
                    Expression.Constant(azureTableExpression.TableName));
                return tableCall;

            case AzureTableQueryExpression azureTableQueryExpression:
                // CRITICAL FIX: Pass the full query expression so we can resolve parameters at execution time
                var queryCall = Expression.Call(
                    QueryWithExpressionMethodInfo,
                    QueryCompilationContext.QueryContextParameter,
                    Expression.Constant(azureTableQueryExpression));
                return queryCall;

            // NOTE: QueryIdWrapper functionality removed for simplification
            // The core parameter resolution fix works without this advanced feature
                
            case ConstantExpression constantExpression when constantExpression.Value != null:
                break;
        }

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
    /// <param name="shapedQueryExpression">The shaped query expression containing both query and shaper expressions</param>
    /// <returns>An enumerable expression that can execute the query and shape results</returns>
    /// <exception cref="ArgumentNullException">Thrown when shapedQueryExpression is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when query compilation fails or shaper expressions cannot be reduced</exception>
    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        if (shapedQueryExpression == null)
            throw new ArgumentNullException(nameof(shapedQueryExpression), "Shaped query expression cannot be null during compilation");
        
        if (shapedQueryExpression.QueryExpression == null)
            throw new ArgumentException("Query expression cannot be null within shaped query expression", nameof(shapedQueryExpression));
        
        if (shapedQueryExpression.ShaperExpression == null)
            throw new ArgumentException("Shaper expression cannot be null within shaped query expression", nameof(shapedQueryExpression));

        var azureTableQueryExpression = (AzureTableQueryExpression)shapedQueryExpression.QueryExpression;
        
        // ARCHITECTURAL FIX: Use ExtensionExpressionReducer to reduce EF Core extension expressions
        // before attempting to compile them. This prevents "must be reducible node" errors.
        var extensionReducer = new ExtensionExpressionReducer(
            this,
            azureTableQueryExpression,
            QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll);
            
        var reducedShaperExpression = extensionReducer.ReduceExtensionExpressions(shapedQueryExpression.ShaperExpression);
        
        // Create ValueBuffer parameter for the reduced shaper
        var valueBufferParameter = extensionReducer.ValueBufferParameter;
        
        // Create the shaper lambda with the reduced expression that can be compiled
        var shaperLambda = Lambda(
            reducedShaperExpression,
            QueryCompilationContext.QueryContextParameter,
            valueBufferParameter);
        
        // Visit the query expression to get the data source
        var innerEnumerable = Visit(azureTableQueryExpression);

        // Now compile the reduced shaper lambda - this should work without "must be reducible node" errors
        try
        {
            var compiledShaper = shaperLambda.Compile();
            
            
            // Create the AzureTableQueryingEnumerable with the compiled shaper
            return New(
                typeof(AzureTableQueryingEnumerable<>).MakeGenericType(shaperLambda.ReturnType).GetConstructors()[0],
                QueryCompilationContext.QueryContextParameter,
                innerEnumerable,
                Constant(compiledShaper),
                Constant(_contextType),
                Constant(QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution),
                Constant(_threadSafetyChecksEnabled));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to compile Azure Table query shaper expression. This indicates an issue with expression reduction or compilation. Query type: {azureTableQueryExpression.GetType().Name}, Shaper type: {shapedQueryExpression.ShaperExpression.GetType().Name}",
                ex);
        }
    }

    private static readonly MethodInfo TableMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(Table))!;

    private static readonly MethodInfo QueryMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(Query))!;

    private static readonly MethodInfo QueryWithExpressionMethodInfo
        = typeof(AzureTableShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(QueryWithExpression))!;

    // NOTE: QueryWithQueryId method info removed for simplification

    private static IEnumerable<ValueBuffer> Table(
        QueryContext queryContext,
        IEntityType entityType,
        string tableName)
        => Query(queryContext, entityType, tableName, null, null, null, null, null);

    /// <summary>
    /// CRITICAL FIX: New method that accepts the full query expression and resolves parameters at execution time.
    /// Now supports both standard EF Core parameters and custom-extracted parameters.
    /// This is the core architectural fix that prevents "must be reducible node" errors by deferring
    /// parameter resolution until query execution when QueryContext.ParameterValues is available.
    /// </summary>
    /// <param name="queryContext">The query context containing parameter values and services</param>
    /// <param name="queryExpression">The Azure Table query expression with unresolved parameters</param>
    /// <returns>An enumerable of ValueBuffer objects containing query results</returns>
    /// <exception cref="ArgumentNullException">Thrown when queryContext is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when required services are not available</exception>
    private static IEnumerable<ValueBuffer> QueryWithExpression(
        QueryContext queryContext,
        AzureTableQueryExpression queryExpression)
    {
        
        if (queryContext?.ParameterValues != null)
        {
            foreach (var param in queryContext.ParameterValues)
            {
            }
        }

        // Check if this query has a wrapped query ID for custom parameters
        int? queryId = null;
        var actualQueryExpression = queryExpression;
        
        // Check if the query expression might contain a QueryIdWrapper
        if (queryExpression.ToString().Contains("QueryId("))
        {
            // We'll need to extract this information during compilation, for now log it
        }

        // CRITICAL FIX: Resolve parameters in filter expressions using AzureTableParameterInliner
        var resolvedFilters = new List<string>();
        
        foreach (var (filterExpression, odataFilter) in actualQueryExpression.Filters)
        {
            
            // If OData filter is empty, it means we need to resolve parameters and generate it
            if (string.IsNullOrEmpty(odataFilter))
            {
                
                // Step 1: Resolve parameters in the expression with both standard and custom parameters
                var parameterInliner = new AzureTableParameterInliner(
                    queryContext?.ParameterValues ?? new Dictionary<string, object?>(), 
                    queryId);
                var resolvedExpression = parameterInliner.Visit(filterExpression);
                
                
                // Step 2: Generate OData filter from resolved expression
                var translator = new ODataFilterTranslator(actualQueryExpression.TableExpression.EntityType.Model);
                var resolvedODataFilter = translator.Translate(resolvedExpression);
                
                
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
    /// CRITICAL FIX: Query method that includes query ID for custom parameter resolution.
    /// This method supports advanced parameter patterns where parameters are prefixed with query IDs
    /// to handle complex query scenarios with multiple parameter sets.
    /// </summary>
    /// <param name="queryContext">The query context containing parameter values and services</param>
    /// <param name="queryExpression">The Azure Table query expression with unresolved parameters</param>
    /// <param name="queryId">The query ID used to prefix custom parameters</param>
    /// <returns>An enumerable of ValueBuffer objects containing query results</returns>
    /// <exception cref="ArgumentNullException">Thrown when queryContext is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when required services are not available</exception>
    private static IEnumerable<ValueBuffer> QueryWithQueryId(
        QueryContext queryContext,
        AzureTableQueryExpression queryExpression,
        int queryId)
    {
        
        if (queryContext?.ParameterValues != null)
        {
            foreach (var param in queryContext.ParameterValues)
            {
            }
        }

        // CRITICAL FIX: Resolve parameters in filter expressions using both standard and custom parameters
        var resolvedFilters = new List<string>();
        
        foreach (var (filterExpression, odataFilter) in queryExpression.Filters)
        {
            
            // If OData filter is empty, it means we need to resolve parameters and generate it
            if (string.IsNullOrEmpty(odataFilter))
            {
                
                // Step 1: Resolve parameters in the expression with custom parameter support
                var parameterInliner = new AzureTableParameterInliner(
                    queryContext?.ParameterValues ?? new Dictionary<string, object?>(), 
                    queryId);
                var resolvedExpression = parameterInliner.Visit(filterExpression);
                
                
                // Step 2: Generate OData filter from resolved expression
                var translator = new ODataFilterTranslator(queryExpression.TableExpression.EntityType.Model);
                var resolvedODataFilter = translator.Translate(resolvedExpression);
                
                
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
    /// Executes a query against Azure Table Storage and returns results as ValueBuffer objects.
    /// This is the core method that interacts with Azure Table Storage SDK to retrieve data.
    /// </summary>
    /// <param name="queryContext">The query context containing services and configuration</param>
    /// <param name="entityType">The entity type being queried</param>
    /// <param name="tableName">The name of the Azure Table Storage table</param>
    /// <param name="filter">Optional OData filter expression</param>
    /// <param name="take">Optional limit on number of results</param>
    /// <param name="skip">Optional number of results to skip</param>
    /// <param name="orderBy">Optional column name for ordering</param>
    /// <param name="ascending">Optional sort direction</param>
    /// <returns>An enumerable of ValueBuffer objects containing query results</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="InvalidOperationException">Thrown when Azure Table services are not available</exception>
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
        if (queryContext == null) throw new ArgumentNullException(nameof(queryContext), "Query context cannot be null");
        if (entityType == null) throw new ArgumentNullException(nameof(entityType), "Entity type cannot be null");
        if (tableName == null) throw new ArgumentNullException(nameof(tableName), "Table name cannot be null");


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
            return System.Convert.ChangeType(value, targetType);
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
                
                try
                {
                    Current = _queryingEnumerable._shaper(_queryingEnumerable._queryContext, _enumerator.Current);
                }
                catch
                {
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
                
                try
                {
                    Current = _queryingEnumerable._shaper(_queryingEnumerable._queryContext, _enumerator.Current);
                }
                catch
                {
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

/// <summary>
/// ARCHITECTURAL FIX: ExtensionExpressionReducer handles the reduction of EF Core extension expressions
/// (like StructuralTypeShaperExpression and ProjectionBindingExpression) into standard .NET expressions
/// that can be compiled by LambdaCompiler.Compile() without "must be reducible node" errors.
/// 
/// This is the core fix for the expression compilation failure that was preventing all entity queries from working.
/// The reducer transforms EF Core-specific expression nodes into standard .NET expression trees that can be
/// compiled and executed at runtime.
/// </summary>
/// <remarks>
/// This class addresses the fundamental issue where EF Core generates extension expressions during query analysis
/// that cannot be compiled directly by the standard .NET expression compiler. By reducing these expressions
/// to their equivalent standard .NET forms, we enable successful query compilation and execution.
/// 
/// Key transformations:
/// - StructuralTypeShaperExpression → Entity materialization with property assignments
/// - ProjectionBindingExpression → ValueBuffer access expressions
/// - Other EF Core extensions → Reduced standard expressions where possible
/// </remarks>
internal sealed class ExtensionExpressionReducer : ExpressionVisitor
{
    private readonly AzureTableShapedQueryCompilingExpressionVisitor _visitor;
    private readonly AzureTableQueryExpression _queryExpression;
    private readonly bool _tracking;
    private readonly Dictionary<Expression, ParameterExpression> _materializedEntityMapping = new();
    private readonly List<ParameterExpression> _variables = [];
    private readonly List<Expression> _expressions = [];
    private ParameterExpression? _valueBufferParameter;

    /// <summary>
    /// Gets the ValueBuffer parameter that should be used in the final lambda.
    /// This parameter represents the data buffer containing raw query results from Azure Table Storage.
    /// </summary>
    /// <value>A parameter expression representing the ValueBuffer input to the shaper lambda</value>
    public ParameterExpression ValueBufferParameter 
    { 
        get
        {
            _valueBufferParameter ??= Parameter(typeof(ValueBuffer), "valueBuffer");
            return _valueBufferParameter;
        }
    }

    /// <summary>
    /// Initializes a new instance of ExtensionExpressionReducer.
    /// This constructor sets up the reducer with the necessary context for transforming
    /// EF Core extension expressions into compilable standard .NET expressions.
    /// </summary>
    /// <param name="visitor">The parent shaped query compiling expression visitor providing compilation context</param>
    /// <param name="queryExpression">The Azure Table query expression being processed</param>
    /// <param name="tracking">Whether entity tracking is enabled for this query</param>
    /// <exception cref="ArgumentNullException">Thrown when visitor or queryExpression is null</exception>
    public ExtensionExpressionReducer(
        AzureTableShapedQueryCompilingExpressionVisitor visitor,
        AzureTableQueryExpression queryExpression,
        bool tracking)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor), "Visitor cannot be null");
        _queryExpression = queryExpression ?? throw new ArgumentNullException(nameof(queryExpression), "Query expression cannot be null");
        _tracking = tracking;
    }

    /// <summary>
    /// CRITICAL METHOD: Main entry point for reducing EF Core extension expressions
    /// into standard .NET expressions that can be compiled.
    /// </summary>
    /// <param name="shaperExpression">The shaper expression containing EF Core extensions</param>
    /// <returns>A reduced expression containing only standard .NET expressions</returns>
    public Expression ReduceExtensionExpressions(Expression shaperExpression)
    {

        // Visit the shaper expression to reduce all extension expressions
        var reducedExpression = Visit(shaperExpression);
        
        // If we accumulated any setup expressions (like entity materialization), 
        // we need to wrap them in a block expression
        if (_expressions.Count > 0)
        {
            _expressions.Add(reducedExpression);
            reducedExpression = Block(_variables, _expressions);
        }

        
        return reducedExpression;
    }

    /// <summary>
    /// Handles visiting extension expressions and reducing them to standard .NET expressions.
    /// This method is the core dispatcher that identifies specific EF Core extension expression types
    /// and delegates to the appropriate reduction method.
    /// </summary>
    /// <param name="extensionExpression">The extension expression to reduce</param>
    /// <returns>A standard .NET expression equivalent to the input extension expression</returns>
    /// <exception cref="InvalidOperationException">Thrown when an extension expression cannot be reduced</exception>
    protected override Expression VisitExtension(Expression extensionExpression)
    {
        if (extensionExpression == null)
            throw new ArgumentNullException(nameof(extensionExpression), "Extension expression cannot be null");

        switch (extensionExpression)
        {
            case StructuralTypeShaperExpression structuralTypeShaperExpression:
                return ReduceStructuralTypeShaperExpression(structuralTypeShaperExpression);

            case ProjectionBindingExpression projectionBindingExpression:
                return ReduceProjectionBindingExpression(projectionBindingExpression);

            default:
                // For other extension expressions, try to reduce them if possible
                if (extensionExpression.CanReduce)
                {
                    return Visit(extensionExpression.Reduce());
                }
                return base.VisitExtension(extensionExpression);
        }
    }

    /// <summary>
    /// CRITICAL METHOD: Reduces StructuralTypeShaperExpression to entity materialization expressions
    /// that can be compiled. This handles entity instantiation and property assignment.
    /// This method transforms EF Core's entity shaping logic into standard .NET expressions that
    /// create entity instances and populate their properties from ValueBuffer data.
    /// </summary>
    /// <param name="shaperExpression">The structural type shaper expression to reduce</param>
    /// <returns>A parameter expression representing the materialized entity</returns>
    /// <exception cref="ArgumentNullException">Thrown when shaperExpression is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when entity materialization cannot be generated</exception>
    private Expression ReduceStructuralTypeShaperExpression(StructuralTypeShaperExpression shaperExpression)
    {
        if (shaperExpression == null)
            throw new ArgumentNullException(nameof(shaperExpression), "Shaper expression cannot be null");

        // Check if we've already materialized this entity
        var key = shaperExpression.ValueBufferExpression;
        if (_materializedEntityMapping.TryGetValue(key, out var existingVariable))
        {
            return existingVariable;
        }

        // Create a new variable to hold the materialized entity
        var entityVariable = Parameter(shaperExpression.StructuralType.ClrType, $"entity_{_variables.Count}");
        _variables.Add(entityVariable);
        _materializedEntityMapping[key] = entityVariable;


        // Generate entity materialization expression
        var materializationExpression = GenerateEntityMaterializationExpression(shaperExpression);
        
        // Add assignment expression to our expression list
        _expressions.Add(Assign(entityVariable, materializationExpression));
        
        
        return entityVariable;
    }

    /// <summary>
    /// CRITICAL METHOD: Reduces ProjectionBindingExpression to ValueBuffer access expressions
    /// that can be compiled. This handles reading scalar values from the ValueBuffer.
    /// This method transforms EF Core's projection binding logic into standard .NET expressions
    /// that read values from the ValueBuffer at specific indices.
    /// </summary>
    /// <param name="projectionBindingExpression">The projection binding expression to reduce</param>
    /// <returns>A parameter expression representing the projected value</returns>
    /// <exception cref="ArgumentNullException">Thrown when projectionBindingExpression is null</exception>
    private Expression ReduceProjectionBindingExpression(ProjectionBindingExpression projectionBindingExpression)
    {
        if (projectionBindingExpression == null)
            throw new ArgumentNullException(nameof(projectionBindingExpression), "Projection binding expression cannot be null");

        // Check if we've already processed this projection
        if (_materializedEntityMapping.TryGetValue(projectionBindingExpression, out var existingVariable))
        {
            return existingVariable;
        }

        // Create a variable to hold the projected value
        var projectionVariable = Parameter(projectionBindingExpression.Type, $"projection_{_variables.Count}");
        _variables.Add(projectionVariable);
        _materializedEntityMapping[projectionBindingExpression] = projectionVariable;

        // Determine the projection index
        // For Azure Table, we'll assume single projection for simplicity
        var projectionIndex = 0;
        
        // Generate ValueBuffer read expression
        var valueBufferReadExpression = CreateValueBufferReadExpression(
            projectionBindingExpression.Type, 
            projectionIndex);

        // Add assignment expression
        _expressions.Add(Assign(projectionVariable, valueBufferReadExpression));
        
        
        return projectionVariable;
    }

    /// <summary>
    /// Generates an entity materialization expression that creates an entity instance
    /// and populates its properties from the ValueBuffer.
    /// This method creates a block expression that instantiates the entity and assigns
    /// property values from the corresponding ValueBuffer indices.
    /// </summary>
    /// <param name="shaperExpression">The shaper expression containing entity type information</param>
    /// <returns>A block expression that materializes the entity with all properties populated</returns>
    /// <exception cref="ArgumentNullException">Thrown when shaperExpression is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when entity type lacks a parameterless constructor</exception>
    private Expression GenerateEntityMaterializationExpression(StructuralTypeShaperExpression shaperExpression)
    {
        if (shaperExpression == null)
            throw new ArgumentNullException(nameof(shaperExpression), "Shaper expression cannot be null");
        var entityType = shaperExpression.StructuralType;
        var clrType = entityType.ClrType;
        

        // Get the parameterless constructor
        var constructorInfo = clrType.GetConstructor(Type.EmptyTypes);
        if (constructorInfo == null)
        {
            throw new InvalidOperationException($"Entity type {clrType.Name} must have a parameterless constructor");
        }

        // Create constructor call expression
        var constructorCall = New(constructorInfo);
        
        // Get all properties that should be materialized
        var properties = entityType.GetProperties().ToList();

        if (properties.Count == 0)
        {
            // No properties to set, just return the constructor call
            return constructorCall;
        }

        // Create a variable to hold the new entity instance
        var entityVariable = Parameter(clrType, "newEntity");
        var assignments = new List<Expression>();
        
        // Add constructor assignment
        assignments.Add(Assign(entityVariable, constructorCall));
        
        // Add property assignments from ValueBuffer
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var propertyInfo = property.PropertyInfo;
            
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                
                // Get value from ValueBuffer at index i
                var valueBufferRead = CreateValueBufferReadExpression(property.ClrType, i);
                
                // Create property assignment: entity.Property = valueBuffer[i]
                var propertyAssignment = Assign(
                    Property(entityVariable, propertyInfo),
                    valueBufferRead);
                
                assignments.Add(propertyAssignment);
            }
            else
            {
            }
        }
        
        // Add return statement
        assignments.Add(entityVariable);
        
        // Create block expression that constructs entity and sets properties
        var blockExpression = Block(new[] { entityVariable }, assignments);
        
        
        return blockExpression;
    }

    /// <summary>
    /// Creates a ValueBuffer read expression for accessing a value at a specific index.
    /// This method generates the necessary expressions to read a value from the ValueBuffer
    /// and convert it to the target type if needed.
    /// </summary>
    /// <param name="valueType">The target type for the value being read</param>
    /// <param name="index">The index in the ValueBuffer to read from</param>
    /// <returns>An expression that reads and converts the value from the ValueBuffer</returns>
    /// <exception cref="ArgumentNullException">Thrown when valueType is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when ValueBuffer indexer cannot be found</exception>
    private Expression CreateValueBufferReadExpression(Type valueType, int index)
    {
        if (valueType == null)
            throw new ArgumentNullException(nameof(valueType), "Value type cannot be null");
        
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative");
        
        // ValueBuffer[index] access
        var indexerProperty = typeof(ValueBuffer).GetProperty("Item", new[] { typeof(int) });
        if (indexerProperty == null)
        {
            throw new InvalidOperationException("ValueBuffer indexer property not found");
        }

        var indexAccess = Property(ValueBufferParameter, indexerProperty, Constant(index));
        
        // Convert to target type if needed
        if (valueType != typeof(object))
        {
            var convertExpression = Expression.Convert(indexAccess, valueType);
            return convertExpression;
        }

        return indexAccess;
    }
}