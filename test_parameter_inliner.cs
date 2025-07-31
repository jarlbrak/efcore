// Simple standalone test to verify our parameter inliner logic
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

// Copy the necessary classes to test standalone
namespace TestParameterInliner
{
    // Simplified version of our parameter inliner for testing
    public class AzureTableParameterInliner : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<string, object?> _parameterValues;
        private readonly int? _queryId;

        public AzureTableParameterInliner(IReadOnlyDictionary<string, object?> parameterValues, int? queryId = null)
        {
            _parameterValues = parameterValues ?? throw new ArgumentNullException(nameof(parameterValues));
            _queryId = queryId;
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            Console.WriteLine($"[PARAM_INLINER] Visiting parameter: {parameterExpression.Name} (Type: {parameterExpression.Type})");

            // Skip entity parameters (like 'r' in 'r => r.Name') - these represent the entity being filtered
            if (parameterExpression.Name != null && 
                !parameterExpression.Name.StartsWith("__", StringComparison.Ordinal))
            {
                Console.WriteLine($"[PARAM_INLINER] Skipping entity parameter: {parameterExpression.Name}");
                return parameterExpression;
            }

            // This is an EF Core generated parameter (starts with "__") - attempt to resolve it
            if (parameterExpression.Name != null && TryResolveParameter(parameterExpression.Name, parameterExpression.Type, out var resolvedExpression))
            {
                Console.WriteLine($"[PARAM_INLINER] Successfully resolved parameter {parameterExpression.Name}");
                return resolvedExpression;
            }

            Console.WriteLine($"[PARAM_INLINER] WARNING: Could not resolve parameter {parameterExpression.Name}");
            Console.WriteLine($"[PARAM_INLINER] Available parameters: {string.Join(", ", _parameterValues.Keys)}");
            
            return parameterExpression;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            Console.WriteLine($"[PARAM_INLINER] Visiting member: {memberExpression.Member.Name}");
            Console.WriteLine($"[PARAM_INLINER] Member expression: {memberExpression.Expression}");
            Console.WriteLine($"[PARAM_INLINER] Member type: {memberExpression.Type}");

            // CRITICAL FIX: Check if this member access represents a closure variable pattern
            if (TryEvaluateClosureVariable(memberExpression, out var closureValue))
            {
                Console.WriteLine($"[PARAM_INLINER] Resolved closure variable to: {closureValue} (Type: {closureValue?.GetType().Name ?? "null"})");
                return Expression.Constant(closureValue, memberExpression.Type);
            }

            // Process the member's expression (could contain parameters)
            var visitedExpression = memberExpression.Expression != null ? Visit(memberExpression.Expression) : null;

            // If the expression changed, create a new member expression
            if (visitedExpression != memberExpression.Expression && visitedExpression != null)
            {
                Console.WriteLine($"[PARAM_INLINER] Member expression changed, creating new member access");
                return Expression.MakeMemberAccess(visitedExpression, memberExpression.Member);
            }

            return memberExpression;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            Console.WriteLine($"[PARAM_INLINER] Visiting binary expression: {binaryExpression.NodeType}");
            Console.WriteLine($"[PARAM_INLINER] Left: {binaryExpression.Left}");
            Console.WriteLine($"[PARAM_INLINER] Right: {binaryExpression.Right}");

            // Visit left and right sides to resolve any parameters
            var visitedLeft = Visit(binaryExpression.Left);
            var visitedRight = Visit(binaryExpression.Right);

            // If either side changed, create a new binary expression
            if (visitedLeft != binaryExpression.Left || visitedRight != binaryExpression.Right)
            {
                Console.WriteLine($"[PARAM_INLINER] Binary expression changed:");
                Console.WriteLine($"[PARAM_INLINER] New Left: {visitedLeft}");
                Console.WriteLine($"[PARAM_INLINER] New Right: {visitedRight}");
                
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

        private bool TryResolveParameter(string parameterName, Type parameterType, out Expression resolvedExpression)
        {
            resolvedExpression = null!;
            
            // Try direct parameter name lookup
            if (_parameterValues.TryGetValue(parameterName, out var value))
            {
                Console.WriteLine($"[PARAM_INLINER] Direct parameter resolution: {parameterName} = {value}");
                resolvedExpression = Expression.Constant(value, parameterType);
                return true;
            }
            
            // Try custom parameter resolution with query ID prefix
            if (_queryId.HasValue)
            {
                var customParameterKey = $"Q{_queryId.Value}_{parameterName}";
                if (_parameterValues.TryGetValue(customParameterKey, out var customValue))
                {
                    Console.WriteLine($"[PARAM_INLINER] Custom parameter resolution: {customParameterKey} = {customValue}");
                    resolvedExpression = Expression.Constant(customValue, parameterType);
                    return true;
                }
            }
            
            return false;
        }

        private bool TryEvaluateClosureVariable(MemberExpression memberExpression, out object? value)
        {
            value = null;
            
            try
            {
                // Check if the member is being accessed on a closure instance
                if (memberExpression.Expression is ConstantExpression closureConstant && 
                    IsClosureInstance(closureConstant))
                {
                    Console.WriteLine($"[PARAM_INLINER] Evaluating closure variable: {memberExpression.Member.Name}");
                    
                    // Use reflection to get the field/property value from the closure instance
                    if (memberExpression.Member is System.Reflection.FieldInfo field)
                    {
                        value = field.GetValue(closureConstant.Value);
                        Console.WriteLine($"[PARAM_INLINER] Closure field value: {value}");
                        return true;
                    }
                    else if (memberExpression.Member is System.Reflection.PropertyInfo property)
                    {
                        value = property.GetValue(closureConstant.Value);
                        Console.WriteLine($"[PARAM_INLINER] Closure property value: {value}");
                        return true;
                    }
                }
                
                // CRITICAL: Try to evaluate the entire member expression if it looks evaluatable
                if (IsEvaluatableExpression(memberExpression))
                {
                    Console.WriteLine($"[PARAM_INLINER] Attempting full expression evaluation");
                    var compiledExpression = Expression.Lambda(memberExpression).Compile();
                    value = compiledExpression.DynamicInvoke();
                    Console.WriteLine($"[PARAM_INLINER] Full expression evaluated to: {value}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PARAM_INLINER] Failed to evaluate closure variable: {ex.Message}");
            }
            
            return false;
        }

        private static bool IsClosureInstance(ConstantExpression constantExpression)
        {
            if (constantExpression.Value == null) return false;
            
            var type = constantExpression.Value.GetType();
            
            // Check for compiler-generated closure types
            return type.Attributes.HasFlag(System.Reflection.TypeAttributes.NestedPrivate) &&
                   Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: true);
        }

        private static bool IsEvaluatableExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return true;
                    
                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)expression;
                    // Check if accessing a field/property on a closure or constant
                    return member.Expression == null || // Static member
                           member.Expression.NodeType == ExpressionType.Constant ||
                           IsEvaluatableExpression(member.Expression);
                           
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var unary = (UnaryExpression)expression;
                    return IsEvaluatableExpression(unary.Operand);
                    
                default:
                    return false;
            }
        }
    }

    // Test closure class
    [CompilerGenerated]
    public class TestClosure
    {
        public string? CapturedValue;
    }

    public class TestEntity
    {
        public string Name { get; set; } = null!;
        public int Level { get; set; }
    }

    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== TESTING AZURE TABLE PARAMETER INLINER ===");
            
            // Test 1: Direct parameter resolution
            Console.WriteLine("\n--- Test 1: Direct Parameter Resolution ---");
            var parameterValues = new Dictionary<string, object?>
            {
                ["__p_0"] = "TestValue"
            };
            var inliner = new AzureTableParameterInliner(parameterValues);
            var parameter = Expression.Parameter(typeof(string), "__p_0");
            var result = inliner.Visit(parameter);
            
            Console.WriteLine($"Original: {parameter}");
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Success: {result is ConstantExpression c && c.Value?.ToString() == "TestValue"}");
            
            // Test 2: Closure variable resolution - THE CRITICAL FIX
            Console.WriteLine("\n--- Test 2: Closure Variable Resolution (CRITICAL FIX) ---");
            var closure = new TestClosure { CapturedValue = "London" };
            var closureConstant = Expression.Constant(closure);
            var memberAccess = Expression.MakeMemberAccess(closureConstant, typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!);
            
            var inliner2 = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result2 = inliner2.Visit(memberAccess);
            
            Console.WriteLine($"Original: {memberAccess}");
            Console.WriteLine($"Result: {result2}");
            Console.WriteLine($"Success: {result2 is ConstantExpression c2 && c2.Value?.ToString() == "London"}");
            
            // Test 3: Binary expression with closure variable (THE FAILING CUSTOMER SCENARIO)
            Console.WriteLine("\n--- Test 3: Binary Expression with Closure Variable (CUSTOMER SCENARIO) ---");
            var entity = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entity, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
            
            var closure3 = new TestClosure { CapturedValue = "Admin" };
            var closureConstant3 = Expression.Constant(closure3);
            var closureAccess3 = Expression.MakeMemberAccess(closureConstant3, typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!);
            
            var comparison = Expression.Equal(entityProperty, closureAccess3);
            
            var inliner3 = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result3 = inliner3.Visit(comparison);
            
            Console.WriteLine($"Original: {comparison}");
            Console.WriteLine($"Result: {result3}");
            
            if (result3 is BinaryExpression binary)
            {
                Console.WriteLine($"Left (entity property): {binary.Left}");
                Console.WriteLine($"Right (resolved closure): {binary.Right}");
                Console.WriteLine($"Success: {binary.Right is ConstantExpression rightConst && rightConst.Value?.ToString() == "Admin"}");
            }
            
            Console.WriteLine("\n=== ALL TESTS COMPLETED ===");
        }
    }
}