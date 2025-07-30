// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure;

/// <summary>
///     Allows Azure Table Storage specific configuration to be performed on <see cref="DbContextOptions" />.
/// </summary>
/// <remarks>
///     <para>
///         Instances of this class are returned from a call to <see cref="AzureTableDbContextOptionsExtensions.UseAzureTable(DbContextOptionsBuilder, string, Action{AzureTableDbContextOptionsBuilder})" />
///         and it is not designed to be directly constructed in your application code.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
///         <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
///     </para>
/// </remarks>
public class AzureTableDbContextOptionsBuilder : IAzureTableDbContextOptionsBuilderInfrastructure
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureTableDbContextOptionsBuilder" /> class.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    public AzureTableDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        OptionsBuilder = optionsBuilder;
    }

    /// <summary>
    ///     Clones the configuration in this builder.
    /// </summary>
    /// <returns>The cloned configuration.</returns>
    protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

    /// <inheritdoc />
    DbContextOptionsBuilder IAzureTableDbContextOptionsBuilderInfrastructure.OptionsBuilder
        => OptionsBuilder;

    /// <summary>
    ///     Configures the table name prefix to use for all tables. This can be useful for multi-tenant scenarios
    ///     or to avoid naming conflicts.
    /// </summary>
    /// <param name="tableNamePrefix">The prefix to apply to all table names.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public virtual AzureTableDbContextOptionsBuilder UseTableNamePrefix(string tableNamePrefix)
    {
        var extension = GetOrCreateExtension().WithTableNamePrefix(tableNamePrefix);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);

        return this;
    }

    /// <summary>
    ///     Configures whether to enable batching of operations within the same partition.
    /// </summary>
    /// <param name="enableBatching">A value indicating whether batching should be enabled.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public virtual AzureTableDbContextOptionsBuilder EnableBatching(bool enableBatching = true)
    {
        var extension = GetOrCreateExtension().WithBatchingEnabled(enableBatching);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);

        return this;
    }

    /// <summary>
    ///     Configures the request timeout for Azure Table Storage operations.
    /// </summary>
    /// <param name="requestTimeout">The timeout value for requests.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public virtual AzureTableDbContextOptionsBuilder UseRequestTimeout(TimeSpan requestTimeout)
    {
        var extension = GetOrCreateExtension().WithRequestTimeout(requestTimeout);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);

        return this;
    }

    /// <summary>
    ///     Configures the maximum number of retry attempts for transient failures.
    /// </summary>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public virtual AzureTableDbContextOptionsBuilder UseMaxRetryAttempts(int maxRetryAttempts)
    {
        var extension = GetOrCreateExtension().WithMaxRetryAttempts(maxRetryAttempts);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);

        return this;
    }

    /// <summary>
    ///     Configures the <see cref="IExecutionStrategy" /> to be used for Azure Table Storage operations.
    /// </summary>
    /// <param name="getExecutionStrategy">A factory for creating the execution strategy to use.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public virtual AzureTableDbContextOptionsBuilder ExecutionStrategy(
        Func<ExecutionStrategyDependencies, IExecutionStrategy> getExecutionStrategy)
    {
        Check.NotNull(getExecutionStrategy, nameof(getExecutionStrategy));

        var extension = GetOrCreateExtension().WithExecutionStrategyFactory(getExecutionStrategy);
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);

        return this;
    }

    private AzureTableOptionsExtension GetOrCreateExtension()
        => OptionsBuilder.Options.FindExtension<AzureTableOptionsExtension>()
        ?? new AzureTableOptionsExtension();
}