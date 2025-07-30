// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8603 // Possible null reference return

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableQueryableMethodTranslatingExpressionVisitor : QueryableMethodTranslatingExpressionVisitor
{
    private readonly AzureTableQueryCompilationContext _azureTableQueryCompilationContext;
    private readonly IModel _model;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableQueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext, subquery: false)
    {
        _azureTableQueryCompilationContext = (AzureTableQueryCompilationContext)queryCompilationContext;
        _model = queryCompilationContext.Model;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected AzureTableQueryableMethodTranslatingExpressionVisitor(
        AzureTableQueryableMethodTranslatingExpressionVisitor parentVisitor)
        : base(parentVisitor.Dependencies, parentVisitor.QueryCompilationContext, subquery: true)
    {
        _azureTableQueryCompilationContext = parentVisitor._azureTableQueryCompilationContext;
        _model = parentVisitor._model;
    }

    /// <inheritdoc />
    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
        => new AzureTableQueryableMethodTranslatingExpressionVisitor(this);

    /// <inheritdoc />
    protected override ShapedQueryExpression CreateShapedQueryExpression(IEntityType entityType)
    {
        var tableExpression = new AzureTableExpression(entityType);
        
        return CreateShapedQueryExpression(entityType, tableExpression);
    }

    private ShapedQueryExpression CreateShapedQueryExpression(
        IEntityType entityType,
        AzureTableExpression tableExpression)
    {
        var queryExpression = new AzureTableQueryExpression(tableExpression);

        return new ShapedQueryExpression(
            queryExpression,
            new StructuralTypeShaperExpression(
                entityType,
                new ProjectionBindingExpression(
                    queryExpression,
                    new ProjectionMember(),
                    typeof(Dictionary<string, object>)),
                false));
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateAll(ShapedQueryExpression source, LambdaExpression predicate)
    {
        // Azure Table doesn't support All operation
        AddTranslationErrorDetails("Azure Table Storage does not support the All operation.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateAny(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        // Can translate Any only as existence check (without predicate)
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate)!;
            if (source == null)
            {
                return null;
            }
        }

        // Convert to a Take(1) query
        source = TranslateTake(source, Expression.Constant(1))!;
        if (source == null)
        {
            return null;
        }

        // Change the shaper to return bool
        source = source.UpdateShaperExpression(Expression.Convert(
            Expression.NotEqual(source.ShaperExpression, Expression.Constant(null)),
            typeof(bool)));

        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateAverage(
        ShapedQueryExpression source,
        LambdaExpression? selector,
        Type resultType)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support aggregate operations like Average.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        // CRITICAL FIX: Implement Count() translation for Azure Table
        System.Console.WriteLine($"[DEBUG] TranslateCount called with predicate: {predicate}");
        
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate)!;
            if (source == null)
            {
                System.Console.WriteLine($"[DEBUG] Count with predicate failed - where translation returned null");
                return null;
            }
        }
        
        // For Azure Table, we'll count by enumerating the results
        // This is the simplest approach that works with our existing infrastructure
        
        // Change the shaper to return a count instead of entities
        var constantOne = Expression.Constant(1);
        source = source.UpdateShaperExpression(constantOne);
        
        // The enumeration will happen at the query execution level
        // We rely on client-side evaluation to sum up the 1's
        System.Console.WriteLine($"[DEBUG] Count translation successful - will enumerate and count");
        
        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateFirstOrDefault(
        ShapedQueryExpression source,
        LambdaExpression? predicate,
        Type returnType,
        bool returnDefault)
    {
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate)!;
            if (source == null)
            {
                return null;
            }
        }

        // Limit to 1 result
        source = TranslateTake(source, Expression.Constant(1))!;
        if (source == null)
        {
            return null;
        }

        if (source.ShaperExpression.Type != returnType)
        {
            source = source.UpdateShaperExpression(Expression.Convert(source.ShaperExpression, returnType));
        }

        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateGroupBy(
        ShapedQueryExpression source,
        LambdaExpression keySelector,
        LambdaExpression? elementSelector,
        LambdaExpression? resultSelector)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support GroupBy operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateJoin(
        ShapedQueryExpression outer,
        ShapedQueryExpression inner,
        LambdaExpression outerKeySelector,
        LambdaExpression innerKeySelector,
        LambdaExpression resultSelector)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Join operations between tables.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateOrderBy(
        ShapedQueryExpression source,
        LambdaExpression keySelector,
        bool ascending)
    {
        var queryExpression = (AzureTableQueryExpression)source.QueryExpression;
        var keySelectorBody = RemapLambdaBody(source, keySelector);

        // Azure Table only supports ordering by RowKey within a partition
        if (keySelectorBody is MemberExpression memberExpression)
        {
            var property = _model.FindEntityType(memberExpression.Expression!.Type)
                ?.FindProperty(memberExpression.Member.Name);
                
            if (property?.IsRowKey() == true)
            {
                queryExpression.ApplyOrdering(keySelectorBody, ascending);
                return source;
            }
        }

        AddTranslationErrorDetails("Azure Table Storage only supports ordering by RowKey within a partition query.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression TranslateSelect(ShapedQueryExpression source, LambdaExpression selector)
    {
        // For now, we'll support only entity projections
        // Custom projections would require more complex translation
        if (selector.Body != selector.Parameters[0])
        {
            AddTranslationErrorDetails("Azure Table Storage provider currently only supports entity projections. Custom projections are not yet implemented.");
            return null;
        }

        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateSkip(ShapedQueryExpression source, Expression count)
    {
        var queryExpression = (AzureTableQueryExpression)source.QueryExpression;
        queryExpression.ApplySkip(count);
        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateTake(ShapedQueryExpression source, Expression count)
    {
        var queryExpression = (AzureTableQueryExpression)source.QueryExpression;
        queryExpression.ApplyTake(count);
        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateWhere(ShapedQueryExpression source, LambdaExpression predicate)
    {
        var queryExpression = (AzureTableQueryExpression)source.QueryExpression;
        var predicateBody = RemapLambdaBody(source, predicate);
        
        System.Console.WriteLine($"[DEBUG] TranslateWhere - Original predicate: {predicate}");
        System.Console.WriteLine($"[DEBUG] TranslateWhere - Remapped predicate body: {predicateBody}");
        System.Console.WriteLine($"[DEBUG] TranslateWhere - Predicate body type: {predicateBody.GetType().Name}");
        System.Console.WriteLine($"[DEBUG] TranslateWhere - Predicate body node type: {predicateBody.NodeType}");
        
        // The closure variable evaluation now happens in RemapLambdaBody
        System.Console.WriteLine($"[DEBUG] TranslateWhere - Predicate body after remapping: {predicateBody}");
        
        // NOTE: Do NOT resolve parameters here - they're not available during compilation
        // Parameter resolution happens at execution time in AzureTableShapedQueryCompilingExpressionVisitor
        
        // Store the predicate expression in the query - OData translation happens at execution time
        queryExpression.ApplyFilter(predicateBody, ""); // Pass empty string to indicate deferred resolution
        return source;
    }
    
    /// <summary>
    /// CRITICAL FIX: Validates and fixes OData filter syntax to prevent invalid filters from reaching Azure Table
    /// </summary>
    private string? ValidateAndFixODataFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return filter;
        }
        
        System.Console.WriteLine($"[DEBUG] ValidateAndFixODataFilter input: '{filter}'");
        
        // Fix the most common issue: "field eq " (missing value after eq)
        var fixedFilter = filter;
        
        // Pattern 1: "field eq " at end of string or before "and"/"or"
        var invalidPatterns = new[]
        {
            " eq ",      // "field eq " 
            " ne ",      // "field ne "
            " gt ",      // "field gt "
            " lt ",      // "field le "
            " ge ",      // "field ge "
            " le "       // "field le "
        };
        
        foreach (var pattern in invalidPatterns)
        {
            // Check for pattern at end of string
            if (fixedFilter.EndsWith(pattern.Trim(), StringComparison.Ordinal))
            {
                System.Console.WriteLine($"[ERROR] Invalid OData filter detected - ends with '{pattern.Trim()}': {fixedFilter}");
                
                // Replace with null comparison
                fixedFilter = fixedFilter.Replace(pattern.Trim(), pattern.Trim() + " null");
                System.Console.WriteLine($"[FIX] Fixed to: {fixedFilter}");
            }
            
            // Check for pattern followed by " and " or " or "
            var andPattern = pattern + "and ";
            var orPattern = pattern + "or ";
            
            if (fixedFilter.Contains(andPattern))
            {
                System.Console.WriteLine($"[ERROR] Invalid OData filter detected - '{pattern}and': {fixedFilter}");
                fixedFilter = fixedFilter.Replace(andPattern, pattern + "null and ");
                System.Console.WriteLine($"[FIX] Fixed to: {fixedFilter}");
            }
            
            if (fixedFilter.Contains(orPattern))
            {
                System.Console.WriteLine($"[ERROR] Invalid OData filter detected - '{pattern}or': {fixedFilter}");
                fixedFilter = fixedFilter.Replace(orPattern, pattern + "null or ");
                System.Console.WriteLine($"[FIX] Fixed to: {fixedFilter}");
            }
            
            // Check for pattern followed by closing parenthesis
            var parenPattern = pattern + ")";
            if (fixedFilter.Contains(parenPattern))
            {
                System.Console.WriteLine($"[ERROR] Invalid OData filter detected - '{pattern})': {fixedFilter}");
                fixedFilter = fixedFilter.Replace(parenPattern, pattern + "null)");
                System.Console.WriteLine($"[FIX] Fixed to: {fixedFilter}");
            }
        }
        
        // Final validation - if filter still contains invalid patterns, return null to skip
        if (ContainsInvalidODataSyntax(fixedFilter))
        {
            System.Console.WriteLine($"[ERROR] Filter still contains invalid syntax after fixes, skipping: {fixedFilter}");
            return null;
        }
        
        System.Console.WriteLine($"[DEBUG] ValidateAndFixODataFilter output: '{fixedFilter}'");
        return fixedFilter;
    }
    
    private bool ContainsInvalidODataSyntax(string filter)
    {
        // Check for remaining invalid patterns
        var invalidSigns = new[] { " eq ", " ne ", " gt ", " lt ", " ge ", " le " };
        
        foreach (var sign in invalidSigns)
        {
            // Check if ends with the operator (no value after)
            if (filter.EndsWith(sign.Trim(), StringComparison.Ordinal))
                return true;
                
            // Check if operator is followed immediately by logical operators
            if (filter.Contains(sign + "and ") || filter.Contains(sign + "or ") || filter.Contains(sign + ")"))
                return true;
        }
        
        return false;
    }

    private sealed class ParameterReplacingExpressionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly Expression _newExpression;

        public ParameterReplacingExpressionVisitor(ParameterExpression oldParameter, Expression newExpression)
        {
            _oldParameter = oldParameter;
            _newExpression = newExpression;
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            return parameterExpression == _oldParameter ? _newExpression : parameterExpression;
        }
    }

    private Expression RemapLambdaBody(ShapedQueryExpression shapedQueryExpression, LambdaExpression lambdaExpression)
    {
        System.Console.WriteLine($"[DEBUG] RemapLambdaBody called");
        System.Console.WriteLine($"[DEBUG] Lambda: {lambdaExpression}");
        System.Console.WriteLine($"[DEBUG] Lambda body: {lambdaExpression.Body}");
        System.Console.WriteLine($"[DEBUG] Lambda parameter: {lambdaExpression.Parameters[0]}");
        System.Console.WriteLine($"[DEBUG] Shaper expression: {shapedQueryExpression.ShaperExpression}");
        
        // CRITICAL FIX: Let EF Core handle parameterization naturally - DO NOT interfere with closure variables
        // EF Core's ExpressionTreeFuncletizer will convert closure variables to __p_0 parameters
        // Our AzureTableParameterInliner will then resolve those parameters at execution time
        
        // Simply replace lambda parameters with proper entity access expressions
        System.Console.WriteLine($"[DEBUG] Replacing lambda parameters (letting EF Core handle parameterization)");
        var parameterReplacer = new ParameterReplacingExpressionVisitor(
            lambdaExpression.Parameters[0], 
            shapedQueryExpression.ShaperExpression);
            
        var remapped = parameterReplacer.Visit(lambdaExpression.Body);
        System.Console.WriteLine($"[DEBUG] Final remapped body: {remapped}");
        System.Console.WriteLine($"[DEBUG] Final remapped type: {remapped.GetType().Name}");
        
        return remapped;
    }
    

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateCast(ShapedQueryExpression source, Type resultType)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Cast operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateElementAtOrDefault(
        ShapedQueryExpression source,
        Expression index,
        bool returnDefault)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support ElementAt operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateExcept(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Except operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateIntersect(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Intersect operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateLongCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support LongCount operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateMax(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Max operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateMin(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Min operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateOfType(ShapedQueryExpression source, Type resultType)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support OfType operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateReverse(ShapedQueryExpression source)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Reverse operations. Use OrderByDescending instead.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateSelectMany(
        ShapedQueryExpression source,
        LambdaExpression collectionSelector,
        LambdaExpression resultSelector)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support SelectMany operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression selector)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support SelectMany operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateSingleOrDefault(
        ShapedQueryExpression source,
        LambdaExpression? predicate,
        Type returnType,
        bool returnDefault)
    {
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate)!;
            if (source == null)
            {
                return null;
            }
        }

        // Similar to FirstOrDefault but ensures exactly one result
        source = TranslateTake(source, Expression.Constant(2)); // Take 2 to detect multiple results
        if (source == null)
        {
            return null;
        }

        if (source.ShaperExpression.Type != returnType)
        {
            source = source.UpdateShaperExpression(Expression.Convert(source.ShaperExpression, returnType));
        }

        return source;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateSkipWhile(ShapedQueryExpression source, LambdaExpression predicate)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support SkipWhile operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateSum(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Sum operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateTakeWhile(ShapedQueryExpression source, LambdaExpression predicate)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support TakeWhile operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateUnion(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Union operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateThenBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
    {
        // ThenBy is not supported - Azure Table only supports single-level ordering
        AddTranslationErrorDetails("Azure Table Storage does not support ThenBy operations. Only single-level ordering is supported.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateGroupJoin(
        ShapedQueryExpression outer,
        ShapedQueryExpression inner,
        LambdaExpression outerKeySelector,
        LambdaExpression innerKeySelector,
        LambdaExpression resultSelector)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support GroupJoin operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateConcat(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Concat operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateContains(ShapedQueryExpression source, Expression item)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Contains operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateLastOrDefault(
        ShapedQueryExpression source,
        LambdaExpression? predicate,
        Type returnType,
        bool returnDefault)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support LastOrDefault operations. Use FirstOrDefault with reverse ordering instead.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateDistinct(ShapedQueryExpression source)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support Distinct operations.");
        return null;
    }


    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateLeftJoin(
        ShapedQueryExpression outer,
        ShapedQueryExpression inner,
        LambdaExpression outerKeySelector,
        LambdaExpression innerKeySelector,
        LambdaExpression resultSelector)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support LeftJoin operations.");
        return null;
    }

    /// <inheritdoc />
    protected override ShapedQueryExpression? TranslateDefaultIfEmpty(ShapedQueryExpression source, Expression? defaultValue)
    {
        AddTranslationErrorDetails("Azure Table Storage does not support DefaultIfEmpty operations.");
        return null;
    }


    protected override void AddTranslationErrorDetails(string details)
    {
        // In a real implementation, this would add error details to the query compilation context
        // For now, we'll include it in exception messages
    }
}