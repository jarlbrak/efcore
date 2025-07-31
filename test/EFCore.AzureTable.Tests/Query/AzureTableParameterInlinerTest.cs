// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.AzureTable.Tests.Query;

/// <summary>
/// CRITICAL UNIT TESTS: These tests verify that our AzureTableParameterInliner correctly handles
/// all the different expression patterns that EF Core generates for closure variables.
/// This is the core fix for the customer-reported parameter binding issue.
/// </summary>
public class AzureTableParameterInlinerTest
{
    /// <summary>
    /// CRITICAL TEST: Direct parameter resolution (existing functionality).
    /// </summary>
    [Fact]
    public void VisitParameter_ResolvesDirect_EFCoreGeneratedParameter()
    {
        // Arrange
        var parameterValues = new Dictionary<string, object?>
        {
            ["__p_0"] = "TestValue"
        };
        var inliner = new AzureTableParameterInliner(parameterValues);
        var parameter = Expression.Parameter(typeof(string), "__p_0");

        // Act
        var result = inliner.Visit(parameter);

        // Assert
        Assert.IsType<ConstantExpression>(result);
        var constant = (ConstantExpression)result;
        Assert.Equal("TestValue", constant.Value);
        Assert.Equal(typeof(string), constant.Type);
    }

    /// <summary>
    /// CRITICAL TEST: This tests the core fix - closure variable resolution.
    /// EF Core compiles 'string city = "London"' into member access expressions.
    /// </summary>
    [Fact]
    public void VisitMember_ResolvesClosureVariable_ReturnsConstant()
    {
        // Arrange - Simulate compiler-generated closure
        var closure = new TestClosure { CapturedValue = "London" };
        var closureConstant = Expression.Constant(closure);
        var memberAccess = Expression.MakeMemberAccess(closureConstant, typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!);
        
        var parameterValues = new Dictionary<string, object?>();
        var inliner = new AzureTableParameterInliner(parameterValues);

        // Act
        var result = inliner.Visit(memberAccess);

        // Assert
        Assert.IsType<ConstantExpression>(result);
        var constant = (ConstantExpression)result;
        Assert.Equal("London", constant.Value);
        Assert.Equal(typeof(string), constant.Type);
    }

    /// <summary>
    /// CRITICAL TEST: Tests nested closure patterns (closure.field.property).
    /// </summary>
    [Fact]
    public void VisitMember_ResolvesNestedClosurePattern()
    {
        // Arrange - Simulate nested closure access
        var nestedClosure = new NestedTestClosure { Value = new TestClosure { CapturedValue = "NestedValue" } };
        var closureConstant = Expression.Constant(nestedClosure);
        var nestedMemberAccess = Expression.MakeMemberAccess(closureConstant, typeof(NestedTestClosure).GetField(nameof(NestedTestClosure.Value))!);
        var finalMemberAccess = Expression.MakeMemberAccess(nestedMemberAccess, typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!);
        
        var parameterValues = new Dictionary<string, object?>();
        var inliner = new AzureTableParameterInliner(parameterValues);

        // Act
        var result = inliner.Visit(finalMemberAccess);

        // Assert
        Assert.IsType<ConstantExpression>(result);
        var constant = (ConstantExpression)result;
        Assert.Equal("NestedValue", constant.Value);
        Assert.Equal(typeof(string), constant.Type);
    }

    /// <summary>
    /// CRITICAL TEST: Binary expressions with closure variables (the main failing scenario).
    /// </summary>
    [Fact]
    public void VisitBinary_ResolvesClosureVariableInComparison()
    {
        // Arrange - Simulate: r.Name == closureVariable
        var entity = Expression.Parameter(typeof(TestEntity), "r");
        var entityProperty = Expression.MakeMemberAccess(entity, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
        
        var closure = new TestClosure { CapturedValue = "Admin" };
        var closureConstant = Expression.Constant(closure);
        var closureAccess = Expression.MakeMemberAccess(closureConstant, typeof(TestClosure).GetField(nameof(TestClosure.CapturedValue))!);
        
        var comparison = Expression.Equal(entityProperty, closureAccess);
        
        var parameterValues = new Dictionary<string, object?>();
        var inliner = new AzureTableParameterInliner(parameterValues);

        // Act
        var result = inliner.Visit(comparison);

        // Assert
        Assert.IsType<BinaryExpression>(result);
        var binary = (BinaryExpression)result;
        
        // Left side should remain unchanged (entity property access)
        Assert.IsType<MemberExpression>(binary.Left);
        
        // Right side should be resolved to constant
        Assert.IsType<ConstantExpression>(binary.Right);
        var rightConstant = (ConstantExpression)binary.Right;
        Assert.Equal("Admin", rightConstant.Value);
    }

    /// <summary>
    /// CRITICAL TEST: Different data types (int, DateTime, bool) in closure variables.
    /// </summary>
    [Theory]
    [InlineData(42)]
    [InlineData(true)]
    [InlineData("2023-01-01")]
    public void VisitMember_ResolvesClosureVariable_DifferentTypes(object value)
    {
        // Arrange
        object convertedValue = value is string s && DateTime.TryParse(s, out var date) ? date : value;
        var closure = new { CapturedValue = convertedValue };
        var closureConstant = Expression.Constant(closure);
        var memberInfo = closure.GetType().GetProperty("CapturedValue")!;
        var memberAccess = Expression.MakeMemberAccess(closureConstant, memberInfo);
        
        var parameterValues = new Dictionary<string, object?>();
        var inliner = new AzureTableParameterInliner(parameterValues);

        // Act
        var result = inliner.Visit(memberAccess);

        // Assert
        Assert.IsType<ConstantExpression>(result);
        var constant = (ConstantExpression)result;
        Assert.Equal(convertedValue, constant.Value);
    }

    /// <summary>
    /// CRITICAL TEST: Parameter patterns with query ID (custom parameter handling).
    /// </summary>
    [Fact]
    public void VisitParameter_ResolvesCustomParameterWithQueryId()
    {
        // Arrange
        var parameterValues = new Dictionary<string, object?>
        {
            ["Q123___p_0"] = "CustomValue"
        };
        var inliner = new AzureTableParameterInliner(parameterValues, queryId: 123);
        var parameter = Expression.Parameter(typeof(string), "__p_0");

        // Act
        var result = inliner.Visit(parameter);

        // Assert
        Assert.IsType<ConstantExpression>(result);
        var constant = (ConstantExpression)result;
        Assert.Equal("CustomValue", constant.Value);
    }

    /// <summary>
    /// CRITICAL TEST: Ensure entity parameters (like 'r' in 'r => r.Name') are not resolved.
    /// </summary>
    [Fact]
    public void VisitParameter_DoesNotResolveEntityParameters()
    {
        // Arrange
        var parameterValues = new Dictionary<string, object?>
        {
            ["r"] = "ShouldNotResolve"
        };
        var inliner = new AzureTableParameterInliner(parameterValues);
        var entityParameter = Expression.Parameter(typeof(TestEntity), "r");

        // Act
        var result = inliner.Visit(entityParameter);

        // Assert
        Assert.Same(entityParameter, result); // Should return unchanged
    }

    /// <summary>
    /// CRITICAL TEST: Mixed parameter and closure variable resolution in complex expressions.
    /// </summary>
    [Fact]
    public void VisitBinary_MixedParameterAndClosureResolution()
    {
        // Arrange - Simulate: entity.Name == parameter && entity.Level == closureVariable
        var entity = Expression.Parameter(typeof(TestEntity), "r");
        var nameProperty = Expression.MakeMemberAccess(entity, typeof(TestEntity).GetProperty(nameof(TestEntity.Name))!);
        var levelProperty = Expression.MakeMemberAccess(entity, typeof(TestEntity).GetProperty(nameof(TestEntity.Level))!);
        
        var parameter = Expression.Parameter(typeof(string), "__p_0");
        
        var closure = new { Level = 5 };
        var closureConstant = Expression.Constant(closure);
        var closureAccess = Expression.MakeMemberAccess(closureConstant, closure.GetType().GetProperty("Level")!);
        
        var nameComparison = Expression.Equal(nameProperty, parameter);
        var levelComparison = Expression.Equal(levelProperty, closureAccess);
        var andExpression = Expression.AndAlso(nameComparison, levelComparison);
        
        var parameterValues = new Dictionary<string, object?>
        {
            ["__p_0"] = "TestName"
        };
        var inliner = new AzureTableParameterInliner(parameterValues);

        // Act
        var result = inliner.Visit(andExpression);

        // Assert
        Assert.IsType<BinaryExpression>(result);
        var binary = (BinaryExpression)result;
        
        // Left side (name comparison)
        var leftBinary = (BinaryExpression)binary.Left;
        Assert.IsType<ConstantExpression>(leftBinary.Right);
        Assert.Equal("TestName", ((ConstantExpression)leftBinary.Right).Value);
        
        // Right side (level comparison)
        var rightBinary = (BinaryExpression)binary.Right;
        Assert.IsType<ConstantExpression>(rightBinary.Right);
        Assert.Equal(5, ((ConstantExpression)rightBinary.Right).Value);
    }

    /// <summary>
    /// Test closure class that simulates compiler-generated closure types.
    /// Uses CompilerGenerated attribute to mimic real closure behavior.
    /// </summary>
    [CompilerGenerated]
    private class TestClosure
    {
        public string? CapturedValue;
    }

    /// <summary>
    /// Test nested closure class for complex scenarios.
    /// </summary>
    [CompilerGenerated]
    private class NestedTestClosure
    {
        public TestClosure? Value;
    }

    /// <summary>
    /// Test entity class for simulating entity property access.
    /// </summary>
    private class TestEntity
    {
        public string Name { get; set; } = null!;
        public int Level { get; set; }
    }
}