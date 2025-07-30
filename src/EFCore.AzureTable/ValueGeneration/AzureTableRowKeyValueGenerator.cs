// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.ValueGeneration;

/// <summary>
///     Generates row key values for Azure Table Storage entities.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-value-generation">Value generation</see> and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public class AzureTableRowKeyValueGenerator : ValueGenerator<string>
{
    /// <summary>
    ///     Gets a value indicating whether the values generated are temporary (i.e. can be replaced by non-temporary values).
    /// </summary>
    public override bool GeneratesTemporaryValues => false;

    /// <summary>
    ///     Gets a value indicating whether the values generated are stable (i.e. remain the same when the entity is saved multiple times).
    /// </summary>
    public override bool GeneratesStableValues => true;

    /// <summary>
    ///     Gets a value to be assigned to a property.
    /// </summary>
    /// <param name="entry">The change tracking entry of the entity for which the value is being generated.</param>
    /// <returns>The generated value.</returns>
    public override string Next(EntityEntry entry)
    {
        // Generate a unique row key using reverse ticks for time-based sorting
        // This ensures newer entities appear first when querying
        var reverseTicks = DateTimeOffset.MaxValue.Ticks - DateTimeOffset.UtcNow.Ticks;
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{reverseTicks:D19}_{uniqueId}";
    }

    /// <summary>
    ///     Gets a value to be assigned to a property.
    /// </summary>
    /// <param name="entry">The change tracking entry of the entity for which the value is being generated.</param>
    /// <returns>The generated value.</returns>
    protected override object NextValue(EntityEntry entry)
        => Next(entry);
}