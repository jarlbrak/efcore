// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableExpression : Expression
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableExpression(IEntityType entityType)
    {
        EntityType = entityType;
    }

    /// <summary>
    ///     The entity type being queried.
    /// </summary>
    public virtual IEntityType EntityType { get; }

    /// <summary>
    ///     The table name in Azure Table Storage.
    /// </summary>
    public virtual string TableName => EntityType.GetAzureTableName() ?? EntityType.Name;

    /// <inheritdoc />
    public override Type Type => typeof(object);

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <inheritdoc />
    public override bool CanReduce => true;

    /// <inheritdoc />
    public override Expression Reduce()
    {
        // Reduce to a method call that can be compiled
        // This should never be called if VisitExtension handles it properly, but it's a safety net
        var tableMethodInfo = typeof(AzureTableExpression).GetMethod(nameof(CreateTable), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return Expression.Call(tableMethodInfo, Expression.Constant(EntityType), Expression.Constant(TableName));
    }

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;

    /// <inheritdoc />
    public override string ToString() => $"AzureTable({TableName})";

    private static IEnumerable<object> CreateTable(IEntityType entityType, string tableName)
    {
        // This is a placeholder that should never be called at runtime
        // If this is called, it means the expression wasn't properly handled by VisitExtension
        throw new InvalidOperationException("AzureTableExpression was not properly handled during query compilation.");
    }
}