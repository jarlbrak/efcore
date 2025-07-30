// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.EntityFrameworkCore.AzureTable.DataAnnotations;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableKeyDiscoveryConvention : KeyDiscoveryConvention
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableKeyDiscoveryConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    protected override void ProcessKeyProperties(IList<IConventionProperty> keyProperties, IConventionEntityType entityType)
    {
        if (keyProperties.Count == 0)
        {
            return;
        }

        // First call base implementation
        base.ProcessKeyProperties(keyProperties, entityType);

        // Then add Azure Table specific logic
        var clrType = entityType.ClrType;
        if (clrType == null)
        {
            return;
        }

        // Discover partition key from attribute or property name
        DiscoverPartitionKey(entityType, clrType);

        // Discover row key from attribute or property name  
        DiscoverRowKey(entityType, clrType);

        // Discover ETag from attribute
        DiscoverETag(entityType, clrType);

        // Discover table name from attribute
        DiscoverTableName(entityType, clrType);
    }

    private void DiscoverPartitionKey(IConventionEntityType entityType, Type clrType)
    {
        // First check for PartitionKeyAttribute
        var partitionKeyProperty = clrType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PartitionKeyAttribute>() != null);

        if (partitionKeyProperty != null)
        {
            var property = entityType.FindProperty(partitionKeyProperty.Name);
            if (property != null)
            {
                entityType.SetPartitionKeyProperty(property);
                return;
            }
        }

        // Fallback to property named "PartitionKey"
        var partitionKeyByName = clrType.GetProperties()
            .FirstOrDefault(p => p.Name.Equals("PartitionKey", StringComparison.OrdinalIgnoreCase));

        if (partitionKeyByName != null)
        {
            var property = entityType.FindProperty(partitionKeyByName.Name);
            if (property != null)
            {
                entityType.SetPartitionKeyProperty(property);
            }
        }
    }

    private void DiscoverRowKey(IConventionEntityType entityType, Type clrType)
    {
        // First check for RowKeyAttribute
        var rowKeyProperty = clrType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<RowKeyAttribute>() != null);

        if (rowKeyProperty != null)
        {
            var property = entityType.FindProperty(rowKeyProperty.Name);
            if (property != null)
            {
                entityType.SetRowKeyProperty(property);
                return;
            }
        }

        // Fallback to property named "RowKey"
        var rowKeyByName = clrType.GetProperties()
            .FirstOrDefault(p => p.Name.Equals("RowKey", StringComparison.OrdinalIgnoreCase));

        if (rowKeyByName != null)
        {
            var property = entityType.FindProperty(rowKeyByName.Name);
            if (property != null)
            {
                entityType.SetRowKeyProperty(property);
                return;
            }
        }

        // Fallback to primary key as row key
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey?.Properties.Count == 1)
        {
            var pkProperty = primaryKey.Properties[0];
            if (pkProperty.ClrType == typeof(string) || pkProperty.ClrType == typeof(int) || pkProperty.ClrType == typeof(long) || pkProperty.ClrType == typeof(Guid))
            {
                entityType.SetRowKeyProperty(pkProperty);
            }
        }
    }

    private void DiscoverETag(IConventionEntityType entityType, Type clrType)
    {
        // Check for ETagAttribute
        var etagProperty = clrType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<ETagAttribute>() != null);

        if (etagProperty != null)
        {
            var property = entityType.FindProperty(etagProperty.Name);
            if (property != null)
            {
                entityType.SetETagProperty(property);
                return;
            }
        }

        // Fallback to property named "ETag"
        var etagByName = clrType.GetProperties()
            .FirstOrDefault(p => p.Name.Equals("ETag", StringComparison.OrdinalIgnoreCase) && p.PropertyType == typeof(string));

        if (etagByName != null)
        {
            var property = entityType.FindProperty(etagByName.Name);
            if (property != null)
            {
                entityType.SetETagProperty(property);
            }
        }
    }

    private void DiscoverTableName(IConventionEntityType entityType, Type clrType)
    {
        var tableNameAttribute = clrType.GetCustomAttribute<TableNameAttribute>();
        if (tableNameAttribute != null)
        {
            entityType.SetTableName(tableNameAttribute.Name);
        }
    }
}