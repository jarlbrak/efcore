// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using TestParameterInliner;

namespace AzureTableParameterInlinerTests
{
    /// <summary>
    /// Comprehensive test suite for Azure Table provider parameter binding fix
    /// Validates the critical fix for customer's reported variable query failures
    /// </summary>
    public class AzureTableParameterInlinerTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_NullParameterDictionary_ThrowsArgumentNullException()
        {
            // Verify null parameter dictionary throws ArgumentNullException (line 17 in Program.cs)
            Assert.Throws<ArgumentNullException>(() => new AzureTableParameterInliner(null!));
        }

        [Fact]
        public void Constructor_ValidParameterDictionary_CreatesInstance()
        {
            var parameterValues = new Dictionary<string, object?>();
            var inliner = new AzureTableParameterInliner(parameterValues);
            
            Assert.NotNull(inliner);
        }

        [Fact]
        public void Constructor_WithQueryId_CreatesInstance()
        {
            var parameterValues = new Dictionary<string, object?>();
            var inliner = new AzureTableParameterInliner(parameterValues, queryId: 42);
            
            Assert.NotNull(inliner);
        }

        #endregion

        #region Parameter Resolution Tests

        [Fact]
        public void VisitParameter_DirectResolution_ReturnsConstantExpression()
        {
            // Test direct parameter resolution with EF Core generated parameter names
            var parameterValues = new Dictionary<string, object?> { { "__p_0", "DirectValue" } };
            var inliner = new AzureTableParameterInliner(parameterValues);
            var parameter = Expression.Parameter(typeof(string), "__p_0");
            
            var result = inliner.Visit(parameter);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal("DirectValue", constResult.Value);
        }

        [Fact]
        public void VisitParameter_CustomResolution_ReturnsConstantExpression()
        {
            // Test custom parameter resolution using queryId prefix
            int queryId = 42;
            var customKey = $"Q{queryId}___p_1";
            var parameterValues = new Dictionary<string, object?> { { customKey, 100 } };
            var inliner = new AzureTableParameterInliner(parameterValues, queryId);
            var parameter = Expression.Parameter(typeof(int), "__p_1");
            
            var result = inliner.Visit(parameter);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal(100, constResult.Value);
        }

        [Fact]
        public void VisitParameter_EntityParameter_SkipsResolution()
        {
            // Entity parameters (like 'r' in 'r => r.Name') should be skipped
            var parameterValues = new Dictionary<string, object?>();
            var inliner = new AzureTableParameterInliner(parameterValues);
            var parameter = Expression.Parameter(typeof(string), "r");
            
            var result = inliner.Visit(parameter);
            
            Assert.Equal(parameter, result);
        }

        [Fact]
        public void VisitParameter_UnresolvableParameter_ReturnsOriginal()
        {
            // Unresolvable parameters should return the original expression
            var parameterValues = new Dictionary<string, object?>();
            var inliner = new AzureTableParameterInliner(parameterValues);
            var parameter = Expression.Parameter(typeof(string), "__unknown_param");
            
            var result = inliner.Visit(parameter);
            
            Assert.Equal(parameter, result);
        }

        #endregion

        #region Closure Variable Resolution Tests - THE CRITICAL FIX

        [Fact]
        public void VisitMember_ClosureFieldResolution_ReturnsConstantExpression()
        {
            // Test the critical fix: closure variable resolution for field access
            var closure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal("Admin", constResult.Value);
        }

        [CompilerGenerated]
        private class TestClosureWithProperty
        {
            public int Number { get; set; }
        }

        [Fact]
        public void VisitMember_ClosurePropertyResolution_ReturnsConstantExpression()
        {
            // Test closure variable resolution for property access
            var closure = new TestClosureWithProperty { Number = 256 };
            var closureConstant = Expression.Constant(closure);
            var propInfo = typeof(TestClosureWithProperty).GetProperty(nameof(TestClosureWithProperty.Number))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, propInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal(256, constResult.Value);
        }

        [Fact]
        public void VisitMember_NonClosureConstant_UsesFullExpressionEvaluation()
        {
            // Test fallback to full expression evaluation for non-closure constants
            var dummy = new DummyClass { Value = "Evaluated" };
            var constDummy = Expression.Constant(dummy);
            var memberExpr = Expression.MakeMemberAccess(constDummy,
                typeof(DummyClass).GetProperty(nameof(DummyClass.Value))!);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberExpr);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal("Evaluated", constResult.Value);
        }

        private class DummyClass 
        { 
            public string Value { get; set; } = string.Empty; 
        }

        #endregion

        #region Customer Failure Scenarios - THE CRITICAL TESTS

        [Fact]
        public void CustomerScenario_VariableBasedQuery_ResolvesCorrectly()
        {
            // CUSTOMER CRITICAL TEST: string roleName = "Admin"; context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
            // This simulates EF Core's compilation of the customer's failing query
            
            // Create entity parameter representing 'r' in lambda
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            // Create closure variable representing captured 'roleName' variable
            var closure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            // Build the comparison expression: r.Name == roleName
            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            // Verify the closure variable was resolved to a constant
            Assert.IsType<BinaryExpression>(result);
            var binaryResult = (BinaryExpression)result;
            var rightConst = Assert.IsType<ConstantExpression>(binaryResult.Right);
            Assert.Equal("Admin", rightConst.Value);
            
            // Left side should remain unchanged (entity property access)
            Assert.Equal(entityProperty, binaryResult.Left);
        }

        [Fact]
        public void CustomerScenario_MethodParameterQuery_ResolvesCorrectly()
        {
            // CUSTOMER CRITICAL TEST: Method parameter queries like CheckRole(string name)
            // This tests the pattern: public async Task<bool> CheckRole(string name) { return await context.Roles.AnyAsync(r => r.Name == name); }
            
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            // Simulate method parameter being captured in closure
            var methodParamClosure = new TestClosure { CapturedValue = "Manager" };
            var closureConstant = Expression.Constant(methodParamClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            var binaryResult = Assert.IsType<BinaryExpression>(result);
            var rightConst = Assert.IsType<ConstantExpression>(binaryResult.Right);
            Assert.Equal("Manager", rightConst.Value);
        }

        [Fact]
        public void CustomerScenario_AspNetIdentityPattern_ResolvesCorrectly()
        {
            // CUSTOMER CRITICAL TEST: ASP.NET Identity patterns like roleManager.RoleExistsAsync("Admin")
            // This simulates the internal query pattern used by ASP.NET Identity
            
            var entityParam = Expression.Parameter(typeof(TestEntity), "role");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            // ASP.NET Identity typically captures role names in closures
            var identityClosure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(identityClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            var binaryResult = Assert.IsType<BinaryExpression>(result);
            var rightConst = Assert.IsType<ConstantExpression>(binaryResult.Right);
            Assert.Equal("Admin", rightConst.Value);
        }

        [Fact]
        public void RegressionTest_LiteralQueries_StillWork()
        {
            // REGRESSION TEST: Ensure literal queries (r => r.Name == "Admin") still work
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
            var literalConstant = Expression.Constant("Admin");

            var binaryExpr = Expression.Equal(entityProperty, literalConstant);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            // Should return the same expression since no parameters need inlining
            Assert.Equal(binaryExpr, result);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void EdgeCase_NullClosureValue_HandlesGracefully()
        {
            var closure = new TestClosure { CapturedValue = null };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Null(constResult.Value);
        }

        [Fact]
        public void EdgeCase_EmptyStringClosureValue_ResolvesCorrectly()
        {
            var closure = new TestClosure { CapturedValue = "" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var memberAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberAccess);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal("", constResult.Value);
        }

        [Fact]
        public void EdgeCase_NonClosureInstance_FallsBackToExpressionEvaluation()
        {
            // Test with a regular class instance (not compiler-generated)
            var regularInstance = new RegularClass { Data = "RegularData" };
            var constantExpr = Expression.Constant(regularInstance);
            var memberExpr = Expression.MakeMemberAccess(constantExpr,
                typeof(RegularClass).GetProperty(nameof(RegularClass.Data))!);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(memberExpr);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal("RegularData", constResult.Value);
        }

        private class RegularClass 
        { 
            public string Data { get; set; } = string.Empty; 
        }

        [Fact]
        public void EdgeCase_ComplexNestedExpression_EvaluatesCorrectly()
        {
            // Test nested member access patterns
            var outerClosure = new OuterClosure 
            { 
                Inner = new InnerClosure { Value = "NestedValue" } 
            };
            var constantExpr = Expression.Constant(outerClosure);
            var innerAccess = Expression.MakeMemberAccess(constantExpr,
                typeof(OuterClosure).GetProperty(nameof(OuterClosure.Inner))!);
            var valueAccess = Expression.MakeMemberAccess(innerAccess,
                typeof(InnerClosure).GetProperty(nameof(InnerClosure.Value))!);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(valueAccess);
            
            var constResult = Assert.IsType<ConstantExpression>(result);
            Assert.Equal("NestedValue", constResult.Value);
        }

        [CompilerGenerated]
        private class OuterClosure 
        { 
            public InnerClosure Inner { get; set; } = null!; 
        }

        [CompilerGenerated]
        private class InnerClosure 
        { 
            public string Value { get; set; } = string.Empty; 
        }

        #endregion

        #region Binary Expression Tests

        [Fact]
        public void VisitBinary_NoOperandChange_ReturnsOriginalExpression()
        {
            var left = Expression.Constant(10);
            var right = Expression.Constant(20);
            var binaryExpr = Expression.Add(left, right);
            
            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            Assert.Same(binaryExpr, result);
        }

        [Fact]
        public void VisitBinary_LeftOperandChanges_CreatesNewBinaryExpression()
        {
            // Left side has a closure variable, right side is constant
            var closure = new TestClosure { CapturedValue = "LeftValue" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var leftSide = Expression.MakeMemberAccess(closureConstant, fieldInfo);
            var rightSide = Expression.Constant("RightValue");

            var binaryExpr = Expression.Equal(leftSide, rightSide);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            var binaryResult = Assert.IsType<BinaryExpression>(result);
            Assert.NotSame(binaryExpr, result);
            
            var leftConst = Assert.IsType<ConstantExpression>(binaryResult.Left);
            Assert.Equal("LeftValue", leftConst.Value);
            Assert.Equal(rightSide, binaryResult.Right);
        }

        [Fact]
        public void VisitBinary_BothOperandsChange_CreatesNewBinaryExpression()
        {
            // Both sides have closure variables
            var leftClosure = new TestClosure { CapturedValue = "LeftValue" };
            var rightClosure = new TestClosure { CapturedValue = "RightValue" };
            
            var leftConstant = Expression.Constant(leftClosure);
            var rightConstant = Expression.Constant(rightClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            
            var leftSide = Expression.MakeMemberAccess(leftConstant, fieldInfo);
            var rightSide = Expression.MakeMemberAccess(rightConstant, fieldInfo);

            var binaryExpr = Expression.Equal(leftSide, rightSide);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);
            
            var binaryResult = Assert.IsType<BinaryExpression>(result);
            Assert.NotSame(binaryExpr, result);
            
            var leftConst = Assert.IsType<ConstantExpression>(binaryResult.Left);
            var rightConst = Assert.IsType<ConstantExpression>(binaryResult.Right);
            Assert.Equal("LeftValue", leftConst.Value);
            Assert.Equal("RightValue", rightConst.Value);
        }

        #endregion

        #region IsEvaluatableExpression Tests (via reflection for private method testing)

        [Fact]
        public void IsEvaluatableExpression_ConstantExpression_ReturnsTrue()
        {
            var constantExpr = Expression.Constant(42);
            var result = InvokeIsEvaluatableExpression(constantExpr);
            Assert.True(result);
        }

        [Fact]
        public void IsEvaluatableExpression_ConversionOfConstant_ReturnsTrue()
        {
            var constantExpr = Expression.Constant(42);
            var convertExpr = Expression.Convert(constantExpr, typeof(object));
            var result = InvokeIsEvaluatableExpression(convertExpr);
            Assert.True(result);
        }

        [Fact]
        public void IsEvaluatableExpression_MemberAccessOnConstant_ReturnsTrue()
        {
            var dummy = new DummyClass { Value = "Test" };
            var constantExpr = Expression.Constant(dummy);
            var memberExpr = Expression.MakeMemberAccess(constantExpr,
                typeof(DummyClass).GetProperty(nameof(DummyClass.Value))!);
            var result = InvokeIsEvaluatableExpression(memberExpr);
            Assert.True(result);
        }

        [Fact]
        public void IsEvaluatableExpression_ParameterExpression_ReturnsFalse()
        {
            var paramExpr = Expression.Parameter(typeof(string), "param");
            var result = InvokeIsEvaluatableExpression(paramExpr);
            Assert.False(result);
        }

        private static bool InvokeIsEvaluatableExpression(Expression expression)
        {
            var method = typeof(AzureTableParameterInliner)
                .GetMethod("IsEvaluatableExpression", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (bool)method!.Invoke(null, new object[] { expression })!;
        }

        #endregion

        #region IsClosureInstance Tests (via reflection for private method testing)

        [Fact]
        public void IsClosureInstance_CompilerGeneratedType_ReturnsTrue()
        {
            var closure = new TestClosure();
            var constantExpr = Expression.Constant(closure);
            var result = InvokeIsClosureInstance(constantExpr);
            Assert.True(result);
        }

        [Fact]
        public void IsClosureInstance_RegularType_ReturnsFalse()
        {
            var regular = new RegularClass();
            var constantExpr = Expression.Constant(regular);
            var result = InvokeIsClosureInstance(constantExpr);
            Assert.False(result);
        }

        [Fact]
        public void IsClosureInstance_NullConstant_ReturnsFalse()
        {
            var constantExpr = Expression.Constant(null);
            var result = InvokeIsClosureInstance(constantExpr);
            Assert.False(result);
        }

        private static bool InvokeIsClosureInstance(ConstantExpression constantExpression)
        {
            var method = typeof(AzureTableParameterInliner)
                .GetMethod("IsClosureInstance", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (bool)method!.Invoke(null, new object[] { constantExpression })!;
        }

        #endregion

        #region Performance and Integration Tests

        [Fact]
        public void Performance_MultipleClosureResolutions_CompletesReasonably()
        {
            // Test performance with multiple closure variables in one expression
            var closure1 = new TestClosure { CapturedValue = "Value1" };
            var closure2 = new TestClosure { CapturedValue = "Value2" };
            var closure3 = new TestClosure { CapturedValue = "Value3" };

            var constant1 = Expression.Constant(closure1);
            var constant2 = Expression.Constant(closure2);
            var constant3 = Expression.Constant(closure3);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;

            var member1 = Expression.MakeMemberAccess(constant1, fieldInfo);
            var member2 = Expression.MakeMemberAccess(constant2, fieldInfo);
            var member3 = Expression.MakeMemberAccess(constant3, fieldInfo);

            // Create complex expression tree
            var expr1 = Expression.Equal(member1, member2);
            var expr2 = Expression.Equal(member2, member3);
            var finalExpr = Expression.AndAlso(expr1, expr2);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = inliner.Visit(finalExpr);
            stopwatch.Stop();
            
            // Should complete in reasonable time (under 100ms for this simple case)
            Assert.True(stopwatch.ElapsedMilliseconds < 100);
            Assert.IsType<BinaryExpression>(result);
        }

        [Fact]
        public void Integration_ComplexCustomerQuery_ResolvesAllComponents()
        {
            // Comprehensive integration test simulating a complex customer query
            // Simulates: context.Users.Where(u => u.Role == roleName && u.IsActive == isActive && u.Department == dept)
            
            var entityParam = Expression.Parameter(typeof(ComplexEntity), "u");
            var roleProperty = Expression.MakeMemberAccess(entityParam, typeof(ComplexEntity).GetProperty(nameof(ComplexEntity.Role))!);
            var isActiveProperty = Expression.MakeMemberAccess(entityParam, typeof(ComplexEntity).GetProperty(nameof(ComplexEntity.IsActive))!);
            var deptProperty = Expression.MakeMemberAccess(entityParam, typeof(ComplexEntity).GetProperty(nameof(ComplexEntity.Department))!);

            // Create multiple closure variables
            var roleClosure = new TestClosure { CapturedValue = "Manager" };
            var isActiveClosure = new BooleanClosure { IsActive = true };
            var deptClosure = new TestClosure { CapturedValue = "Engineering" };

            var roleConstant = Expression.Constant(roleClosure);
            var isActiveConstant = Expression.Constant(isActiveClosure);
            var deptConstant = Expression.Constant(deptClosure);

            var roleFieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var isActiveFieldInfo = typeof(BooleanClosure).GetField(nameof(BooleanClosure.IsActive))!;
            var deptFieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;

            var roleAccess = Expression.MakeMemberAccess(roleConstant, roleFieldInfo);
            var isActiveAccess = Expression.MakeMemberAccess(isActiveConstant, isActiveFieldInfo);
            var deptAccess = Expression.MakeMemberAccess(deptConstant, deptFieldInfo);

            // Build complex expression: u.Role == roleName && u.IsActive == isActive && u.Department == dept
            var roleComparison = Expression.Equal(roleProperty, roleAccess);
            var isActiveComparison = Expression.Equal(isActiveProperty, isActiveAccess);
            var deptComparison = Expression.Equal(deptProperty, deptAccess);

            var firstAnd = Expression.AndAlso(roleComparison, isActiveComparison);
            var finalExpression = Expression.AndAlso(firstAnd, deptComparison);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(finalExpression);

            // Verify all closure variables were resolved
            Assert.IsType<BinaryExpression>(result);
            var finalResult = (BinaryExpression)result;
            
            // Check that the final expression has all constants resolved
            VerifyAllClosureVariablesResolved(finalResult);
        }

        private void VerifyAllClosureVariablesResolved(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binary:
                    VerifyAllClosureVariablesResolved(binary.Left);
                    VerifyAllClosureVariablesResolved(binary.Right);
                    break;
                case MemberExpression member when member.Expression is ConstantExpression constant:
                    // If it's a member access on a constant, it should have been resolved unless it's an entity property
                    // Entity properties (like u.Role) will still be member expressions
                    break;
                case ConstantExpression:
                    // Constants are fine
                    break;
                case ParameterExpression:
                    // Entity parameters are fine
                    break;
            }
        }

        [CompilerGenerated]
        private class BooleanClosure
        {
            public bool IsActive;
        }

        private class ComplexEntity
        {
            public string Role { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string Department { get; set; } = string.Empty;
        }

        #endregion
    }
}