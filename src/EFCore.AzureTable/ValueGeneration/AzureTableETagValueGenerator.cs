// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Azure;

namespace Microsoft.EntityFrameworkCore.AzureTable.ValueGeneration;

/// <summary>
///     Generates ETag values for Azure Table Storage entities.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-value-generation">Value generation</see> and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public class AzureTableETagValueGenerator : ValueGenerator<string>
{
    /// <summary>
    ///     Gets a value indicating whether the values generated are temporary (i.e. can be replaced by non-temporary values).
    /// </summary>
    public override bool GeneratesTemporaryValues => true;

    /// <summary>
    ///     Gets a value indicating whether the values generated are stable (i.e. remain the same when the entity is saved multiple times).
    /// </summary>
    public override bool GeneratesStableValues => false;

    /// <summary>
    ///     Gets a value to be assigned to a property.
    /// </summary>
    /// <param name="entry">The change tracking entry of the entity for which the value is being generated.</param>
    /// <returns>The generated value.</returns>
    public override string Next(EntityEntry entry)
    {
        // For new entities, we return a temporary ETag
        // Azure Table Storage will assign the real ETag when the entity is saved
        if (entry.State == EntityState.Added)
        {
            return "*"; // Wildcard ETag for new entities
        }

        // For existing entities being updated, preserve the current ETag
        // This will be used for optimistic concurrency control
        var etagProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsETag());
        if (etagProperty != null)
        {
            var currentValue = etagProperty.CurrentValue;
            return currentValue as string ?? ETag.All.ToString();
        }
        return ETag.All.ToString();
    }

    /// <summary>
    ///     Gets a value to be assigned to a property.
    /// </summary>
    /// <param name="entry">The change tracking entry of the entity for which the value is being generated.</param>
    /// <returns>The generated value.</returns>
    protected override object NextValue(EntityEntry entry)
        => Next(entry);
}