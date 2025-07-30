// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.Tables;
using Azure;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableClientWrapper : IAzureTableClientWrapper, IDisposable
{
    private readonly IAzureTableSingletonClientWrapper _singletonClient;
    private readonly string? _tableNamePrefix;
    private readonly Dictionary<string, TableClient> _tableClients = new();
    private readonly object _lock = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableClientWrapper(
        IAzureTableSingletonClientWrapper singletonClient,
        IDbContextOptions dbContextOptions)
    {
        Check.NotNull(singletonClient, nameof(singletonClient));
        Check.NotNull(dbContextOptions, nameof(dbContextOptions));

        _singletonClient = singletonClient;

        var options = dbContextOptions.FindExtension<AzureTableOptionsExtension>();
        Check.NotNull(options, nameof(options));

        var tableNamePrefix = options.TableNamePrefix;
        if (!string.IsNullOrEmpty(tableNamePrefix))
        {
            _tableNamePrefix = tableNamePrefix.EndsWith("_", StringComparison.Ordinal) ? tableNamePrefix : tableNamePrefix + "_";
        }
    }

    /// <inheritdoc />
    public TableClient GetTableClient(string tableName)
    {
        Check.NotEmpty(tableName, nameof(tableName));

        var actualTableName = GetActualTableName(tableName);

        lock (_lock)
        {
            if (!_tableClients.TryGetValue(actualTableName, out var tableClient))
            {
                tableClient = _singletonClient.Client.GetTableClient(actualTableName);
                _tableClients[actualTableName] = tableClient;
            }

            return tableClient;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CreateTableIfNotExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        Check.NotEmpty(tableName, nameof(tableName));

        var actualTableName = GetActualTableName(tableName);
        var tableClient = GetTableClient(actualTableName);
        
        var response = await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        return response != null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTableIfExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        Check.NotEmpty(tableName, nameof(tableName));

        var actualTableName = GetActualTableName(tableName);
        
        lock (_lock)
        {
            _tableClients.Remove(actualTableName);
        }

        var response = await _singletonClient.Client.DeleteTableAsync(actualTableName, cancellationToken).ConfigureAwait(false);
        return response.Status != 404; // Not found means it didn't exist
    }

    private string GetActualTableName(string tableName)
        => string.IsNullOrEmpty(_tableNamePrefix) ? tableName : _tableNamePrefix + tableName;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _tableClients.Clear();
        }
    }
}