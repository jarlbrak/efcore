// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class ODataFilterTranslator : ExpressionVisitor
{
    private readonly StringBuilder _filter = new();
    private readonly IModel _model;
    private readonly QueryContext? _queryContext;
    
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public ODataFilterTranslator(IModel model, QueryContext? queryContext = null)
    {
        _model = model;
        _queryContext = queryContext;
    }
    
    /// <summary>
    ///     Translates an expression to an OData filter string.
    /// </summary>
    public virtual string? Translate(Expression expression)
    {
        _filter.Clear();
        
        // CRITICAL FIX: Pre-validate expression to avoid generating invalid filters
        if (ShouldSkipFilter(expression))
        {
            return null;
        }
        
        Visit(expression);
        var result = _filter.Length > 0 ? _filter.ToString() : null;
        
        // Parameter resolution should have happened before OData translation
        // If we reach here with a valid result, all parameters should be resolved
        
        // Final validation to ensure we don't return invalid OData syntax
        if (!string.IsNullOrEmpty(result) && IsInvalidODataFilter(result))
        {
            System.Console.WriteLine($"[WARNING] Generated invalid OData filter: {result}, skipping filter");
            return null;
        }
        
        return result;
    }
    
    private bool ShouldSkipFilter(Expression expression)
    {
        // Skip filters that would result in meaningless comparisons
        if (expression is BinaryExpression binary)
        {
            // Check for comparisons with null on both sides
            if (binary.Left is ConstantExpression leftConst && leftConst.Value == null &&
                binary.Right is ConstantExpression rightConst && rightConst.Value == null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsInvalidODataFilter(string filter)
    {
        // Check for common invalid patterns
        return filter.Contains(" eq null null") ||                     // Double null
               filter.Trim().EndsWith(" eq ", StringComparison.Ordinal) ||                       // Incomplete equality
               filter.Trim().EndsWith(" ne ", StringComparison.Ordinal) ||                       // Incomplete inequality
               filter.Trim().EndsWith(" gt ", StringComparison.Ordinal) ||                       // Incomplete greater than
               filter.Trim().EndsWith(" lt ", StringComparison.Ordinal) ||                       // Incomplete less than
               filter.Trim().EndsWith(" ge ", StringComparison.Ordinal) ||                       // Incomplete greater equal
               filter.Trim().EndsWith(" le ", StringComparison.Ordinal) ||                       // Incomplete less equal
               string.IsNullOrWhiteSpace(filter);
    }
    
    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
        var requiresParentheses = RequiresParentheses(binaryExpression);

        if (requiresParentheses)
        {
            _filter.Append('(');
        }

        Visit(binaryExpression.Left);

        var op = binaryExpression.NodeType switch
        {
            ExpressionType.Equal => " eq ",
            ExpressionType.NotEqual => " ne ",
            ExpressionType.GreaterThan => " gt ",
            ExpressionType.GreaterThanOrEqual => " ge ",
            ExpressionType.LessThan => " lt ",
            ExpressionType.LessThanOrEqual => " le ",
            ExpressionType.AndAlso => " and ",
            ExpressionType.OrElse => " or ",
            _ => throw new InvalidOperationException($"Binary operator {binaryExpression.NodeType} is not supported in Azure Table Storage OData filters.")
        };

        _filter.Append(op);

        Visit(binaryExpression.Right);

        if (requiresParentheses)
        {
            _filter.Append(')');
        }

        return binaryExpression;
    }

    /// <inheritdoc />
    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
        if (constantExpression.Value == null)
        {
            _filter.Append("null");
        }
        else
        {
            var value = constantExpression.Value;
            var type = value.GetType();

            if (type == typeof(string))
            {
                var stringValue = (string)value;
                
                // CRITICAL FIX: Handle empty strings properly to avoid invalid OData syntax
                if (string.IsNullOrEmpty(stringValue))
                {
                    // For empty strings, generate valid OData syntax
                    _filter.Append("''");
                }
                else
                {
                    _filter.Append('\'')
                        .Append(EscapeStringValue(stringValue))
                        .Append('\'');
                }
            }
            else if (type == typeof(bool))
            {
                _filter.Append((bool)value ? "true" : "false");
            }
            else if (type == typeof(DateTime))
            {
                _filter.Append("datetime'")
                    .Append(((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"))
                    .Append('\'');
            }
            else if (type == typeof(DateTimeOffset))
            {
                _filter.Append("datetimeoffset'")
                    .Append(((DateTimeOffset)value).ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"))
                    .Append('\'');
            }
            else if (type == typeof(Guid))
            {
                _filter.Append("guid'")
                    .Append(value.ToString())
                    .Append('\'');
            }
            else if (type == typeof(byte[]))
            {
                _filter.Append("X'")
                    .Append(Convert.ToHexString((byte[])value))
                    .Append('\'');
            }
            else if (IsNumericType(type))
            {
                _filter.Append(value);
            }
            else
            {
                throw new InvalidOperationException($"Constant type {type} is not supported in Azure Table Storage OData filters.");
            }
        }

        return constantExpression;
    }

    /// <inheritdoc />
    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        System.Console.WriteLine($"[DEBUG] VisitMember: {memberExpression}");
        System.Console.WriteLine($"[DEBUG] Member name: {memberExpression.Member.Name}");
        System.Console.WriteLine($"[DEBUG] Expression: {memberExpression.Expression}");
        System.Console.WriteLine($"[DEBUG] Expression type: {memberExpression.Expression?.GetType().Name}");
        System.Console.WriteLine($"[DEBUG] Expression NodeType: {memberExpression.Expression?.NodeType}");

        if (memberExpression.Expression != null)
        {
            switch (memberExpression.Expression.NodeType)
            {
                case ExpressionType.Parameter:
                    // Simple case: r.Property
                    _filter.Append(memberExpression.Member.Name);
                    break;

                case ExpressionType.MemberAccess:
                    // Nested member access: Visit the parent member first
                    Visit(memberExpression.Expression);
                    _filter.Append('.');
                    _filter.Append(memberExpression.Member.Name);
                    break;

                case ExpressionType.Call:
                    // Method call result member access - evaluate the call first
                    Visit(memberExpression.Expression);
                    _filter.Append('.');
                    _filter.Append(memberExpression.Member.Name);
                    break;

                case ExpressionType.Extension:
                    // Handle extension expressions like StructuralTypeShaperExpression
                    if (memberExpression.Expression is StructuralTypeShaperExpression)
                    {
                        // This is entity property access - just use the member name
                        _filter.Append(memberExpression.Member.Name);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Extension expression type '{memberExpression.Expression.GetType().Name}' is not supported in Azure Table Storage OData filters.");
                    }
                    break;

                default:
                    // For other expression types, try to evaluate if it's a simple property access
                    if (TryGetSimplePropertyName(memberExpression, out var propertyName))
                    {
                        _filter.Append(propertyName);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Member access pattern '{memberExpression}' is not supported in Azure Table Storage OData filters. Expression type: {memberExpression.Expression.NodeType}");
                    }
                    break;
            }
        }
        else
        {
            // Static member access
            _filter.Append(memberExpression.Member.Name);
        }

        return memberExpression;
    }

    private bool TryGetSimplePropertyName(MemberExpression memberExpression, out string propertyName)
    {
        propertyName = string.Empty;
        
        // Check if this is ultimately a parameter-based property access
        var current = memberExpression;
        var names = new List<string>();
        
        while (current != null)
        {
            names.Add(current.Member.Name);
            
            if (current.Expression is ParameterExpression)
            {
                // We found a parameter at the root - this is a valid property chain
                names.Reverse();
                propertyName = string.Join(".", names);
                return true;
            }
            
            if (current.Expression is MemberExpression parentMember)
            {
                current = parentMember;
                continue;
            }
            
            // Not a simple property chain
            break;
        }
        
        return false;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Method.DeclaringType == typeof(string))
        {
            switch (methodCallExpression.Method.Name)
            {
                case nameof(string.StartsWith) when methodCallExpression.Arguments.Count == 1:
                    _filter.Append("startswith(");
                    Visit(methodCallExpression.Object);
                    _filter.Append(", ");
                    Visit(methodCallExpression.Arguments[0]);
                    _filter.Append(')');
                    return methodCallExpression;

                case nameof(string.EndsWith) when methodCallExpression.Arguments.Count == 1:
                    _filter.Append("endswith(");
                    Visit(methodCallExpression.Object);
                    _filter.Append(", ");
                    Visit(methodCallExpression.Arguments[0]);
                    _filter.Append(')');
                    return methodCallExpression;

                case nameof(string.Contains) when methodCallExpression.Arguments.Count == 1:
                    _filter.Append("substringof(");
                    Visit(methodCallExpression.Arguments[0]);
                    _filter.Append(", ");
                    Visit(methodCallExpression.Object);
                    _filter.Append(')');
                    return methodCallExpression;
            }
        }

        throw new InvalidOperationException($"Method {methodCallExpression.Method.Name} is not supported in Azure Table Storage OData filters.");
    }

    /// <inheritdoc />
    protected override Expression VisitParameter(ParameterExpression parameterExpression)
    {
        System.Console.WriteLine($"[ODATA] VisitParameter: {parameterExpression.Name} (Type: {parameterExpression.Type})");
        
        // Check if this is an EF Core generated parameter (they start with "__")
        if (parameterExpression.Name != null && 
            parameterExpression.Name.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal))
        {
            // CRITICAL ERROR: If we see EF parameters here, it means parameter resolution failed
            System.Console.WriteLine($"[ERROR] Unresolved EF Core parameter '{parameterExpression.Name}' reached OData translator");
            System.Console.WriteLine($"[ERROR] This should have been resolved by AzureTableParameterInliner before OData translation");
            
            throw new InvalidOperationException($"Parameter '{parameterExpression.Name}' was not resolved before OData translation. This indicates a bug in the parameter resolution pipeline.");
        }
        
        // For entity parameters (like 'r' in 'r => r.Name'), we don't append anything to the filter
        // These represent the entity being filtered and are handled by member access expressions
        System.Console.WriteLine($"[ODATA] Entity parameter '{parameterExpression.Name}' - no OData output needed");
        return parameterExpression;
    }
    

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        switch (unaryExpression.NodeType)
        {
            case ExpressionType.Not:
                _filter.Append("not ");
                Visit(unaryExpression.Operand);
                return unaryExpression;

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                // Skip conversions in most cases
                return Visit(unaryExpression.Operand);

            default:
                throw new InvalidOperationException($"Unary operator {unaryExpression.NodeType} is not supported in Azure Table Storage OData filters.");
        }
    }

    private static bool RequiresParentheses(BinaryExpression expression)
    {
        // Add parentheses for complex logical expressions
        return expression.NodeType == ExpressionType.AndAlso || expression.NodeType == ExpressionType.OrElse;
    }

    private static string EscapeStringValue(string value)
    {
        // Escape single quotes in OData string values
        return value.Replace("'", "''");
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(double) ||
               type == typeof(float) || type == typeof(decimal) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(sbyte) || type == typeof(uint) ||
               type == typeof(ulong) || type == typeof(ushort);
    }
}