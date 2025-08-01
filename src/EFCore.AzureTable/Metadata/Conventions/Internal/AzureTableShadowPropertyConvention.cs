// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableShadowPropertyConvention : IEntityTypeAddedConvention, IModelFinalizingConvention
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableShadowPropertyConvention()
    {
    }

    /// <inheritdoc />
    public virtual void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        var entityType = entityTypeBuilder.Metadata;
        
        // Add ETag shadow property if not exists
        if (entityType.FindProperty("ETag") == null && entityType.FindProperty("etag") == null)
        {
            var etagProperty = entityTypeBuilder.Property(typeof(string), "ETag");
            if (etagProperty != null)
            {
                etagProperty.HasMaxLength(255);
                etagProperty.IsConcurrencyToken(true);
                etagProperty.ValueGenerated(ValueGenerated.OnAddOrUpdate);
            }
        }
        
        // Add Timestamp shadow property if not exists
        if (entityType.FindProperty("Timestamp") == null && entityType.FindProperty("timestamp") == null)
        {
            var timestampProperty = entityTypeBuilder.Property(typeof(DateTimeOffset?), "Timestamp");
            if (timestampProperty != null)
            {
                timestampProperty.ValueGenerated(ValueGenerated.OnAddOrUpdate);
            }
        }
    }

    /// <inheritdoc />
    public virtual void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            // Skip if not configured for Azure Table
            var tableName = entityType.GetAzureTableName();
            if (string.IsNullOrEmpty(tableName))
            {
                continue;
            }

            // Handle PartitionKey shadow property if property name is specified but property doesn't exist
            var existingPartitionKeyProperty = entityType.GetPartitionKeyProperty();
            var partitionKeyName = existingPartitionKeyProperty?.Name;
            if (!string.IsNullOrEmpty(partitionKeyName))
            {
                var partitionKeyProperty = entityType.FindProperty(partitionKeyName);
                if (partitionKeyProperty == null)
                {
                    // Create shadow property
                    var shadowProperty = entityType.AddProperty(
                        partitionKeyName,
                        typeof(string));
                    
                    if (shadowProperty != null)
                    {
                        shadowProperty.SetIsNullable(false);
                    }
                }
            }

            // Handle RowKey shadow property if property name is specified but property doesn't exist
            var existingRowKeyProperty = entityType.GetRowKeyProperty();
            var rowKeyName = existingRowKeyProperty?.Name;
            if (!string.IsNullOrEmpty(rowKeyName))
            {
                var rowKeyProperty = entityType.FindProperty(rowKeyName);
                if (rowKeyProperty == null)
                {
                    // Create shadow property
                    var shadowProperty = entityType.AddProperty(
                        rowKeyName,
                        typeof(string));
                    
                    if (shadowProperty != null)
                    {
                        shadowProperty.SetIsNullable(false);
                    }
                }
            }
        }
    }
}