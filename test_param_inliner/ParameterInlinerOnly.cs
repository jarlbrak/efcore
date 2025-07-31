// Extracted AzureTableParameterInliner class for testing
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace TestParameterInliner
{
    /// <summary>
    /// Expression visitor that inlines parameter values in LINQ expressions for Azure Table Storage queries.
    /// This visitor resolves EF Core generated parameters and closure variables to their actual values,
    /// enabling more efficient query processing by converting parameterized expressions to constant expressions.
    /// </summary>
    /// <remarks>
    /// This implementation includes security validation for reflection operations and performance optimizations
    /// through expression compilation caching. Only compiler-generated closure types are allowed for
    /// reflection-based member access to prevent security vulnerabilities.
    /// </remarks>
    public class AzureTableParameterInliner : ExpressionVisitor
    {
        /// <summary>
        /// Dictionary containing parameter names mapped to their resolved values.
        /// </summary>
        private readonly IReadOnlyDictionary<string, object?> _parameterValues;
        
        /// <summary>
        /// Optional query identifier used for custom parameter naming schemes.
        /// </summary>
        private readonly int? _queryId;
        
        /// <summary>
        /// Cache for compiled expressions to avoid recompilation in hot paths.
        /// Thread-safe concurrent dictionary ensures safe access from multiple threads.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Func<object?>> _compiledExpressionCache = new();

        /// <summary>
        /// Initializes a new instance of the AzureTableParameterInliner class.
        /// </summary>
        /// <param name="parameterValues">Dictionary containing parameter names mapped to their resolved values. Cannot be null.</param>
        /// <param name="queryId">Optional query identifier used for custom parameter naming schemes. When provided, parameters will be resolved using "Q{queryId}_{parameterName}" format.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameterValues"/> is null.</exception>
        public AzureTableParameterInliner(IReadOnlyDictionary<string, object?> parameterValues, int? queryId = null)
        {
            _parameterValues = parameterValues ?? throw new ArgumentNullException(nameof(parameterValues));
            _queryId = queryId;
        }

        /// <summary>
        /// Visits a parameter expression and attempts to resolve it to a constant value.
        /// Entity parameters (not starting with "__") are left as-is, while EF Core generated
        /// parameters are resolved from the parameter values dictionary.
        /// </summary>
        /// <param name="parameterExpression">The parameter expression to visit.</param>
        /// <returns>A constant expression if the parameter was resolved, otherwise the original parameter expression.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameterExpression"/> is null.</exception>
        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            if (parameterExpression == null)
                throw new ArgumentNullException(nameof(parameterExpression));
                
            LogDebug($"Visiting parameter: {parameterExpression.Name} (Type: {parameterExpression.Type})");

            // Skip entity parameters (like 'r' in 'r => r.Name') - these represent the entity being filtered
            if (parameterExpression.Name != null && 
                !parameterExpression.Name.StartsWith("__", StringComparison.Ordinal))
            {
                LogDebug($"Skipping entity parameter: {parameterExpression.Name}");
                return parameterExpression;
            }

            // This is an EF Core generated parameter (starts with "__") - attempt to resolve it
            if (parameterExpression.Name != null && TryResolveParameter(parameterExpression.Name, parameterExpression.Type, out var resolvedExpression))
            {
                LogDebug($"Successfully resolved parameter {parameterExpression.Name}");
                return resolvedExpression;
            }

            LogWarning($"Could not resolve parameter {parameterExpression.Name}. Available parameters: {string.Join(", ", _parameterValues.Keys)}");
            
            return parameterExpression;
        }

        /// <summary>
        /// Visits a member access expression and attempts to resolve closure variables to constant values.
        /// This method handles both direct member access on closure instances and nested member expressions
        /// that may contain resolvable parameters.
        /// </summary>
        /// <param name="memberExpression">The member access expression to visit.</param>
        /// <returns>A constant expression if the member was resolved from a closure, otherwise a potentially modified member expression.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="memberExpression"/> is null.</exception>
        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            if (memberExpression == null)
                throw new ArgumentNullException(nameof(memberExpression));
                
            LogDebug($"Visiting member: {memberExpression.Member.Name} (Type: {memberExpression.Type})");

            // CRITICAL FIX: Check if this member access represents a closure variable pattern
            if (TryEvaluateClosureVariable(memberExpression, out var closureValue))
            {
                LogDebug($"Resolved closure variable to: {closureValue} (Type: {closureValue?.GetType().Name ?? "null"})");
                return Expression.Constant(closureValue, memberExpression.Type);
            }

            // Process the member's expression (could contain parameters)
            var visitedExpression = memberExpression.Expression != null ? Visit(memberExpression.Expression) : null;

            // If the expression changed, create a new member expression
            if (visitedExpression != memberExpression.Expression && visitedExpression != null)
            {
                LogDebug($"Member expression changed, creating new member access");
                return Expression.MakeMemberAccess(visitedExpression, memberExpression.Member);
            }

            return memberExpression;
        }

        /// <summary>
        /// Visits a binary expression and processes both operands for parameter resolution.
        /// Creates a new binary expression if either operand was modified during visitation.
        /// </summary>
        /// <param name="binaryExpression">The binary expression to visit.</param>
        /// <returns>A potentially modified binary expression with resolved parameters in its operands.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binaryExpression"/> is null.</exception>
        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            if (binaryExpression == null)
                throw new ArgumentNullException(nameof(binaryExpression));
                
            LogDebug($"Visiting binary expression: {binaryExpression.NodeType}");

            // Visit left and right sides to resolve any parameters
            var visitedLeft = Visit(binaryExpression.Left);
            var visitedRight = Visit(binaryExpression.Right);

            // If either side changed, create a new binary expression
            if (visitedLeft != binaryExpression.Left || visitedRight != binaryExpression.Right)
            {
                LogDebug($"Binary expression changed - creating new binary expression");
                
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
        /// Attempts to resolve a parameter name to its corresponding value and create a constant expression.
        /// First tries direct parameter name lookup, then falls back to query-ID prefixed parameter names.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to resolve.</param>
        /// <param name="parameterType">The expected type of the parameter.</param>
        /// <param name="resolvedExpression">When this method returns true, contains the constant expression with the resolved value.</param>
        /// <returns>True if the parameter was successfully resolved; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameterName"/> or <paramref name="parameterType"/> is null.</exception>
        private bool TryResolveParameter(string parameterName, Type parameterType, out Expression resolvedExpression)
        {
            if (parameterName == null)
                throw new ArgumentNullException(nameof(parameterName));
            if (parameterType == null)
                throw new ArgumentNullException(nameof(parameterType));
                
            resolvedExpression = null!;
            
            // Try direct parameter name lookup
            if (_parameterValues.TryGetValue(parameterName, out var value))
            {
                LogDebug($"Direct parameter resolution: {parameterName} = {value}");
                resolvedExpression = Expression.Constant(value, parameterType);
                return true;
            }
            
            // Try custom parameter resolution with query ID prefix
            if (_queryId.HasValue)
            {
                var customParameterKey = $"Q{_queryId.Value}_{parameterName}";
                if (_parameterValues.TryGetValue(customParameterKey, out var customValue))
                {
                    LogDebug($"Custom parameter resolution: {customParameterKey} = {customValue}");
                    resolvedExpression = Expression.Constant(customValue, parameterType);
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to evaluate a member expression that represents a closure variable access.
        /// This method uses secure reflection with validation to extract values from compiler-generated
        /// closure instances, and implements expression compilation caching for performance.
        /// </summary>
        /// <param name="memberExpression">The member expression to evaluate.</param>
        /// <param name="value">When this method returns true, contains the evaluated value.</param>
        /// <returns>True if the member expression was successfully evaluated; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="memberExpression"/> is null.</exception>
        private bool TryEvaluateClosureVariable(MemberExpression memberExpression, out object? value)
        {
            if (memberExpression == null)
                throw new ArgumentNullException(nameof(memberExpression));
                
            value = null;
            
            try
            {
                // SECURITY FIX: Check if the member is being accessed on a validated closure instance
                if (memberExpression.Expression is ConstantExpression closureConstant && 
                    IsValidClosureInstance(closureConstant))
                {
                    LogDebug($"Evaluating closure variable: {memberExpression.Member.Name}");
                    
                    // SECURITY FIX: Use secure reflection with validation
                    if (memberExpression.Member is FieldInfo field)
                    {
                        // Validate field access is safe
                        if (!IsSecureFieldAccess(field, closureConstant.Value))
                        {
                            LogWarning($"Security validation failed for field access: {field.Name}");
                            return false;
                        }
                        
                        value = field.GetValue(closureConstant.Value);
                        LogDebug($"Closure field value: {value}");
                        return true;
                    }
                    else if (memberExpression.Member is PropertyInfo property)
                    {
                        // Validate property access is safe
                        if (!IsSecurePropertyAccess(property, closureConstant.Value))
                        {
                            LogWarning($"Security validation failed for property access: {property.Name}");
                            return false;
                        }
                        
                        value = property.GetValue(closureConstant.Value);
                        LogDebug($"Closure property value: {value}");
                        return true;
                    }
                }
                
                // PERFORMANCE FIX: Try to evaluate the entire member expression with caching
                if (IsEvaluatableExpression(memberExpression))
                {
                    LogDebug($"Attempting cached expression evaluation");
                    value = EvaluateExpressionWithCaching(memberExpression);
                    LogDebug($"Expression evaluated to: {value}");
                    return true;
                }
            }
            catch (SecurityException ex)
            {
                LogError($"Security exception during closure variable evaluation: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"Unauthorized access during closure variable evaluation: {ex.Message}");
                return false;
            }
            catch (TargetInvocationException ex)
            {
                LogError($"Target invocation exception during closure variable evaluation: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to evaluate closure variable: {ex.Message}");
                return false;
            }
            
            return false;
        }

        /// <summary>
        /// Validates that a constant expression contains a legitimate compiler-generated closure instance.
        /// This method performs security validation to ensure only safe closure types are processed.
        /// </summary>
        /// <param name="constantExpression">The constant expression to validate.</param>
        /// <returns>True if the expression contains a valid and secure closure instance; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="constantExpression"/> is null.</exception>
        private static bool IsValidClosureInstance(ConstantExpression constantExpression)
        {
            if (constantExpression == null)
                throw new ArgumentNullException(nameof(constantExpression));
                
            if (constantExpression.Value == null) 
                return false;
            
            var type = constantExpression.Value.GetType();
            
            // SECURITY FIX: Enhanced validation for compiler-generated closure types
            // Only allow types that are definitely compiler-generated closures
            var isCompilerGenerated = Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: true);
            var isNestedPrivate = type.Attributes.HasFlag(TypeAttributes.NestedPrivate);
            var hasClosureNaming = type.Name.Contains("DisplayClass") || type.Name.StartsWith("<>", StringComparison.Ordinal);
            
            // Additional security check: ensure the type is from trusted assemblies
            var isFromTrustedAssembly = type.Assembly == typeof(AzureTableParameterInliner).Assembly ||
                                      type.Assembly.FullName?.StartsWith("System", StringComparison.Ordinal) == true ||
                                      type.Assembly.FullName?.StartsWith("Microsoft", StringComparison.Ordinal) == true;
            
            return isCompilerGenerated && (isNestedPrivate || hasClosureNaming) && isFromTrustedAssembly;
        }
        
        /// <summary>
        /// Validates that field access is secure and only allows access to compiler-generated closure fields.
        /// </summary>
        /// <param name="field">The field to validate access for.</param>
        /// <param name="instance">The instance the field belongs to.</param>
        /// <returns>True if field access is secure; otherwise, false.</returns>
        private static bool IsSecureFieldAccess(FieldInfo field, object? instance)
        {
            if (field == null || instance == null)
                return false;
                
            // Only allow access to fields in compiler-generated closure types
            var declaringType = field.DeclaringType;
            return declaringType != null && 
                   Attribute.IsDefined(declaringType, typeof(CompilerGeneratedAttribute), inherit: true);
        }
        
        /// <summary>
        /// Validates that property access is secure and only allows access to compiler-generated closure properties.
        /// </summary>
        /// <param name="property">The property to validate access for.</param>
        /// <param name="instance">The instance the property belongs to.</param>
        /// <returns>True if property access is secure; otherwise, false.</returns>
        private static bool IsSecurePropertyAccess(PropertyInfo property, object? instance)
        {
            if (property == null || instance == null)
                return false;
                
            // Only allow access to properties in compiler-generated closure types
            var declaringType = property.DeclaringType;
            return declaringType != null && 
                   Attribute.IsDefined(declaringType, typeof(CompilerGeneratedAttribute), inherit: true);
        }

        /// <summary>
        /// Determines whether an expression can be safely evaluated to a constant value.
        /// This method recursively checks expression trees to ensure they contain only
        /// constants, member accesses, and safe conversion operations.
        /// </summary>
        /// <param name="expression">The expression to evaluate for safety.</param>
        /// <returns>True if the expression can be safely evaluated; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        private static bool IsEvaluatableExpression(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
                
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return true;
                    
                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)expression;
                    // Check if accessing a field/property on a closure or constant
                    return member.Expression == null || // Static member
                           (member.Expression.NodeType == ExpressionType.Constant && 
                            member.Expression is ConstantExpression constExpr &&
                            IsValidClosureInstance(constExpr)) ||
                           IsEvaluatableExpression(member.Expression);
                           
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var unary = (UnaryExpression)expression;
                    return IsEvaluatableExpression(unary.Operand);
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// PERFORMANCE FIX: Evaluates an expression with caching to avoid recompilation in hot paths.
        /// Uses a thread-safe cache to store compiled expressions for reuse.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The evaluated result of the expression.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        private static object? EvaluateExpressionWithCaching(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
                
            // Create a cache key based on the expression tree structure
            var cacheKey = expression.ToString();
            
            // Try to get compiled expression from cache
            var compiledExpression = _compiledExpressionCache.GetOrAdd(cacheKey, _ => 
            {
                try
                {
                    var lambda = Expression.Lambda(expression);
                    var compiled = lambda.Compile();
                    return () => compiled.DynamicInvoke();
                }
                catch
                {
                    // Return a dummy function that returns null if compilation fails
                    return () => null;
                }
            });
            
            // Execute the cached compiled expression
            return compiledExpression();
        }
        
        /// <summary>
        /// Logs debug information. In a real EF Core implementation, this would use the EF Core logging infrastructure.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        private static void LogDebug(string message)
        {
            // TODO: In real EF Core implementation, replace with proper EF Core logging
            // Example: _logger?.LogDebug("[PARAM_INLINER] {Message}", message);
            Console.WriteLine($"[PARAM_INLINER] {message}");
        }
        
        /// <summary>
        /// Logs warning information. In a real EF Core implementation, this would use the EF Core logging infrastructure.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        private static void LogWarning(string message)
        {
            // TODO: In real EF Core implementation, replace with proper EF Core logging
            // Example: _logger?.LogWarning("[PARAM_INLINER] {Message}", message);
            Console.WriteLine($"[PARAM_INLINER] WARNING: {message}");
        }
        
        /// <summary>
        /// Logs error information. In a real EF Core implementation, this would use the EF Core logging infrastructure.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        private static void LogError(string message)
        {
            // TODO: In real EF Core implementation, replace with proper EF Core logging
            // Example: _logger?.LogError("[PARAM_INLINER] {Message}", message);
            Console.WriteLine($"[PARAM_INLINER] ERROR: {message}");
        }
    }

    /// <summary>
    /// Test closure class that simulates compiler-generated closure types for testing purposes.
    /// This class is marked with CompilerGenerated attribute to match real closure behavior.
    /// </summary>
    [CompilerGenerated]
    public class TestClosure
    {
        /// <summary>
        /// Simulated captured variable that would be created by the compiler for lambda expressions.
        /// </summary>
        public string? CapturedValue;
    }

    /// <summary>
    /// Test entity class representing a typical entity that would be queried in EF Core.
    /// Used for testing parameter inlining with Azure Table Storage queries.
    /// </summary>
    public class TestEntity
    {
        /// <summary>
        /// Gets or sets the name of the entity.
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Gets or sets the level of the entity.
        /// </summary>
        public int Level { get; set; }
    }
}