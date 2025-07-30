// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Azure Table-specific extension methods for <see cref="ModelBuilder" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableModelBuilderExtensions
{
    /// <summary>
    ///     Configures the model to use Azure Table Storage conventions.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseAzureTableConventions(this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        // Apply any global Azure Table Storage conventions here
        // For example, we could configure default naming conventions, 
        // default partition key/row key strategies, etc.
        
        return modelBuilder;
    }

    /// <summary>
    ///     Configures the default table name schema for entities.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    /// <param name="tableNamePrefix">The prefix to apply to all table names.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder HasDefaultTableNamePrefix(this ModelBuilder modelBuilder, string? tableNamePrefix)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Model.SetDefaultTableNamePrefix(tableNamePrefix);
        
        return modelBuilder;
    }

    /// <summary>
    ///     Configures the model to ignore navigation properties by default since Azure Table Storage
    ///     doesn't support relationships.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder IgnoreNavigationPropertiesByDefault(this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        // This would be implemented by iterating through entity types and their navigation properties
        // For now, this is a placeholder for the functionality
        
        return modelBuilder;
    }

    /// <summary>
    ///     Configures whether to use batch operations for better performance when saving changes.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    /// <param name="enableBatching">True to enable batching; false otherwise.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseAzureTableBatching(this ModelBuilder modelBuilder, bool enableBatching = true)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Model.SetAzureTableBatchingEnabled(enableBatching);
        
        return modelBuilder;
    }
}