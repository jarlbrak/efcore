// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Diagnostics;

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableModelValidator : ModelValidator
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableModelValidator(ModelValidatorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        base.Validate(model, logger);

        // Add Azure Table-specific validation
        ValidateNoRelationships(model, logger);
        ValidateKeysConfiguration(model, logger);
        ValidatePropertyTypes(model, logger);
        ValidateDiscriminatorMappings(model, logger);
    }

    private void ValidateNoRelationships(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var navigation in entityType.GetNavigations())
            {
                logger.Logger.LogWarning(
                    AzureTableEventId.RelationshipIgnoredWarning,
                    AzureTable.Internal.AzureTableStrings.RelationshipsNotSupported);
            }
        }
    }

    private static void ValidateKeysConfiguration(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            // Validate partition key
            var partitionKeyProperty = entityType.GetPartitionKeyProperty();
            if (partitionKeyProperty == null)
            {
                throw new InvalidOperationException(
                    AzureTable.Internal.AzureTableStrings.MissingPartitionKey(entityType.DisplayName()));
            }

            // Validate partition key type
            if (partitionKeyProperty.ClrType != typeof(string))
            {
                throw new InvalidOperationException(
                    AzureTable.Internal.AzureTableStrings.InvalidPartitionKeyType(
                        partitionKeyProperty.Name,
                        entityType.DisplayName(),
                        partitionKeyProperty.ClrType.ShortDisplayName()));
            }

            // Validate row key
            var rowKeyProperty = entityType.GetRowKeyProperty();
            if (rowKeyProperty == null)
            {
                throw new InvalidOperationException(
                    AzureTable.Internal.AzureTableStrings.MissingRowKey(entityType.DisplayName()));
            }

            // Validate row key type
            if (rowKeyProperty.ClrType != typeof(string))
            {
                throw new InvalidOperationException(
                    AzureTable.Internal.AzureTableStrings.InvalidRowKeyType(
                        rowKeyProperty.Name,
                        entityType.DisplayName(),
                        rowKeyProperty.ClrType.ShortDisplayName()));
            }

            // Ensure partition key and row key are different properties
            if (partitionKeyProperty == rowKeyProperty)
            {
                throw new InvalidOperationException(
                    AzureTable.Internal.AzureTableStrings.SamePropertyPartitionAndRowKey(
                        partitionKeyProperty.Name,
                        entityType.DisplayName()));
            }
        }
    }

    private static void ValidatePropertyTypes(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        var supportedTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(byte[]),
            typeof(bool),
            typeof(bool?),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(DateTimeOffset),
            typeof(DateTimeOffset?),
            typeof(double),
            typeof(double?),
            typeof(Guid),
            typeof(Guid?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?)
        };

        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = property.ClrType;

                // Skip special Azure Table properties
                if (property.IsPartitionKey() || property.IsRowKey() || 
                    property.IsETag() || property.IsTimestamp())
                {
                    continue;
                }

                // Check if type is supported directly
                if (supportedTypes.Contains(clrType))
                {
                    continue;
                }

                // Check for enum types (will be stored as strings)
                if (clrType.IsEnum || (Nullable.GetUnderlyingType(clrType)?.IsEnum == true))
                {
                    continue;
                }

                // Complex types will be JSON serialized (logged as warning)
                if (!clrType.IsValueType && clrType != typeof(string))
                {
                    logger.Logger.LogWarning(
                        AzureTableEventId.UnsupportedQueryWarning,
                        $"The property '{property.Name}' on entity type '{entityType.DisplayName()}' is of complex type '{clrType.ShortDisplayName()}' and will be serialized as JSON. This may impact query performance.");
                    continue;
                }

                // Unsupported type
                throw new InvalidOperationException(
                    $"The property '{property.Name}' on entity type '{entityType.DisplayName()}' is of type '{clrType.ShortDisplayName()}' which is not supported by Azure Table Storage.");
            }
        }
    }

    /// <summary>
    ///     Validates discriminator mappings for inheritance hierarchies in Azure Table Storage.
    ///     Based on Cosmos DB discriminator validation patterns.
    /// </summary>
    private static void ValidateDiscriminatorMappings(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        // Group entity types by table name to validate discriminators within each table
        var tableGroups = model.GetEntityTypes()
            .Where(et => et.FindPrimaryKey() != null)
            .GroupBy(et => et.GetAzureTableName() ?? et.Name)
            .Where(g => g.Count() > 1) // Only validate tables with multiple entity types
            .ToList();

        foreach (var tableGroup in tableGroups)
        {
            var tableName = tableGroup.Key;
            var entityTypes = tableGroup.ToList();
            
            ValidateTableDiscriminatorMappings(entityTypes, tableName, logger);
        }
    }

    private static void ValidateTableDiscriminatorMappings(
        IReadOnlyList<IEntityType> entityTypes,
        string tableName,
        IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        var discriminatorValues = new Dictionary<object, IEntityType>();
        var hasDiscriminatorProperty = false;

        foreach (var entityType in entityTypes)
        {
            // Skip if this is not a root entity type in an inheritance hierarchy
            if (entityType.BaseType != null)
            {
                continue;
            }

            var discriminatorProperty = entityType.FindDiscriminatorProperty();
            if (discriminatorProperty != null)
            {
                hasDiscriminatorProperty = true;
                
                // Validate discriminator property type (should be string for Azure Table)
                if (discriminatorProperty.ClrType != typeof(string))
                {
                    throw new InvalidOperationException(
                        $"The discriminator property '{discriminatorProperty.Name}' on entity type '{entityType.DisplayName()}' " +
                        $"is of type '{discriminatorProperty.ClrType.ShortDisplayName()}', but discriminator properties must be of type 'string' for Azure Table Storage.");
                }

                // Validate discriminator values for all types in the hierarchy
                foreach (var typeInHierarchy in entityType.GetDerivedTypesInclusive())
                {
                    if (!typeInHierarchy.ClrType.IsInstantiable())
                    {
                        continue;
                    }

                    var discriminatorValue = typeInHierarchy.GetDiscriminatorValue();
                    if (discriminatorValue == null)
                    {
                        throw new InvalidOperationException(
                            $"The entity type '{typeInHierarchy.DisplayName()}' requires a discriminator value but none has been configured.");
                    }

                    if (discriminatorValues.TryGetValue(discriminatorValue, out var duplicateEntityType))
                    {
                        throw new InvalidOperationException(
                            $"The discriminator value '{discriminatorValue}' has been configured for both entity types " +
                            $"'{typeInHierarchy.DisplayName()}' and '{duplicateEntityType.DisplayName()}' " +
                            $"that share the table '{tableName}'. Each entity type must have a unique discriminator value.");
                    }

                    discriminatorValues[discriminatorValue] = typeInHierarchy;
                }
            }
        }

        // If multiple entity types share a table but no discriminator is configured, log a warning
        if (!hasDiscriminatorProperty && entityTypes.Count > 1)
        {
            var entityTypeNames = string.Join(", ", entityTypes.Select(et => et.DisplayName()));
            logger.Logger.LogWarning(
                AzureTableEventId.MultipleEntityTypesInTableWarning,
                $"Multiple entity types ({entityTypeNames}) are mapped to the same table '{tableName}' " +
                "but no discriminator property is configured. Consider configuring a discriminator property " +
                "to distinguish between different entity types.");
        }
    }
}