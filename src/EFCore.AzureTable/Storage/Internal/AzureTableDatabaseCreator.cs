// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableDatabaseCreator : IDatabaseCreator
{
    private readonly IAzureTableClientWrapper _clientWrapper;
    private readonly IDesignTimeModel _designTimeModel;
    private readonly IUpdateAdapterFactory _updateAdapterFactory;
    private readonly IDatabase _database;
    private readonly ICurrentDbContext _currentDbContext;
    private readonly IDbContextOptions _contextOptions;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableDatabaseCreator(
        IAzureTableClientWrapper clientWrapper,
        IDesignTimeModel designTimeModel,
        IUpdateAdapterFactory updateAdapterFactory,
        IDatabase database,
        ICurrentDbContext currentDbContext,
        IDbContextOptions contextOptions)
    {
        _clientWrapper = clientWrapper;
        _designTimeModel = designTimeModel;
        _updateAdapterFactory = updateAdapterFactory;
        _database = database;
        _currentDbContext = currentDbContext;
        _contextOptions = contextOptions;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool EnsureCreated()
        => EnsureCreatedAsync().GetAwaiter().GetResult();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public async Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var model = _currentDbContext.Context.Model;
        var created = false;

        // Create tables for all entity types
        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetAzureTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                System.Diagnostics.Debug.WriteLine($"Creating table: '{tableName}'");
                var result = await _clientWrapper.CreateTableIfNotExistsAsync(tableName, cancellationToken).ConfigureAwait(false);
                created = created || result;
            }
        }

        return created;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool EnsureDeleted()
    {
        // Azure Table Storage doesn't have a "database" concept
        // Individual tables would need to be deleted
        return true;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default)
    {
        // Azure Table Storage doesn't have a "database" concept
        // Individual tables would need to be deleted
        return Task.FromResult(true);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool CanConnect()
    {
        // TODO: Implement connection test
        return true;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement connection test
        return Task.FromResult(true);
    }
}