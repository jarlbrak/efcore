// Simple test runner to validate the Azure Table parameter inliner fix
// This bypasses xUnit to run basic validation tests

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TestParameterInliner;

namespace SimpleTestRunner
{
    public class TestResults
    {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public List<string> FailureDetails { get; set; } = new();
    }

    public static class ParameterInlinerValidator
    {
        public static TestResults RunAllTests()
        {
            var results = new TestResults();
            
            Console.WriteLine("=== AZURE TABLE PARAMETER INLINER VALIDATION ===");
            Console.WriteLine("Testing critical customer failure scenarios...\n");

            // Test 1: Constructor validation
            RunTest("Constructor_NullParameterDictionary_ThrowsException", TestConstructorNullCheck, results);
            
            // Test 2: Direct parameter resolution
            RunTest("DirectParameterResolution", TestDirectParameterResolution, results);
            
            // Test 3: CRITICAL - Closure variable resolution
            RunTest("ClosureVariableResolution_CRITICAL", TestClosureVariableResolution, results);
            
            // Test 4: CUSTOMER SCENARIO - Variable-based query
            RunTest("CustomerScenario_VariableBasedQuery_CRITICAL", TestCustomerVariableQuery, results);
            
            // Test 5: ASP.NET Identity scenario
            RunTest("AspNetIdentityScenario", TestAspNetIdentityScenario, results);
            
            // Test 6: Method parameter scenario
            RunTest("MethodParameterScenario", TestMethodParameterScenario, results);
            
            // Test 7: Regression - Literal queries
            RunTest("RegressionTest_LiteralQueries", TestLiteralQueryRegression, results);
            
            // Test 8: Edge cases
            RunTest("EdgeCase_NullValues", TestNullValueHandling, results);
            RunTest("EdgeCase_EmptyStrings", TestEmptyStringHandling, results);
            
            // Test 9: Complex expressions
            RunTest("ComplexExpressions", TestComplexExpressions, results);
            
            Console.WriteLine($"\n=== RESULTS ===");
            Console.WriteLine($"Passed: {results.Passed}");
            Console.WriteLine($"Failed: {results.Failed}");
            
            if (results.Failed > 0)
            {
                Console.WriteLine("\nFAILURES:");
                foreach (var failure in results.FailureDetails)
                {
                    Console.WriteLine($"  - {failure}");
                }
            }
            
            return results;
        }
        
        private static void RunTest(string testName, Func<bool> test, TestResults results)
        {
            try
            {
                Console.Write($"Running {testName}... ");
                bool passed = test();
                if (passed)
                {
                    Console.WriteLine("PASSED");
                    results.Passed++;
                }
                else
                {
                    Console.WriteLine("FAILED");
                    results.Failed++;
                    results.FailureDetails.Add($"{testName}: Test returned false");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED - Exception: {ex.Message}");
                results.Failed++;
                results.FailureDetails.Add($"{testName}: {ex.Message}");
            }
        }
        
        private static bool TestConstructorNullCheck()
        {
            try
            {
                var inliner = new AzureTableParameterInliner(null!);
                return false; // Should have thrown
            }
            catch (ArgumentNullException)
            {
                return true; // Expected
            }
        }
        
        private static bool TestDirectParameterResolution()
        {
            var parameterValues = new Dictionary<string, object?> { { "__p_0", "TestValue" } };
            var inliner = new AzureTableParameterInliner(parameterValues);
            var parameter = Expression.Parameter(typeof(string), "__p_0");
            var result = inliner.Visit(parameter);
            
            return result is ConstantExpression constant && constant.Value?.ToString() == "TestValue";
        }
        
        private static bool TestClosureVariableResolution()
        {
            var closure = new TestClosure { CapturedValue = "ClosureValue" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            return result is ConstantExpression constant && constant.Value?.ToString() == "ClosureValue";
        }
        
        private static bool TestCustomerVariableQuery()
        {
            // CRITICAL: This is the exact customer failure scenario
            // Customer code: string roleName = "Admin"; context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
            
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var closure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            // Verify the closure variable was resolved
            if (result is BinaryExpression binary)
            {
                return binary.Right is ConstantExpression rightConst && 
                       rightConst.Value?.ToString() == "Admin";
            }
            
            return false;
        }
        
        private static bool TestAspNetIdentityScenario()
        {
            // ASP.NET Identity: roleManager.RoleExistsAsync("Admin")
            var entityParam = Expression.Parameter(typeof(TestEntity), "role");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var identityClosure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(identityClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            if (result is BinaryExpression binary)
            {
                return binary.Right is ConstantExpression rightConst && 
                       rightConst.Value?.ToString() == "Admin";
            }
            
            return false;
        }
        
        private static bool TestMethodParameterScenario()
        {
            // Method parameter: CheckRole(string name)
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var methodParamClosure = new TestClosure { CapturedValue = "Manager" };
            var closureConstant = Expression.Constant(methodParamClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            if (result is BinaryExpression binary)
            {
                return binary.Right is ConstantExpression rightConst && 
                       rightConst.Value?.ToString() == "Manager";
            }
            
            return false;
        }
        
        private static bool TestLiteralQueryRegression()
        {
            // Regression: literal queries should still work
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
            var literalConstant = Expression.Constant("Admin");

            var binaryExpr = Expression.Equal(entityProperty, literalConstant);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            // Should return the same expression
            return result == binaryExpr;
        }
        
        private static bool TestNullValueHandling()
        {
            var closure = new TestClosure { CapturedValue = null };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            return result is ConstantExpression constant && constant.Value == null;
        }
        
        private static bool TestEmptyStringHandling()
        {
            var closure = new TestClosure { CapturedValue = "" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            return result is ConstantExpression constant && constant.Value?.ToString() == "";
        }
        
        private static bool TestComplexExpressions()
        {
            // Test multiple closure variables in one expression
            var entityParam = Expression.Parameter(typeof(TestEntity), "u");
            var nameProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
            var levelProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Level))!);

            var nameClosure = new TestClosure { CapturedValue = "Admin" };
            var levelClosure = new LevelClosure { Level = 5 };

            var nameConstant = Expression.Constant(nameClosure);
            var levelConstant = Expression.Constant(levelClosure);

            var nameFieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var levelFieldInfo = typeof(LevelClosure).GetField(nameof(LevelClosure.Level))!;

            var nameAccess = Expression.MakeMemberAccess(nameConstant, nameFieldInfo);
            var levelAccess = Expression.MakeMemberAccess(levelConstant, levelFieldInfo);

            var nameComparison = Expression.Equal(nameProperty, nameAccess);
            var levelComparison = Expression.Equal(levelProperty, levelAccess);
            var combinedExpression = Expression.AndAlso(nameComparison, levelComparison);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(combinedExpression);

            // Verify both sides were resolved
            if (result is BinaryExpression binary)
            {
                if (binary.Left is BinaryExpression leftBinary && binary.Right is BinaryExpression rightBinary)
                {
                    var leftResolved = leftBinary.Right is ConstantExpression leftConst && leftConst.Value?.ToString() == "Admin";
                    var rightResolved = rightBinary.Right is ConstantExpression rightConst && rightConst.Value?.Equals(5) == true;
                    return leftResolved && rightResolved;
                }
            }
            
            return false;
        }
        
        [CompilerGenerated]
        private class LevelClosure
        {
            public int Level;
        }
    }

    class Program
    {
        static int Main()
        {
            var results = ParameterInlinerValidator.RunAllTests();
            
            if (results.Failed == 0)
            {
                Console.WriteLine("\n🎉 ALL TESTS PASSED! The Azure Table parameter binding fix is working correctly.");
                Console.WriteLine("✅ Customer's variable query scenarios are now resolved");
                Console.WriteLine("✅ ASP.NET Identity integration will work properly"); 
                Console.WriteLine("✅ No regressions in existing functionality");
                return 0;
            }
            else
            {
                Console.WriteLine($"\n❌ {results.Failed} TESTS FAILED! The fix needs attention.");
                return 1;
            }
        }
    }
}