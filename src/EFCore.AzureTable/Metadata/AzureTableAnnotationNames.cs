// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata;

/// <summary>
///     Names for Azure Table-specific annotations.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableAnnotationNames
{
    /// <summary>
    ///     The prefix for all Azure Table annotations.
    /// </summary>
    public const string Prefix = "AzureTable:";

    /// <summary>
    ///     The annotation name for the table name in Azure Table Storage.
    /// </summary>
    public const string TableName = Prefix + "TableName";

    /// <summary>
    ///     The annotation name for the partition key property.
    /// </summary>
    public const string PartitionKey = Prefix + "PartitionKey";

    /// <summary>
    ///     The annotation name for the row key property.
    /// </summary>
    public const string RowKey = Prefix + "RowKey";

    /// <summary>
    ///     The annotation name for the ETag property used for optimistic concurrency.
    /// </summary>
    public const string ETag = Prefix + "ETag";

    /// <summary>
    ///     The annotation name for the timestamp property.
    /// </summary>
    public const string Timestamp = Prefix + "Timestamp";

    /// <summary>
    ///     The annotation name for custom property mapping in Azure Table Storage.
    /// </summary>
    public const string PropertyName = Prefix + "PropertyName";

    /// <summary>
    ///     The annotation name for JSON serialization options for complex types.
    /// </summary>
    public const string JsonSerializationOptions = Prefix + "JsonSerializationOptions";

    /// <summary>
    ///     The annotation name for maximum property size warnings.
    /// </summary>
    public const string MaxSize = Prefix + "MaxSize";

    /// <summary>
    ///     The annotation name for the default table name prefix at the model level.
    /// </summary>
    public const string DefaultTableNamePrefix = Prefix + "DefaultTableNamePrefix";

    /// <summary>
    ///     The annotation name for whether batching is enabled at the model level.
    /// </summary>
    public const string BatchingEnabled = Prefix + "BatchingEnabled";
}