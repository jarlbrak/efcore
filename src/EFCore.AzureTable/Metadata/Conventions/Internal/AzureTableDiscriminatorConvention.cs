// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     A convention that configures the discriminator value for entity types as the entity type name.
///     This follows the same pattern as Cosmos DB for inheritance support in Azure Table Storage.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-conventions">Model building conventions</see> for more information and examples.
/// </remarks>
public class AzureTableDiscriminatorConvention :
    DiscriminatorConvention,
    IEntityTypeAddedConvention,
    IEntityTypeAnnotationChangedConvention,
    IEntityTypeBaseTypeChangedConvention
{
    /// <summary>
    ///     Creates a new instance of <see cref="AzureTableDiscriminatorConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    public AzureTableDiscriminatorConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    public virtual void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
        => ProcessEntityType(entityTypeBuilder);

    /// <inheritdoc />
    public virtual void ProcessEntityTypeAnnotationChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        // Process when table name changes as it affects discriminator setup
        if (name == AzureTableAnnotationNames.TableName
            && (annotation == null) != (oldAnnotation == null))
        {
            ProcessEntityType(entityTypeBuilder);
        }
    }

    /// <inheritdoc />
    public override void ProcessEntityTypeBaseTypeChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionEntityType? newBaseType,
        IConventionEntityType? oldBaseType,
        IConventionContext<IConventionEntityType> context)
    {
        if (entityTypeBuilder.Metadata.BaseType != newBaseType)
        {
            return;
        }

        var entityType = entityTypeBuilder.Metadata;
        if (newBaseType == null)
        {
            // Becoming a root type - set up discriminator if needed
            if (ShouldHaveDiscriminator(entityType))
            {
                entityTypeBuilder.HasDiscriminator(GetDiscriminatorPropertyName(), typeof(string));
            }
        }
        else
        {
            // Becoming a derived type - ensure root type has discriminator
            var rootType = newBaseType.GetRootType();
            if (ShouldHaveDiscriminator(rootType))
            {
                var discriminator = rootType.Builder.HasDiscriminator(GetDiscriminatorPropertyName(), typeof(string));
                if (discriminator != null)
                {
                    SetDefaultDiscriminatorValues(entityTypeBuilder.Metadata.GetDerivedTypesInclusive(), discriminator);
                }
            }
        }
    }

    private static void ProcessEntityType(IConventionEntityTypeBuilder entityTypeBuilder)
    {
        var entityType = entityTypeBuilder.Metadata;
        if (entityType.BaseType != null)
        {
            return;
        }

        // Set up discriminator for root types that need it
        if (ShouldHaveDiscriminator(entityType))
        {
            entityTypeBuilder.HasDiscriminator(GetDiscriminatorPropertyName(), typeof(string))
                ?.HasValue(entityType, entityType.ShortName());
        }
        else
        {
            entityTypeBuilder.HasNoDiscriminator();
        }
    }

    /// <summary>
    ///     Determines if an entity type should have a discriminator property.
    ///     In Azure Table Storage, discriminators are needed when multiple entity types
    ///     share the same table (which is the typical scenario).
    /// </summary>
    private static bool ShouldHaveDiscriminator(IConventionEntityType entityType)
    {
        // For Azure Table Storage, we need discriminators when:
        // 1. The entity type has derived types (inheritance hierarchy)
        // 2. Multiple entity types share the same table name
        
        if (entityType.GetDerivedTypes().Any())
        {
            return true;
        }

        // Check if other entity types share the same table
        var tableName = entityType.GetAzureTableName() ?? entityType.Name;
        var model = entityType.Model;
        
        return model.GetEntityTypes()
            .Where(et => et != entityType)
            .Any(et => (et.GetAzureTableName() ?? et.Name) == tableName);
    }

    /// <summary>
    ///     Gets the default discriminator property name for Azure Table Storage.
    ///     Using "Discriminator" as the property name to follow EF Core conventions.
    /// </summary>
    private static string GetDiscriminatorPropertyName() => "Discriminator";

    /// <inheritdoc />
    protected override void SetDefaultDiscriminatorValues(
        IEnumerable<IConventionEntityType> entityTypes,
        IConventionDiscriminatorBuilder discriminatorBuilder)
    {
        foreach (var entityType in entityTypes)
        {
            discriminatorBuilder.HasValue(entityType, entityType.ShortName());
        }
    }

    /// <inheritdoc />
    public override void ProcessEntityTypeRemoved(
        IConventionModelBuilder modelBuilder,
        IConventionEntityType entityType,
        IConventionContext<IConventionEntityType> context)
    {
        // Clean up discriminator if no longer needed
        var tableName = entityType.GetAzureTableName() ?? entityType.Name;
        var remainingTypesInTable = modelBuilder.Metadata.GetEntityTypes()
            .Where(et => (et.GetAzureTableName() ?? et.Name) == tableName)
            .ToList();

        // If only one type remains in the table, remove discriminator
        if (remainingTypesInTable.Count == 1)
        {
            var remainingType = remainingTypesInTable[0];
            if (remainingType.BaseType == null)
            {
                remainingType.Builder.HasNoDiscriminator();
            }
        }
    }
}