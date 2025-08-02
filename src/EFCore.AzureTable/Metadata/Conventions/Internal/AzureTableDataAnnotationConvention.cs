// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.DataAnnotations;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTablePartitionKeyDataAnnotationConvention : PropertyAttributeConventionBase<PartitionKeyAttribute>
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTablePartitionKeyDataAnnotationConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    protected override void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        PartitionKeyAttribute attribute,
        MemberInfo clrMember,
        IConventionContext context)
    {
        var entityType = (IConventionEntityType)propertyBuilder.Metadata.DeclaringType;
        var property = propertyBuilder.Metadata;

        // Set this property as the partition key for the entity type
        entityType.SetPartitionKeyProperty(property);

        // Ensure partition key properties are properly configured
        ConfigurePartitionKeyProperty(property);
    }

    private static void ConfigurePartitionKeyProperty(IConventionProperty property)
    {
        // Ensure partition key properties are not nullable
        if (property.IsNullable)
        {
            property.SetIsNullable(false);
        }

        // For Azure Table Storage, partition keys should be strings
        if (property.ClrType != typeof(string) && property.GetValueConverter() == null)
        {
            // Add value converter for non-string types to convert to strings
            property.SetValueConverter(typeof(string));
        }
    }
}

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableRowKeyDataAnnotationConvention : PropertyAttributeConventionBase<RowKeyAttribute>
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableRowKeyDataAnnotationConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    protected override void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        RowKeyAttribute attribute,
        MemberInfo clrMember,
        IConventionContext context)
    {
        var entityType = (IConventionEntityType)propertyBuilder.Metadata.DeclaringType;
        var property = propertyBuilder.Metadata;

        // Set this property as the row key for the entity type
        entityType.SetRowKeyProperty(property);

        // Ensure row key properties are properly configured
        ConfigureRowKeyProperty(property);
    }

    private static void ConfigureRowKeyProperty(IConventionProperty property)
    {
        // Ensure row key properties are not nullable
        if (property.IsNullable)
        {
            property.SetIsNullable(false);
        }

        // For Azure Table Storage, row keys should be strings
        if (property.ClrType != typeof(string) && property.GetValueConverter() == null)
        {
            // Add value converter for non-string types to convert to strings
            property.SetValueConverter(typeof(string));
        }
    }
}