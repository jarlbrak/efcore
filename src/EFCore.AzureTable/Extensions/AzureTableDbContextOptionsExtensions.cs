// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure;
using Microsoft.EntityFrameworkCore.AzureTable.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Azure Table-specific extension methods for <see cref="DbContextOptionsBuilder" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableDbContextOptionsExtensions
{
    /// <summary>
    ///     Configures the context to connect to Azure Table Storage. The connection details need to be specified in a separate call.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="azureTableOptionsAction">An action to allow Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseAzureTable<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Action<AzureTableDbContextOptionsBuilder> azureTableOptionsAction)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseAzureTable(
            (DbContextOptionsBuilder)optionsBuilder,
            azureTableOptionsAction);

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage. The connection details need to be specified in a separate call.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="azureTableOptionsAction">An action to allow Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseAzureTable(
        this DbContextOptionsBuilder optionsBuilder,
        Action<AzureTableDbContextOptionsBuilder> azureTableOptionsAction)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));
        Check.NotNull(azureTableOptionsAction, nameof(azureTableOptionsAction));

        ConfigureWarnings(optionsBuilder);

        azureTableOptionsAction.Invoke(new AzureTableDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the Azure Table Storage account to connect to.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseAzureTable<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseAzureTable(
            (DbContextOptionsBuilder)optionsBuilder,
            connectionString,
            azureTableOptionsAction);

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the Azure Table Storage account to connect to.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseAzureTable(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));
        Check.NotNull(connectionString, nameof(connectionString));

        var extension = optionsBuilder.Options.FindExtension<AzureTableOptionsExtension>()
            ?? new AzureTableOptionsExtension();

        extension = extension.WithConnectionString(connectionString);

        ConfigureWarnings(optionsBuilder);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        azureTableOptionsAction?.Invoke(new AzureTableDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage using an account name and key.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="accountName">The Azure Storage account name.</param>
    /// <param name="accountKey">The Azure Storage account key.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseAzureTable<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string accountName,
        string accountKey,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseAzureTable(
            (DbContextOptionsBuilder)optionsBuilder,
            accountName,
            accountKey,
            azureTableOptionsAction);

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage using an account name and key.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="accountName">The Azure Storage account name.</param>
    /// <param name="accountKey">The Azure Storage account key.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseAzureTable(
        this DbContextOptionsBuilder optionsBuilder,
        string accountName,
        string accountKey,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));
        Check.NotNull(accountName, nameof(accountName));
        Check.NotNull(accountKey, nameof(accountKey));

        var extension = optionsBuilder.Options.FindExtension<AzureTableOptionsExtension>()
            ?? new AzureTableOptionsExtension();

        extension = extension.WithAccountNameAndKey(accountName, accountKey);

        ConfigureWarnings(optionsBuilder);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        azureTableOptionsAction?.Invoke(new AzureTableDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage using a service URI and SAS token.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="serviceUri">The URI of the Azure Table Service endpoint.</param>
    /// <param name="sasToken">The SAS token for authentication.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseAzureTable<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Uri serviceUri,
        string sasToken,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseAzureTable(
            (DbContextOptionsBuilder)optionsBuilder,
            serviceUri,
            sasToken,
            azureTableOptionsAction);

    /// <summary>
    ///     Configures the context to connect to Azure Table Storage using a service URI and SAS token.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="serviceUri">The URI of the Azure Table Service endpoint.</param>
    /// <param name="sasToken">The SAS token for authentication.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseAzureTable(
        this DbContextOptionsBuilder optionsBuilder,
        Uri serviceUri,
        string sasToken,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));
        Check.NotNull(serviceUri, nameof(serviceUri));
        Check.NotNull(sasToken, nameof(sasToken));

        var extension = optionsBuilder.Options.FindExtension<AzureTableOptionsExtension>()
            ?? new AzureTableOptionsExtension();

        extension = extension.WithSasToken(serviceUri, sasToken);

        ConfigureWarnings(optionsBuilder);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        azureTableOptionsAction?.Invoke(new AzureTableDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        var coreOptionsExtension
            = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        coreOptionsExtension = coreOptionsExtension.WithWarningsConfiguration(
            coreOptionsExtension.WarningsConfiguration.TryWithExplicit(
                AzureTableEventId.CrossPartitionQueryWarning, WarningBehavior.Log));

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
    }
}