// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Extension methods for Azure Table metadata on <see cref="IMutableEntityType" /> and <see cref="IMutableProperty" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableMetadataExtensions
{
    /// <summary>
    ///     Gets the Azure Table Storage table name for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The table name, or null if not configured.</returns>
    public static string? GetAzureTableName(this IReadOnlyEntityType entityType)
        => entityType[AzureTableAnnotationNames.TableName] as string;

    /// <summary>
    ///     Sets the Azure Table Storage table name for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="tableName">The table name.</param>
    public static void SetTableName(this IMutableEntityType entityType, string? tableName)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.TableName, tableName);

    /// <summary>
    ///     Gets the partition key property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The partition key property, or null if not configured.</returns>
    public static IReadOnlyProperty? GetPartitionKeyProperty(this IReadOnlyEntityType entityType)
    {
        var partitionKeyName = entityType[AzureTableAnnotationNames.PartitionKey] as string;
        return partitionKeyName != null ? entityType.FindProperty(partitionKeyName) : null;
    }

    /// <summary>
    ///     Sets the partition key property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as partition key.</param>
    public static void SetPartitionKeyProperty(this IMutableEntityType entityType, IMutableProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.PartitionKey, property?.Name);

    /// <summary>
    ///     Gets the row key property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The row key property, or null if not configured.</returns>
    public static IReadOnlyProperty? GetRowKeyProperty(this IReadOnlyEntityType entityType)
    {
        var rowKeyName = entityType[AzureTableAnnotationNames.RowKey] as string;
        return rowKeyName != null ? entityType.FindProperty(rowKeyName) : null;
    }

    /// <summary>
    ///     Sets the row key property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as row key.</param>
    public static void SetRowKeyProperty(this IMutableEntityType entityType, IMutableProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.RowKey, property?.Name);

    /// <summary>
    ///     Gets the ETag property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The ETag property, or null if not configured.</returns>
    public static IReadOnlyProperty? GetETagProperty(this IReadOnlyEntityType entityType)
    {
        var etagName = entityType[AzureTableAnnotationNames.ETag] as string;
        return etagName != null ? entityType.FindProperty(etagName) : null;
    }

    /// <summary>
    ///     Sets the ETag property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as ETag.</param>
    public static void SetETagProperty(this IMutableEntityType entityType, IMutableProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.ETag, property?.Name);

    /// <summary>
    ///     Gets the timestamp property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The timestamp property, or null if not configured.</returns>
    public static IReadOnlyProperty? GetTimestampProperty(this IReadOnlyEntityType entityType)
    {
        var timestampName = entityType[AzureTableAnnotationNames.Timestamp] as string;
        return timestampName != null ? entityType.FindProperty(timestampName) : null;
    }

    /// <summary>
    ///     Sets the timestamp property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as timestamp.</param>
    public static void SetTimestampProperty(this IMutableEntityType entityType, IMutableProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.Timestamp, property?.Name);

    /// <summary>
    ///     Gets the Azure Table Storage property name for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The Azure Table Storage property name, or null to use the CLR property name.</returns>
    public static string? GetPropertyName(this IReadOnlyProperty property)
        => property[AzureTableAnnotationNames.PropertyName] as string;

    /// <summary>
    ///     Gets the Azure Table Storage column name for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The Azure Table Storage column name, or null to use the CLR property name.</returns>
    public static string? GetAzureTableColumn(this IReadOnlyProperty property)
        => property[AzureTableAnnotationNames.PropertyName] as string;

    /// <summary>
    ///     Sets the Azure Table Storage property name for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="propertyName">The Azure Table Storage property name.</param>
    public static void SetPropertyName(this IMutableProperty property, string? propertyName)
        => property.SetOrRemoveAnnotation(AzureTableAnnotationNames.PropertyName, propertyName);

    /// <summary>
    ///     Sets the Azure Table Storage column name for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="columnName">The Azure Table Storage column name.</param>
    public static void SetAzureTableColumn(this IMutableProperty property, string? columnName)
        => property.SetOrRemoveAnnotation(AzureTableAnnotationNames.PropertyName, columnName);

    /// <summary>
    ///     Gets whether the property is used as a partition key.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>True if the property is a partition key, false otherwise.</returns>
    public static bool IsPartitionKey(this IReadOnlyProperty property)
        => property.DeclaringType is IReadOnlyEntityType entityType && entityType.GetPartitionKeyProperty() == property;

    /// <summary>
    ///     Gets whether the property is used as a row key.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>True if the property is a row key, false otherwise.</returns>
    public static bool IsRowKey(this IReadOnlyProperty property)
        => property.DeclaringType is IReadOnlyEntityType entityType && entityType.GetRowKeyProperty() == property;

    /// <summary>
    ///     Gets whether the property is used as an ETag.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>True if the property is an ETag, false otherwise.</returns>
    public static bool IsETag(this IReadOnlyProperty property)
        => property.DeclaringType is IReadOnlyEntityType entityType && entityType.GetETagProperty() == property;

    /// <summary>
    ///     Gets whether the property is used as a timestamp.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>True if the property is a timestamp, false otherwise.</returns>
    public static bool IsTimestamp(this IReadOnlyProperty property)
        => property.DeclaringType is IReadOnlyEntityType entityType && entityType.GetTimestampProperty() == property;

    /// <summary>
    ///     Gets the default table name prefix for the model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The default table name prefix, or null if not configured.</returns>
    public static string? GetDefaultTableNamePrefix(this IReadOnlyModel model)
        => model[AzureTableAnnotationNames.DefaultTableNamePrefix] as string;

    /// <summary>
    ///     Sets the default table name prefix for the model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="tableNamePrefix">The default table name prefix.</param>
    public static void SetDefaultTableNamePrefix(this IMutableModel model, string? tableNamePrefix)
        => model.SetOrRemoveAnnotation(AzureTableAnnotationNames.DefaultTableNamePrefix, tableNamePrefix);

    /// <summary>
    ///     Gets whether Azure Table batching is enabled for the model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>True if batching is enabled, false otherwise.</returns>
    public static bool GetAzureTableBatchingEnabled(this IReadOnlyModel model)
        => model[AzureTableAnnotationNames.BatchingEnabled] as bool? ?? true;

    /// <summary>
    ///     Sets whether Azure Table batching is enabled for the model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="enableBatching">True to enable batching, false otherwise.</param>
    public static void SetAzureTableBatchingEnabled(this IMutableModel model, bool enableBatching)
        => model.SetOrRemoveAnnotation(AzureTableAnnotationNames.BatchingEnabled, enableBatching);

    #region Convention Overloads
    
    /// <summary>
    ///     Sets the Azure Table Storage table name for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="tableName">The table name.</param>
    public static void SetTableName(this IConventionEntityType entityType, string? tableName)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.TableName, tableName);
    
    /// <summary>
    ///     Sets the partition key property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as partition key.</param>
    public static void SetPartitionKeyProperty(this IConventionEntityType entityType, IConventionProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.PartitionKey, property?.Name);
    
    /// <summary>
    ///     Sets the row key property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as row key.</param>
    public static void SetRowKeyProperty(this IConventionEntityType entityType, IConventionProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.RowKey, property?.Name);
    
    /// <summary>
    ///     Sets the ETag property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as ETag.</param>
    public static void SetETagProperty(this IConventionEntityType entityType, IConventionProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.ETag, property?.Name);
    
    /// <summary>
    ///     Sets the timestamp property for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="property">The property to use as timestamp.</param>
    public static void SetTimestampProperty(this IConventionEntityType entityType, IConventionProperty? property)
        => entityType.SetOrRemoveAnnotation(AzureTableAnnotationNames.Timestamp, property?.Name);
    
    /// <summary>
    ///     Gets the Azure Table Storage column name for the property (convention overload).
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The Azure Table Storage column name, or null to use the CLR property name.</returns>
    public static string? GetAzureTableColumn(this IConventionProperty property)
        => property[AzureTableAnnotationNames.PropertyName] as string;

    /// <summary>
    ///     Sets the Azure Table Storage column name for the property (convention overload).
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="columnName">The Azure Table Storage column name.</param>
    public static void SetAzureTableColumn(this IConventionProperty property, string? columnName)
        => property.SetOrRemoveAnnotation(AzureTableAnnotationNames.PropertyName, columnName);
    
    #endregion
}