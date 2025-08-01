// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Azure Table-specific extension methods for <see cref="PropertyBuilder" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTablePropertyBuilderExtensions
{
    /// <summary>
    ///     Configures the property name to use in Azure Table Storage.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="propertyName">The name to use for the property in Azure Table Storage.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder HasPropertyName(this PropertyBuilder propertyBuilder, string propertyName)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));
        Check.NotNull(propertyName, nameof(propertyName));

        propertyBuilder.Metadata.SetPropertyName(propertyName);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures the property name to use in Azure Table Storage.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="propertyName">The name to use for the property in Azure Table Storage.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder<TProperty> HasPropertyName<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string propertyName)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));
        Check.NotNull(propertyName, nameof(propertyName));

        propertyBuilder.Metadata.SetPropertyName(propertyName);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures the Azure Table column name for this property.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="columnName">The name to use for the column in Azure Table Storage.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder ToAzureTableColumn(this PropertyBuilder propertyBuilder, string columnName)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));
        Check.NotNull(columnName, nameof(columnName));

        propertyBuilder.Metadata.SetAzureTableColumn(columnName);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures the Azure Table column name for this property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="columnName">The name to use for the column in Azure Table Storage.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder<TProperty> ToAzureTableColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string columnName)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));
        Check.NotNull(columnName, nameof(columnName));

        propertyBuilder.Metadata.SetAzureTableColumn(columnName);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the partition key for the entity.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder IsPartitionKey(this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetPartitionKeyProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the partition key for the entity.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder<TProperty> IsPartitionKey<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetPartitionKeyProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the row key for the entity.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder IsRowKey(this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetRowKeyProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the row key for the entity.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder<TProperty> IsRowKey<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetRowKeyProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the ETag for optimistic concurrency control.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder IsETag(this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetETagProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the ETag for optimistic concurrency control.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder<TProperty> IsETag<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetETagProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the timestamp for the entity.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder IsTimestamp(this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetTimestampProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }

    /// <summary>
    ///     Configures this property as the timestamp for the entity.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public static PropertyBuilder<TProperty> IsTimestamp<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        ((IMutableEntityType)propertyBuilder.Metadata.DeclaringType).SetTimestampProperty(propertyBuilder.Metadata);
        return propertyBuilder;
    }
}