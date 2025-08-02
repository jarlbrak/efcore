// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableKeyDiscoveryConvention : IModelFinalizingConvention
{
    private readonly ProviderConventionSetBuilderDependencies _dependencies;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableKeyDiscoveryConvention(ProviderConventionSetBuilderDependencies dependencies)
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
            // Skip if not configured for Azure Table or if it's an owned type
            if (string.IsNullOrEmpty(entityType.GetAzureTableName()) || 
                entityType.IsOwned() ||
                entityType.ClrType == null)
            {
                continue;
            }

            // Skip if keys are already explicitly configured
            if (HasExplicitKeyConfiguration(entityType))
            {
                continue;
            }

            var partitionKeyProperty = DiscoverPartitionKey(entityType);
            var rowKeyProperty = DiscoverRowKey(entityType);

            // Configure discovered keys
            if (partitionKeyProperty != null)
            {
                entityType.SetPartitionKeyProperty(partitionKeyProperty);
            }

            if (rowKeyProperty != null)
            {
                entityType.SetRowKeyProperty(rowKeyProperty);
            }

            // Validate that we have both keys or provide helpful error
            ValidateKeyConfiguration(entityType, partitionKeyProperty, rowKeyProperty);

            // Configure composite primary key if both keys are discovered
            ConfigureCompositeKey(entityType, partitionKeyProperty, rowKeyProperty);
        }
    }

    private static bool HasExplicitKeyConfiguration(IConventionEntityType entityType)
    {
        return entityType.GetPartitionKeyProperty() != null || 
               entityType.GetRowKeyProperty() != null ||
               entityType.GetKeys().Any(k => !k.IsInModel);
    }

    private static IConventionProperty? DiscoverPartitionKey(IConventionEntityType entityType)
    {
        // Phase 0: Check if data annotations have already configured a partition key
        var existingPartitionKey = entityType.GetPartitionKeyProperty();
        if (existingPartitionKey != null)
        {
            return entityType.FindProperty(existingPartitionKey.Name);
        }

        var properties = entityType.GetProperties().ToList();
        
        // Phase 1: Explicit property names
        var explicitPartitionKey = properties.FirstOrDefault(p => 
            string.Equals(p.Name, "PartitionKey", StringComparison.OrdinalIgnoreCase));
        if (explicitPartitionKey != null)
        {
            return explicitPartitionKey;
        }

        // Find what will be used as row key to avoid conflicts
        var rowKeyProperty = DiscoverRowKey(entityType);

        // Phase 2: Look for other properties ending with "Id" that aren't the row key
        var otherIdProperty = properties.FirstOrDefault(p => 
            p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
            p != rowKeyProperty);
        if (otherIdProperty != null)
        {
            return otherIdProperty;
        }

        return null;
    }

    private static IConventionProperty? DiscoverRowKey(IConventionEntityType entityType)
    {
        // Phase 0: Check if data annotations have already configured a row key
        var existingRowKey = entityType.GetRowKeyProperty();
        if (existingRowKey != null)
        {
            return entityType.FindProperty(existingRowKey.Name);
        }

        var properties = entityType.GetProperties().ToList();
        
        // Phase 1: Explicit property names
        var explicitRowKey = properties.FirstOrDefault(p => 
            string.Equals(p.Name, "RowKey", StringComparison.OrdinalIgnoreCase));
        if (explicitRowKey != null)
        {
            return explicitRowKey;
        }

        // Phase 2: Standard EF Core patterns
        // Prefer "Id" over "{EntityName}Id"
        var idProperty = properties.FirstOrDefault(p => 
            string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
        if (idProperty != null)
        {
            return idProperty;
        }

        // Look for "{EntityName}Id" pattern
        var entityNameIdPattern = $"{entityType.ShortName()}Id";
        var entityNameIdProperty = properties.FirstOrDefault(p => 
            string.Equals(p.Name, entityNameIdPattern, StringComparison.OrdinalIgnoreCase));
        if (entityNameIdProperty != null)
        {
            return entityNameIdProperty;
        }

        return null;
    }

    private static void ValidateKeyConfiguration(
        IConventionEntityType entityType, 
        IConventionProperty? partitionKeyProperty, 
        IConventionProperty? rowKeyProperty)
    {
        var errors = new List<string>();

        if (partitionKeyProperty == null)
        {
            errors.Add("No partition key could be discovered.");
        }

        if (rowKeyProperty == null)
        {
            errors.Add("No row key could be discovered.");
        }

        if (errors.Any())
        {
            var errorMessage = BuildErrorMessage(entityType, errors, partitionKeyProperty, rowKeyProperty);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static string BuildErrorMessage(
        IConventionEntityType entityType, 
        List<string> errors,
        IConventionProperty? partitionKeyProperty, 
        IConventionProperty? rowKeyProperty)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Unable to configure keys for entity type '{entityType.DisplayName()}':");
        
        foreach (var error in errors)
        {
            sb.AppendLine($"  - {error}");
        }

        sb.AppendLine();
        sb.AppendLine("Azure Table Storage requires both a partition key and row key. Consider one of these options:");
        sb.AppendLine();

        // Suggest explicit configuration
        sb.AppendLine("1. Use explicit configuration in OnModelCreating:");
        sb.AppendLine("   modelBuilder.Entity<{entityType.ShortName()}>()");
        sb.AppendLine("       .HasPartitionKey(e => e.PropertyName)");
        sb.AppendLine("       .HasRowKey(e => e.PropertyName);");
        sb.AppendLine();

        // Suggest conventional naming
        sb.AppendLine("2. Use conventional property names:");
        if (partitionKeyProperty == null)
        {
            sb.AppendLine("   - Add a 'PartitionKey' property, or");
            sb.AppendLine("   - Use common patterns like 'UserId', 'TenantId', 'Category', 'Region'");
        }
        if (rowKeyProperty == null)
        {
            sb.AppendLine("   - Add an 'Id' or 'RowKey' property, or");
            sb.AppendLine($"   - Use the pattern '{entityType.ShortName()}Id'");
        }

        return sb.ToString();
    }

    private static void ConfigureCompositeKey(
        IConventionEntityType entityType,
        IConventionProperty? partitionKeyProperty,
        IConventionProperty? rowKeyProperty)
    {
        if (partitionKeyProperty != null && rowKeyProperty != null)
        {
            // Ensure properties are not nullable and are string type (for Azure Table compatibility)
            ConfigureKeyProperty(partitionKeyProperty);
            ConfigureKeyProperty(rowKeyProperty);

            // Configure composite primary key if not already configured
            var existingKey = entityType.FindPrimaryKey();
            if (existingKey == null)
            {
                var keyProperties = new[] { partitionKeyProperty, rowKeyProperty };
                entityType.SetPrimaryKey(keyProperties);
            }
        }
    }

    private static void ConfigureKeyProperty(IConventionProperty property)
    {
        // Ensure key properties are not nullable
        if (property.IsNullable)
        {
            property.SetIsNullable(false);
        }

        // For Azure Table Storage, keys must be strings or convertible to strings
        if (property.ClrType != typeof(string) && property.GetValueConverter() == null)
        {
            // Add value converter for non-string types
            property.SetValueConverter(typeof(string));
        }
    }
}