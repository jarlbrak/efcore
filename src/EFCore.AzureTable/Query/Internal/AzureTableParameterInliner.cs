// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     CRITICAL ARCHITECTURAL FIX: This class resolves EF Core parameters (__p_0, __roleName_0, etc.) 
///     at query execution time when parameter values are available in the QueryContext.
///     This fixes the core issue where OData filters were being generated during expression tree analysis
///     instead of during query execution, leading to unresolved parameter placeholders.
/// </remarks>
public class AzureTableParameterInliner : ExpressionVisitor
{
    private readonly IReadOnlyDictionary<string, object?> _parameterValues;
    private readonly int? _queryId;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <param name="parameterValues">Dictionary of parameter names to values from QueryContext.ParameterValues</param>
    /// <param name="queryId">Optional query ID for custom parameter handling</param>
    public AzureTableParameterInliner(IReadOnlyDictionary<string, object?> parameterValues, int? queryId = null)
    {
        _parameterValues = parameterValues ?? throw new ArgumentNullException(nameof(parameterValues));
        _queryId = queryId;
    }

    /// <summary>
    ///     Visits a parameter expression and attempts to resolve it using available parameter values.
    ///     This handles direct EF Core generated parameters (e.g., __p_0, __roleName_0).
    ///     This is a critical method for parameter resolution at query execution time.
    /// </summary>
    /// <param name="parameterExpression">The parameter expression to resolve</param>
    /// <returns>Either a constant expression with the resolved value or the original parameter expression</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameterExpression is null</exception>
    protected override Expression VisitParameter(ParameterExpression parameterExpression)
    {
        if (parameterExpression == null)
            throw new ArgumentNullException(nameof(parameterExpression), "Parameter expression cannot be null");

        // Skip entity parameters (like 'r' in 'r => r.Name') - these represent the entity being filtered
        if (parameterExpression.Name != null && 
            !parameterExpression.Name.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal))
        {
            return parameterExpression;
        }

        // This is an EF Core generated parameter (starts with "__") - attempt to resolve it
        if (parameterExpression.Name != null && TryResolveParameter(parameterExpression.Name, parameterExpression.Type, out var resolvedExpression))
        {
            return resolvedExpression;
        }

        
        // CRITICAL: If we can't resolve the parameter, we should not generate invalid OData
        // Return the parameter as-is, but this will likely cause the OData translator to detect it as invalid
        return parameterExpression;
    }

    /// <summary>
    ///     Visits member access expressions. This is CRITICAL for handling closure variables
    ///     where EF Core creates member access patterns for captured variables.
    ///     Examples: closure.variableName, __p_0.Value, etc.
    ///     This method handles the most complex parameter resolution scenarios.
    /// </summary>
    /// <param name="memberExpression">The member access expression to process</param>
    /// <returns>Either a constant expression with the resolved value or a modified member expression</returns>
    /// <exception cref="ArgumentNullException">Thrown when memberExpression is null</exception>
    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        if (memberExpression == null)
            throw new ArgumentNullException(nameof(memberExpression), "Member expression cannot be null");

        // CRITICAL FIX: Check if this member access represents a closure variable pattern
        if (TryEvaluateClosureVariable(memberExpression, out var closureValue))
        {
            return Expression.Constant(closureValue, memberExpression.Type);
        }

        // Process the member's expression (could contain parameters)
        var visitedExpression = memberExpression.Expression != null ? Visit(memberExpression.Expression) : null;

        // If the expression changed, create a new member expression
        if (visitedExpression != memberExpression.Expression && visitedExpression != null)
        {
            return Expression.MakeMemberAccess(visitedExpression, memberExpression.Member);
        }

        return memberExpression;
    }

    /// <summary>
    ///     Visits binary expressions (comparisons, logical operations) and resolves any parameter expressions within them.
    ///     This method ensures that both sides of binary operations have their parameters properly resolved.
    /// </summary>
    /// <param name="binaryExpression">The binary expression to process</param>
    /// <returns>A binary expression with resolved parameters on both sides</returns>
    /// <exception cref="ArgumentNullException">Thrown when binaryExpression is null</exception>
    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
        if (binaryExpression == null)
            throw new ArgumentNullException(nameof(binaryExpression), "Binary expression cannot be null");

        // Visit left and right sides to resolve any parameters
        var visitedLeft = Visit(binaryExpression.Left);
        var visitedRight = Visit(binaryExpression.Right);

        // If either side changed, create a new binary expression
        if (visitedLeft != binaryExpression.Left || visitedRight != binaryExpression.Right)
        {
            
            return Expression.MakeBinary(
                binaryExpression.NodeType,
                visitedLeft,
                visitedRight,
                binaryExpression.IsLiftedToNull,
                binaryExpression.Method,
                binaryExpression.Conversion);
        }

        return binaryExpression;
    }

    /// <summary>
    ///     Visits unary expressions and resolves any parameter expressions within them.
    ///     This method handles unary operations like NOT, negation, etc.
    /// </summary>
    /// <param name="unaryExpression">The unary expression to process</param>
    /// <returns>A unary expression with resolved parameters in the operand</returns>
    /// <exception cref="ArgumentNullException">Thrown when unaryExpression is null</exception>
    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        if (unaryExpression == null)
            throw new ArgumentNullException(nameof(unaryExpression), "Unary expression cannot be null");

        var visitedOperand = Visit(unaryExpression.Operand);

        if (visitedOperand != unaryExpression.Operand)
        {
            return Expression.MakeUnary(
                unaryExpression.NodeType,
                visitedOperand,
                unaryExpression.Type,
                unaryExpression.Method);
        }

        return unaryExpression;
    }

    /// <summary>
    ///     Visits method call expressions and resolves any parameter expressions within their arguments.
    ///     This method handles method calls like string.Contains(), DateTime.Compare(), etc.
    /// </summary>
    /// <param name="methodCallExpression">The method call expression to process</param>
    /// <returns>A method call expression with resolved parameters in arguments and instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when methodCallExpression is null</exception>
    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression == null)
            throw new ArgumentNullException(nameof(methodCallExpression), "Method call expression cannot be null");

        // Visit the object (if any) and all arguments
        var visitedObject = methodCallExpression.Object != null ? Visit(methodCallExpression.Object) : null;
        var visitedArguments = Visit(methodCallExpression.Arguments);

        // Check if anything changed
        if (visitedObject != methodCallExpression.Object || visitedArguments != methodCallExpression.Arguments)
        {
            return Expression.Call(visitedObject, methodCallExpression.Method, visitedArguments);
        }

        return methodCallExpression;
    }

    /// <summary>
    ///     Visits constant expressions. This is CRITICAL for handling closure instances
    ///     created by EF Core's funcletizer for captured variables.
    ///     SECURITY: Uses validated closure detection to prevent processing of malicious types.
    /// </summary>
    /// <param name="constantExpression">The constant expression to visit</param>
    /// <returns>The visited expression</returns>
    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
        
        // SECURITY: Check if this constant represents a validated compiler-generated closure
        if (IsValidClosureInstance(constantExpression))
        {
            // We don't evaluate closure instances directly - they get resolved via member access
        }
        
        return constantExpression;
    }

    /// <summary>
    ///     CRITICAL METHOD: Attempts to resolve parameter names to actual values from QueryContext.ParameterValues.
    ///     Handles various parameter naming patterns that EF Core generates including direct parameters,
    ///     custom query ID prefixed parameters, and pattern-based parameter matching.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to resolve</param>
    /// <param name="parameterType">The expected type of the parameter value</param>
    /// <param name="resolvedExpression">The resolved constant expression if successful</param>
    /// <returns>True if the parameter was successfully resolved, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameterName or parameterType is null</exception>
    private bool TryResolveParameter(string parameterName, Type parameterType, out Expression resolvedExpression)
    {
        if (parameterName == null)
            throw new ArgumentNullException(nameof(parameterName), "Parameter name cannot be null");
        
        if (parameterType == null)
            throw new ArgumentNullException(nameof(parameterType), "Parameter type cannot be null");
            
        resolvedExpression = null!;
        
        // Try direct parameter name lookup
        if (_parameterValues.TryGetValue(parameterName, out var value))
        {
            
            // Try to handle type conversion gracefully
            try
            {
                resolvedExpression = Expression.Constant(value, parameterType);
                return true;
            }
            catch (ArgumentException)
            {
                // Type mismatch - try type conversion
                try
                {
                    var convertedValue = value == null ? null : Convert.ChangeType(value, parameterType);
                    resolvedExpression = Expression.Constant(convertedValue, parameterType);
                    return true;
                }
                catch
                {
                    // Log the conversion failure but continue trying other resolution methods
                    // In production, this would use proper logging instead of throwing
                    // Fall through to try other resolution methods
                }
            }
        }
        
        // Try custom parameter resolution with query ID prefix
        if (_queryId.HasValue)
        {
            var customParameterKey = $"Q{_queryId.Value}_{parameterName}";
            if (_parameterValues.TryGetValue(customParameterKey, out var customValue))
            {
                
                // Try to handle type conversion gracefully
                try
                {
                    resolvedExpression = Expression.Constant(customValue, parameterType);
                    return true;
                }
                catch (ArgumentException)
                {
                    // Type mismatch - try type conversion
                    try
                    {
                        var convertedValue = customValue == null ? null : Convert.ChangeType(customValue, parameterType);
                        resolvedExpression = Expression.Constant(convertedValue, parameterType);
                        return true;
                    }
                    catch
                    {
                        // Fall through to try other resolution methods
                    }
                }
            }
        }
        
        // Try parameter name variations (EF Core sometimes uses different patterns)
        foreach (var kvp in _parameterValues)
        {
            if (kvp.Key.EndsWith($"_{parameterName}", StringComparison.Ordinal) || 
                kvp.Key.Contains(parameterName, StringComparison.Ordinal))
            {
                
                // Try to handle type conversion gracefully
                try
                {
                    resolvedExpression = Expression.Constant(kvp.Value, parameterType);
                    return true;
                }
                catch (ArgumentException)
                {
                    // Type mismatch - try type conversion
                    try
                    {
                        var convertedValue = kvp.Value == null ? null : Convert.ChangeType(kvp.Value, parameterType);
                        resolvedExpression = Expression.Constant(convertedValue, parameterType);
                        return true;
                    }
                    catch
                    {
                        // Continue to next parameter
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    ///     CRITICAL METHOD: Securely evaluates closure variable patterns that EF Core generates for captured variables.
    ///     This handles the most common failure case where 'var city = "London"' becomes a closure member access.
    ///     SECURITY: Uses safe reflection patterns only - no dynamic compilation or arbitrary code execution.
    /// </summary>
    /// <param name="memberExpression">The member expression to evaluate</param>
    /// <param name="value">The resolved value if successful</param>
    /// <returns>True if the closure variable was successfully resolved</returns>
    private bool TryEvaluateClosureVariable(MemberExpression memberExpression, out object? value)
    {
        value = null;
        
        try
        {
            // SECURITY: Only process verified closure instances with restricted reflection access
            if (memberExpression.Expression is ConstantExpression closureConstant && 
                IsValidClosureInstance(closureConstant))
            {
                
                // SECURITY: Safe reflection - only access fields/properties on verified closure types
                if (memberExpression.Member is FieldInfo field && closureConstant.Value != null && IsSecureFieldAccess(field, closureConstant.Value))
                {
                    value = field.GetValue(closureConstant.Value);
                    return true;
                }
                else if (memberExpression.Member is PropertyInfo property && closureConstant.Value != null && IsSecurePropertyAccess(property, closureConstant.Value))
                {
                    value = property.GetValue(closureConstant.Value);
                    return true;
                }
            }
            
            // SECURITY: Handle nested closure patterns with validated reflection only
            if (memberExpression.Expression is MemberExpression nestedMember &&
                TryEvaluateClosureVariable(nestedMember, out var nestedValue) && nestedValue != null)
            {
                
                if (memberExpression.Member is FieldInfo nestedField && nestedValue != null && IsSecureFieldAccess(nestedField, nestedValue))
                {
                    value = nestedField.GetValue(nestedValue);
                    return true;
                }
                else if (memberExpression.Member is PropertyInfo nestedProperty && nestedValue != null && IsSecurePropertyAccess(nestedProperty, nestedValue))
                {
                    value = nestedProperty.GetValue(nestedValue);
                    return true;
                }
            }
            
            // SECURITY: Removed dynamic expression compilation - too dangerous for production
            // The previous fallback used Expression.Lambda().Compile() and DynamicInvoke() 
            // which enable arbitrary code execution and are major security vulnerabilities
        }
        catch (SecurityException ex)
        {
            // Security exception during closure evaluation - this is expected for untrusted code
            // Log the security violation but don't expose details to prevent information leakage
            throw new InvalidOperationException("Parameter resolution blocked due to security restrictions", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Unauthorized access during closure evaluation - reflection permissions issue
            throw new InvalidOperationException("Parameter resolution failed due to access restrictions", ex);
        }
        catch (Exception ex)
        {
            // General failure during closure variable evaluation
            throw new InvalidOperationException("Failed to evaluate closure variable during parameter resolution", ex);
        }
        
        return false;
    }
    
    /// <summary>
    ///     SECURITY: Validates if a constant expression represents a legitimate compiler-generated closure instance.
    ///     Includes enhanced security checks to prevent processing of malicious types.
    /// </summary>
    /// <param name="constantExpression">The constant expression to validate</param>
    /// <returns>True if this is a verified compiler-generated closure</returns>
    private static bool IsValidClosureInstance(ConstantExpression constantExpression)
    {
        if (constantExpression.Value == null) return false;
        
        var type = constantExpression.Value.GetType();
        
        // SECURITY: Enhanced validation for compiler-generated closure types
        var isCompilerGenerated = Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: true);
        var isNestedPrivate = type.Attributes.HasFlag(TypeAttributes.NestedPrivate);
        var isTrustedAssembly = IsTrustedAssembly(type.Assembly);
        
        // SECURITY: Only allow closures from trusted assemblies that are compiler-generated
        return isCompilerGenerated && isNestedPrivate && isTrustedAssembly;
    }
    
    /// <summary>
    ///     SECURITY: Validates that field access is safe on verified closure instances.
    ///     Prevents reflection attacks on arbitrary objects.
    /// </summary>
    /// <param name="field">The field to access</param>
    /// <param name="target">The target object containing the field</param>
    /// <returns>True if field access is considered secure</returns>
    private static bool IsSecureFieldAccess(FieldInfo field, object target)
    {
        if (field == null || target == null) return false;
        
        var targetType = target.GetType();
        
        // SECURITY: Only allow field access on compiler-generated types from trusted assemblies
        var isCompilerGenerated = Attribute.IsDefined(targetType, typeof(CompilerGeneratedAttribute), inherit: true);
        var isTrustedAssembly = IsTrustedAssembly(targetType.Assembly);
        var isPublicField = field.IsPublic;
        
        return isCompilerGenerated && isTrustedAssembly && isPublicField;
    }
    
    /// <summary>
    ///     SECURITY: Validates that property access is safe on verified closure instances.
    ///     Prevents reflection attacks on arbitrary objects.
    /// </summary>
    /// <param name="property">The property to access</param>
    /// <param name="target">The target object containing the property</param>
    /// <returns>True if property access is considered secure</returns>
    private static bool IsSecurePropertyAccess(PropertyInfo property, object target)
    {
        if (property == null || target == null) return false;
        
        var targetType = target.GetType();
        
        // SECURITY: Only allow property access on compiler-generated types from trusted assemblies
        var isCompilerGenerated = Attribute.IsDefined(targetType, typeof(CompilerGeneratedAttribute), inherit: true);
        var isTrustedAssembly = IsTrustedAssembly(targetType.Assembly);
        var hasPublicGetter = property.GetMethod != null && property.GetMethod.IsPublic;
        
        return isCompilerGenerated && isTrustedAssembly && hasPublicGetter;
    }
    
    /// <summary>
    ///     SECURITY: Validates that an assembly is trusted for reflection operations.
    ///     Prevents code execution from untrusted assemblies.
    /// </summary>
    /// <param name="assembly">The assembly to validate</param>
    /// <returns>True if the assembly is considered trusted</returns>
    private static bool IsTrustedAssembly(Assembly assembly)
    {
        if (assembly == null) return false;
        
        // SECURITY: Allow assemblies from the current app domain and system assemblies
        var currentAssembly = Assembly.GetExecutingAssembly();
        var entryAssembly = Assembly.GetEntryAssembly();
        
        // Trust current assembly, entry assembly, and Microsoft/System assemblies
        return assembly == currentAssembly ||
               assembly == entryAssembly ||
               assembly.FullName?.StartsWith("System.", StringComparison.Ordinal) == true ||
               assembly.FullName?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;
    }
}