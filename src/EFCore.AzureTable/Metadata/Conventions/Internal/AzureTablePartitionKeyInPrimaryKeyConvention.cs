// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTablePartitionKeyInPrimaryKeyConvention : IModelFinalizingConvention
{
    private readonly ProviderConventionSetBuilderDependencies _dependencies;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTablePartitionKeyInPrimaryKeyConvention(
        ProviderConventionSetBuilderDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    /// <inheritdoc />
    public virtual void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (entityType.BaseType != null || entityType.IsOwned())
            {
                continue;
            }

            var partitionKeyProperty = entityType.GetPartitionKeyProperty();
            var rowKeyProperty = entityType.GetRowKeyProperty();

            if (partitionKeyProperty != null && rowKeyProperty != null)
            {
                // Convert to IConventionProperty by finding them on the convention entity type
                var conventionPartitionKey = entityType.FindProperty(partitionKeyProperty.Name);
                var conventionRowKey = entityType.FindProperty(rowKeyProperty.Name);
                
                if (conventionPartitionKey != null && conventionRowKey != null)
                {
                    var keyProperties = new List<IConventionProperty> { conventionPartitionKey, conventionRowKey };
                
                    // Ensure the primary key includes both partition key and row key
                    var primaryKey = entityType.FindPrimaryKey();
                    if (primaryKey == null || !KeyPropertiesMatch(primaryKey.Properties, keyProperties))
                    {
                        entityType.SetPrimaryKey(keyProperties);
                    }
                }
            }
        }
    }

    private static bool KeyPropertiesMatch(IReadOnlyList<IConventionProperty> keyProperties, List<IConventionProperty> expectedProperties)
    {
        if (keyProperties.Count != expectedProperties.Count)
        {
            return false;
        }

        for (var i = 0; i < keyProperties.Count; i++)
        {
            if (keyProperties[i] != expectedProperties[i])
            {
                return false;
            }
        }

        return true;
    }
}