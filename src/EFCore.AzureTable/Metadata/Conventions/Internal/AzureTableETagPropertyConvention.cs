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
public class AzureTableETagPropertyConvention : IModelFinalizingConvention
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableETagPropertyConvention()
    {
    }

    /// <inheritdoc />
    public virtual void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var etagProperty = entityType.GetETagProperty();
            if (etagProperty is IConventionProperty conventionEtagProperty && !conventionEtagProperty.IsConcurrencyToken)
            {
                // Ensure ETag property is marked as concurrency token
                conventionEtagProperty.SetIsConcurrencyToken(true);
            }

            // Also ensure Timestamp property handling if needed
            var timestampProperty = entityType.GetTimestampProperty();
            if (timestampProperty is IConventionProperty conventionTimestampProperty)
            {
                // Timestamp is read-only from Azure Table Storage
                conventionTimestampProperty.SetValueGenerated(ValueGenerated.OnAddOrUpdate);
                // Note: Timestamp properties are always store-generated in Azure Table Storage
            }
        }
    }
}