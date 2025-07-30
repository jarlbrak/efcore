// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.ValueGeneration;

/// <summary>
///     Generates timestamp values for Azure Table Storage entities.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-value-generation">Value generation</see> and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public class AzureTableTimestampValueGenerator : ValueGenerator<DateTimeOffset>
{
    /// <summary>
    ///     Gets a value indicating whether the values generated are temporary (i.e. can be replaced by non-temporary values).
    /// </summary>
    public override bool GeneratesTemporaryValues => false;

    /// <summary>
    ///     Gets a value indicating whether the values generated are stable (i.e. remain the same when the entity is saved multiple times).
    /// </summary>
    public override bool GeneratesStableValues => false;

    /// <summary>
    ///     Gets a value to be assigned to a property.
    /// </summary>
    /// <param name="entry">The change tracking entry of the entity for which the value is being generated.</param>
    /// <returns>The generated value.</returns>
    public override DateTimeOffset Next(EntityEntry entry)
    {
        // Azure Table Storage automatically manages the Timestamp property
        // This is a placeholder that returns current UTC time
        // The actual timestamp will be set by Azure Table Storage
        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Gets a value to be assigned to a property.
    /// </summary>
    /// <param name="entry">The change tracking entry of the entity for which the value is being generated.</param>
    /// <returns>The generated value.</returns>
    protected override object NextValue(EntityEntry entry)
        => Next(entry);
}