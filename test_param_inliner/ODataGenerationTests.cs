// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Xunit;
using TestParameterInliner;

namespace AzureTableParameterInlinerTests
{
    /// <summary>
    /// Tests specifically focused on validating that the parameter inliner produces
    /// expressions that would generate correct OData filters for Azure Table queries
    /// </summary>
    public class ODataGenerationValidationTests
    {
        /// <summary>
        /// Simulates the OData generation process to verify correct filter strings
        /// In real Azure Table provider, this would generate OData like "Name eq 'Admin'"
        /// </summary>
        private static string SimulateODataGeneration(Expression expression)
        {
            if (expression is BinaryExpression binary && binary.NodeType == ExpressionType.Equal)
            {
                var leftStr = GetODataPropertyName(binary.Left);
                var rightStr = GetODataValue(binary.Right);
                return $"{leftStr} eq {rightStr}";
            }
            return expression.ToString();
        }

        private static string GetODataPropertyName(Expression expression)
        {
            if (expression is MemberExpression member && 
                member.Expression is ParameterExpression)
            {
                return member.Member.Name;
            }
            return expression.ToString();
        }

        private static string GetODataValue(Expression expression)
        {
            if (expression is ConstantExpression constant)
            {
                if (constant.Value is string stringValue)
                {
                    return $"'{stringValue}'";
                }
                return constant.Value?.ToString() ?? "null";
            }
            return expression.ToString();
        }

        [Fact]
        public void ODataGeneration_VariableQuery_ProducesCorrectFilter()
        {
            // CRITICAL: This test validates the customer's specific failure scenario
            // Before fix: Would generate "Name eq " (missing value)
            // After fix: Should generate "Name eq 'Admin'"
            
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            // Simulate the closure variable from: string roleName = "Admin";
            var closure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            // Simulate OData generation
            var odataFilter = SimulateODataGeneration(result);
            
            // This should generate correct OData: "Name eq 'Admin'"
            Assert.Equal("Name eq 'Admin'", odataFilter);
        }

        [Fact]
        public void ODataGeneration_LiteralQuery_StillProducesCorrectFilter()
        {
            // REGRESSION: Ensure literal queries still work correctly
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
            var literalConstant = Expression.Constant("Admin");

            var binaryExpr = Expression.Equal(entityProperty, literalConstant);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            Assert.Equal("Name eq 'Admin'", odataFilter);
        }

        [Fact]
        public void ODataGeneration_MethodParameterQuery_ProducesCorrectFilter()
        {
            // Test method parameter scenario: CheckRole(string name)
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var methodParamClosure = new TestClosure { CapturedValue = "Manager" };
            var closureConstant = Expression.Constant(methodParamClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            Assert.Equal("Name eq 'Manager'", odataFilter);
        }

        [Fact]
        public void ODataGeneration_NumericClosureVariable_ProducesCorrectFilter()
        {
            // Test numeric closure variables
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Level))!);

            var numericClosure = new NumericClosure { Level = 5 };
            var closureConstant = Expression.Constant(numericClosure);
            var fieldInfo = typeof(NumericClosure).GetField(nameof(NumericClosure.Level))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            Assert.Equal("Level eq 5", odataFilter);
        }

        [CompilerGenerated]
        private class NumericClosure
        {
            public int Level;
        }

        [Fact]
        public void ODataGeneration_ComplexQuery_ProducesCorrectFilter()
        {
            // Test complex query with multiple conditions: r => r.Name == roleName && r.Level > minLevel
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var nameProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
            var levelProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Level))!);

            // First closure variable: roleName
            var nameClosure = new TestClosure { CapturedValue = "Admin" };
            var nameClosureConstant = Expression.Constant(nameClosure);
            var nameFieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var nameClosureAccess = Expression.MakeMemberAccess(nameClosureConstant, nameFieldInfo);

            // Second closure variable: minLevel
            var levelClosure = new NumericClosure { Level = 3 };
            var levelClosureConstant = Expression.Constant(levelClosure);
            var levelFieldInfo = typeof(NumericClosure).GetField(nameof(NumericClosure.Level))!;
            var levelClosureAccess = Expression.MakeMemberAccess(levelClosureConstant, levelFieldInfo);

            // Build expressions
            var nameComparison = Expression.Equal(nameProperty, nameClosureAccess);
            var levelComparison = Expression.GreaterThan(levelProperty, levelClosureAccess);
            var combinedExpression = Expression.AndAlso(nameComparison, levelComparison);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(combinedExpression);

            // Verify both closure variables were resolved
            Assert.IsType<BinaryExpression>(result);
            var binaryResult = (BinaryExpression)result;
            
            // Check left side (Name == "Admin")
            var leftBinary = Assert.IsType<BinaryExpression>(binaryResult.Left);
            var leftRight = Assert.IsType<ConstantExpression>(leftBinary.Right);
            Assert.Equal("Admin", leftRight.Value);

            // Check right side (Level > 3)
            var rightBinary = Assert.IsType<BinaryExpression>(binaryResult.Right);
            var rightRight = Assert.IsType<ConstantExpression>(rightBinary.Right);
            Assert.Equal(3, rightRight.Value);
        }

        [Fact]
        public void ODataGeneration_NullStringVariable_ProducesCorrectFilter()
        {
            // Test null string handling (important for optional filters)
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var nullClosure = new TestClosure { CapturedValue = null };
            var closureConstant = Expression.Constant(nullClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            Assert.Equal("Name eq null", odataFilter);
        }

        [Fact]
        public void ODataGeneration_EmptyStringVariable_ProducesCorrectFilter()
        {
            // Test empty string handling
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var emptyClosure = new TestClosure { CapturedValue = "" };
            var closureConstant = Expression.Constant(emptyClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            Assert.Equal("Name eq ''", odataFilter);
        }

        [Theory]
        [InlineData("Admin", "Name eq 'Admin'")]
        [InlineData("Manager", "Name eq 'Manager'")]
        [InlineData("User", "Name eq 'User'")]
        [InlineData("", "Name eq ''")]
        public void ODataGeneration_VariousStringValues_ProducesCorrectFilters(string inputValue, string expectedOData)
        {
            // Parameterized test for various string values
            var entityParam = Expression.Parameter(typeof(TestEntity), "r");
            var entityProperty = Expression.MakeMemberAccess(entityParam, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);

            var closure = new TestClosure { CapturedValue = inputValue };
            var closureConstant = Expression.Constant(closure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(entityProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            Assert.Equal(expectedOData, odataFilter);
        }

        [Fact]
        public void ODataGeneration_AspNetIdentityScenario_ProducesCorrectFilter()
        {
            // Specific test for ASP.NET Identity RoleManager.RoleExistsAsync pattern
            // This simulates the internal query generated by ASP.NET Identity
            var entityParam = Expression.Parameter(typeof(IdentityRole), "role");
            var nameProperty = Expression.MakeMemberAccess(entityParam, typeof(IdentityRole).GetProperty(nameof(IdentityRole.Name))!);

            // ASP.NET Identity passes role names through closures
            var identityClosure = new TestClosure { CapturedValue = "Admin" };
            var closureConstant = Expression.Constant(identityClosure);
            var fieldInfo = typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!;
            var closureAccess = Expression.MakeMemberAccess(closureConstant, fieldInfo);

            var binaryExpr = Expression.Equal(nameProperty, closureAccess);

            var inliner = new AzureTableParameterInliner(new Dictionary<string, object?>());
            var result = inliner.Visit(binaryExpr);

            var odataFilter = SimulateODataGeneration(result);
            
            // Critical: This should generate proper OData for ASP.NET Identity
            Assert.Equal("Name eq 'Admin'", odataFilter);
        }

        /// <summary>
        /// Simplified IdentityRole class for testing ASP.NET Identity scenarios
        /// </summary>
        private class IdentityRole
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}