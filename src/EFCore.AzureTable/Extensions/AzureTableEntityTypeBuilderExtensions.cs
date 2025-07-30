// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Azure Table-specific extension methods for <see cref="EntityTypeBuilder" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableEntityTypeBuilderExtensions
{
    /// <summary>
    ///     Configures the Azure Table Storage table name for this entity type.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="tableName">The name of the table in Azure Table Storage.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder ToAzureTable(this EntityTypeBuilder entityTypeBuilder, string tableName)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(tableName, nameof(tableName));

        entityTypeBuilder.Metadata.SetTableName(tableName);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the Azure Table Storage table name for this entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="tableName">The name of the table in Azure Table Storage.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder<TEntity> ToAzureTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string tableName)
        where TEntity : class
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(tableName, nameof(tableName));

        entityTypeBuilder.Metadata.SetTableName(tableName);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the partition key for this entity type.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyName">The name of the property to be used as the partition key.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder HasPartitionKey(this EntityTypeBuilder entityTypeBuilder, string propertyName)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyName, nameof(propertyName));

        var property = entityTypeBuilder.Metadata.FindProperty(propertyName);
        if (property == null)
        {
            property = entityTypeBuilder.Property(propertyName).Metadata;
        }

        entityTypeBuilder.Metadata.SetPartitionKeyProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the partition key for this entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyExpression">
    ///     A lambda expression representing the property to be used as the partition key
    ///     (<c>blog => blog.Region</c>).
    /// </param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder<TEntity> HasPartitionKey<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, object?>> propertyExpression)
        where TEntity : class
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyExpression, nameof(propertyExpression));

        var propertyInfo = propertyExpression.GetMemberAccess();
        var property = entityTypeBuilder.Metadata.FindProperty(propertyInfo.GetSimpleMemberName());
        if (property == null)
        {
            property = entityTypeBuilder.Property(propertyExpression).Metadata;
        }

        entityTypeBuilder.Metadata.SetPartitionKeyProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the row key for this entity type.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyName">The name of the property to be used as the row key.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder HasRowKey(this EntityTypeBuilder entityTypeBuilder, string propertyName)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyName, nameof(propertyName));

        var property = entityTypeBuilder.Metadata.FindProperty(propertyName);
        if (property == null)
        {
            property = entityTypeBuilder.Property(propertyName).Metadata;
        }

        entityTypeBuilder.Metadata.SetRowKeyProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the row key for this entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyExpression">
    ///     A lambda expression representing the property to be used as the row key
    ///     (<c>blog => blog.Id</c>).
    /// </param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder<TEntity> HasRowKey<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, object?>> propertyExpression)
        where TEntity : class
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyExpression, nameof(propertyExpression));

        var propertyInfo = propertyExpression.GetMemberAccess();
        var property = entityTypeBuilder.Metadata.FindProperty(propertyInfo.GetSimpleMemberName());
        if (property == null)
        {
            property = entityTypeBuilder.Property(propertyExpression).Metadata;
        }

        entityTypeBuilder.Metadata.SetRowKeyProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the ETag property for optimistic concurrency control.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyName">The name of the property to be used as the ETag.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder HasETag(this EntityTypeBuilder entityTypeBuilder, string propertyName)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyName, nameof(propertyName));

        var property = entityTypeBuilder.Metadata.FindProperty(propertyName);
        if (property == null)
        {
            property = entityTypeBuilder.Property<string>(propertyName).Metadata;
        }

        entityTypeBuilder.Metadata.SetETagProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the ETag property for optimistic concurrency control.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyExpression">
    ///     A lambda expression representing the property to be used as the ETag
    ///     (<c>blog => blog.ETag</c>).
    /// </param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder<TEntity> HasETag<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, string?>> propertyExpression)
        where TEntity : class
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyExpression, nameof(propertyExpression));

        var propertyInfo = propertyExpression.GetMemberAccess();
        var property = entityTypeBuilder.Metadata.FindProperty(propertyInfo.GetSimpleMemberName());
        if (property == null)
        {
            property = entityTypeBuilder.Property(propertyExpression).Metadata;
        }

        entityTypeBuilder.Metadata.SetETagProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the timestamp property.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyName">The name of the property to be used as the timestamp.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder HasTimestamp(this EntityTypeBuilder entityTypeBuilder, string propertyName)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyName, nameof(propertyName));

        var property = entityTypeBuilder.Metadata.FindProperty(propertyName);
        if (property == null)
        {
            property = entityTypeBuilder.Property<DateTimeOffset>(propertyName).Metadata;
        }

        entityTypeBuilder.Metadata.SetTimestampProperty(property);
        return entityTypeBuilder;
    }

    /// <summary>
    ///     Configures the timestamp property.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
    /// <param name="propertyExpression">
    ///     A lambda expression representing the property to be used as the timestamp
    ///     (<c>blog => blog.Timestamp</c>).
    /// </param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static EntityTypeBuilder<TEntity> HasTimestamp<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, DateTimeOffset>> propertyExpression)
        where TEntity : class
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
        Check.NotNull(propertyExpression, nameof(propertyExpression));

        var propertyInfo = propertyExpression.GetMemberAccess();
        var property = entityTypeBuilder.Metadata.FindProperty(propertyInfo.GetSimpleMemberName());
        if (property == null)
        {
            property = entityTypeBuilder.Property(propertyExpression).Metadata;
        }

        entityTypeBuilder.Metadata.SetTimestampProperty(property);
        return entityTypeBuilder;
    }
}