// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.Update.Internal;

#pragma warning disable EF1001 // Internal EF Core API usage.

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class TableEntitySource
{
    private readonly string _tableName;
    private readonly AzureTableDatabaseWrapper _database;
    private readonly IEntityType _entityType;
    private readonly IProperty? _partitionKeyProperty;
    private readonly IProperty? _rowKeyProperty;
    private readonly IProperty? _etagProperty;
    private readonly IProperty? _timestampProperty;
    private readonly ITypeMappingSource _typeMappingSource;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TableEntitySource(IEntityType entityType, AzureTableDatabaseWrapper database, ITypeMappingSource typeMappingSource)
    {
        _tableName = entityType.GetAzureTableName() ?? entityType.ShortName();
        _database = database;
        _entityType = entityType;
        _typeMappingSource = typeMappingSource;
        
        _partitionKeyProperty = (IProperty?)entityType.GetPartitionKeyProperty();
        _rowKeyProperty = (IProperty?)entityType.GetRowKeyProperty();
        _etagProperty = (IProperty?)entityType.GetETagProperty();
        _timestampProperty = (IProperty?)entityType.GetTimestampProperty();
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string GetTableName()
        => _tableName;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string GetPartitionKey(IUpdateEntry entry)
        => _partitionKeyProperty is null
            ? throw new InvalidOperationException($"No partition key property found for entity type {_entityType.DisplayName()}")
            : (string)entry.GetCurrentProviderValue(_partitionKeyProperty)!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string GetRowKey(IUpdateEntry entry)
        => _rowKeyProperty is null
            ? throw new InvalidOperationException($"No row key property found for entity type {_entityType.DisplayName()}")
            : (string)entry.GetCurrentProviderValue(_rowKeyProperty)!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TableEntity CreateTableEntity(IUpdateEntry entry)
    {
        var tableEntity = new TableEntity(GetPartitionKey(entry), GetRowKey(entry));

        foreach (var property in entry.EntityType.GetProperties())
        {
            // Skip system properties
            if (property == _partitionKeyProperty || 
                property == _rowKeyProperty || 
                property == _timestampProperty)
            {
                continue;
            }

            var value = entry.GetCurrentProviderValue(property);
            if (value != null)
            {
                var mapping = _typeMappingSource.FindMapping(property);
                var storageValue = mapping?.Converter?.ConvertToProvider(value) ?? value;
                
                var columnName = property.Name;
                tableEntity[columnName] = storageValue;
            }
        }

        // Handle ETag for updates
        if (entry.EntityState == EntityState.Modified && _etagProperty != null)
        {
            var etag = entry.GetOriginalValue(_etagProperty) as string;
            if (!string.IsNullOrEmpty(etag))
            {
                tableEntity.ETag = new Azure.ETag(etag);
            }
        }

        return tableEntity;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void UpdateEntityFromTableEntity(IUpdateEntry entry, TableEntity tableEntity)
    {
        // Update ETag if property exists
        if (_etagProperty != null && tableEntity.ETag != default)
        {
            entry.SetStoreGeneratedValue(_etagProperty, tableEntity.ETag.ToString());
        }

        // Update Timestamp if property exists
        if (_timestampProperty != null && tableEntity.Timestamp.HasValue)
        {
            entry.SetStoreGeneratedValue(_timestampProperty, tableEntity.Timestamp.Value);
        }
    }
}

#pragma warning restore EF1001 // Internal EF Core API usage.