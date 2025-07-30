// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Query.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.ValueGeneration;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Azure Table-specific extension methods for <see cref="IServiceCollection" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
///     <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
/// </remarks>
public static class AzureTableServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the given Entity Framework <see cref="DbContext" /> as a service in the <see cref="IServiceCollection" />
    ///     and configures it to connect to Azure Table Storage.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is a shortcut for configuring a <see cref="DbContext" /> to use Azure Table Storage. It does not support all options.
    ///         Use <see cref="O:EntityFrameworkServiceCollectionExtensions.AddDbContext" /> and related methods for full control of
    ///         this process.
    ///     </para>
    ///     <para>
    ///         Use this method when using dependency injection in your application, such as with ASP.NET Core.
    ///         For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///         instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///         overridden to configure the Azure Table Storage provider.
    ///     </para>
    ///     <para>
    ///         To configure the <see cref="DbContextOptions{TContext}" /> for the context, either override the
    ///         <see cref="DbContext.OnConfiguring" /> method in your derived context, or supply
    ///         an optional action to configure the <see cref="DbContextOptions" /> for the context.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///         <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be registered.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">The connection string of the Azure Table Storage account to connect to.</param>
    /// <param name="azureTableOptionsAction">An optional action to allow additional Azure Table-specific configuration.</param>
    /// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptions" /> for the context.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddAzureTable<TContext>(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<AzureTableDbContextOptionsBuilder>? azureTableOptionsAction = null,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
        => serviceCollection.AddDbContext<TContext>(
            (serviceProvider, options) =>
            {
                optionsAction?.Invoke(options);
                options.UseAzureTable(connectionString, azureTableOptionsAction);
            });

    /// <summary>
    ///     <para>
    ///         Adds the services required by the Azure Table Storage provider for Entity Framework
    ///         to an <see cref="IServiceCollection" />.
    ///     </para>
    ///     <para>
    ///         Warning: Do not call this method accidentally. It is much more likely you need
    ///         to call <see cref="AddAzureTable{TContext}" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Calling this method is no longer necessary when building most applications, including those that
    ///     use dependency injection in ASP.NET or elsewhere.
    ///     It is only needed when building the internal service provider for use with
    ///     the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
    ///     This is not recommend other than for some advanced scenarios.
    /// </remarks>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkAzureTable(this IServiceCollection serviceCollection)
    {
        var builder = new EntityFrameworkServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, AzureTableLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<AzureTableOptionsExtension>>()
            .TryAdd<IDatabase, AzureTableDatabaseWrapper>()
            .TryAdd<IExecutionStrategyFactory, AzureTableExecutionStrategyFactory>()
            .TryAdd<IDbContextTransactionManager, AzureTableTransactionManager>()
            .TryAdd<IModelValidator, AzureTableModelValidator>()
            .TryAdd<IModelRuntimeInitializer, AzureTableModelRuntimeInitializer>()
            .TryAdd<IProviderConventionSetBuilder, AzureTableConventionSetBuilder>()
            .TryAdd<IDatabaseCreator, AzureTableDatabaseCreator>()
            .TryAdd<IQueryContextFactory, AzureTableQueryContextFactory>()
            .TryAdd<ITypeMappingSource, AzureTableTypeMappingSource>()
            .TryAdd<IValueGeneratorSelector, AzureTableValueGeneratorSelector>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, AzureTableQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IShapedQueryCompilingExpressionVisitorFactory, AzureTableShapedQueryCompilingExpressionVisitorFactory>()
            .TryAdd<IQueryTranslationPreprocessorFactory, AzureTableQueryTranslationPreprocessorFactory>()
            .TryAdd<IQueryCompilationContextFactory, AzureTableQueryCompilationContextFactory>()
            .TryAdd<IQueryTranslationPostprocessorFactory, AzureTableQueryTranslationPostprocessorFactory>()
            .TryAdd<ISingletonOptions, IAzureTableSingletonOptions>(p => p.GetRequiredService<IAzureTableSingletonOptions>())
            .TryAddProviderSpecificServices(
                b => b
                    .TryAddSingleton<IAzureTableSingletonOptions, AzureTableSingletonOptions>()
                    .TryAddSingleton<IAzureTableSingletonClientWrapper, AzureTableSingletonClientWrapper>()
                    .TryAddSingleton<AzureTableModelRuntimeInitializerDependencies, AzureTableModelRuntimeInitializerDependencies>()
                    .TryAddScoped<IAzureTableClientWrapper, AzureTableClientWrapper>()
                    .TryAddScoped<IAzureTableTableClientFactory, AzureTableTableClientFactory>());

        builder.TryAddCoreServices();

        return serviceCollection;
    }
}