// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Azure;
using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore.AzureTable.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Update.Internal;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableDatabaseWrapper : Database
{
    private readonly IAzureTableClientWrapper _clientWrapper;
    private readonly ITypeMappingSource _typeMappingSource;
    private readonly bool _sensitiveLoggingEnabled;
    private readonly Dictionary<IEntityType, TableEntitySource> _tableEntitySources = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableDatabaseWrapper(
        DatabaseDependencies dependencies,
        IAzureTableClientWrapper clientWrapper,
        ITypeMappingSource typeMappingSource,
        ILoggingOptions loggingOptions)
        : base(dependencies)
    {
        _clientWrapper = clientWrapper;
        _typeMappingSource = typeMappingSource;
        _sensitiveLoggingEnabled = loggingOptions.IsSensitiveDataLoggingEnabled;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TableEntitySource GetTableEntitySource(IEntityType entityType)
    {
        if (!_tableEntitySources.TryGetValue(entityType, out var tableEntitySource))
        {
            tableEntitySource = new TableEntitySource(entityType, this, _typeMappingSource);
            _tableEntitySources[entityType] = tableEntitySource;
        }

        return tableEntitySource;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override int SaveChanges(IList<IUpdateEntry> entries)
    {
        var rowsAffected = 0;

        // Group entries by table and partition key for batch operations
        var groupedEntries = GroupEntriesByTableAndPartition(entries);

        foreach (var tableGroup in groupedEntries)
        {
            var tableName = tableGroup.Key;
            var tableClient = _clientWrapper.GetTableClient(tableName);

            foreach (var partitionGroup in tableGroup.Value)
            {
                // Azure Table Storage supports batch operations within a single partition
                var batch = new List<TableTransactionAction>();
                var batchEntries = new List<IUpdateEntry>();

                foreach (var entry in partitionGroup.Value)
                {
                    try
                    {
                        var action = CreateTableTransactionAction(entry);
                        if (action != null)
                        {
                            batch.Add(action);
                            batchEntries.Add(entry);
                        }
                    }
                    catch (Exception ex) when (ex is not DbUpdateException and not OperationCanceledException)
                    {
                        throw WrapUpdateException(ex, new[] { entry });
                    }
                }

                if (batch.Any())
                {
                    try
                    {
                        // Execute batch transaction
                        var response = tableClient.SubmitTransaction(batch);
                        rowsAffected += batch.Count;

                        // Update entries with response data (ETag, Timestamp)
                        for (var i = 0; i < response.Value.Count; i++)
                        {
                            UpdateEntryAfterSave(batchEntries[i], response.Value[i]);
                        }
                    }
                    catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
                    {
                        // Handle optimistic concurrency failures
                        var concurrencyException = new DbUpdateConcurrencyException(
                            "Store update, insert, or delete statement affected an unexpected number of rows.",
                            batchEntries);

                        if (!Dependencies.Logger.OptimisticConcurrencyException(
                            batchEntries[0].Context, batchEntries, concurrencyException, null).IsSuppressed)
                        {
                            throw concurrencyException;
                        }
                    }
                    catch (Exception ex) when (ex is not DbUpdateException and not OperationCanceledException)
                    {
                        throw WrapUpdateException(ex, batchEntries);
                    }
                }
            }
        }

        return rowsAffected;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override async Task<int> SaveChangesAsync(
        IList<IUpdateEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var rowsAffected = 0;

        // Group entries by table and partition key for batch operations
        var groupedEntries = GroupEntriesByTableAndPartition(entries);

        foreach (var tableGroup in groupedEntries)
        {
            var tableName = tableGroup.Key;
            var tableClient = _clientWrapper.GetTableClient(tableName);

            foreach (var partitionGroup in tableGroup.Value)
            {
                // Azure Table Storage supports batch operations within a single partition
                var batch = new List<TableTransactionAction>();
                var batchEntries = new List<IUpdateEntry>();

                foreach (var entry in partitionGroup.Value)
                {
                    try
                    {
                        var action = CreateTableTransactionAction(entry);
                        if (action != null)
                        {
                            batch.Add(action);
                            batchEntries.Add(entry);
                        }
                    }
                    catch (Exception ex) when (ex is not DbUpdateException and not OperationCanceledException)
                    {
                        throw WrapUpdateException(ex, new[] { entry });
                    }
                }

                if (batch.Any())
                {
                    try
                    {
                        // Execute batch transaction
                        var response = await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
                        rowsAffected += batch.Count;

                        // Update entries with response data (ETag, Timestamp)
                        for (var i = 0; i < response.Value.Count; i++)
                        {
                            UpdateEntryAfterSave(batchEntries[i], response.Value[i]);
                        }
                    }
                    catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
                    {
                        // Handle optimistic concurrency failures
                        var concurrencyException = new DbUpdateConcurrencyException(
                            "Store update, insert, or delete statement affected an unexpected number of rows.",
                            batchEntries);

                        if (!(await Dependencies.Logger.OptimisticConcurrencyExceptionAsync(
                                batchEntries[0].Context, batchEntries, concurrencyException, null, cancellationToken)
                            .ConfigureAwait(false)).IsSuppressed)
                        {
                            throw concurrencyException;
                        }
                    }
                    catch (Exception ex) when (ex is not DbUpdateException and not OperationCanceledException)
                    {
                        throw WrapUpdateException(ex, batchEntries);
                    }
                }
            }
        }

        return rowsAffected;
    }





    private Dictionary<string, Dictionary<string, List<IUpdateEntry>>> GroupEntriesByTableAndPartition(IList<IUpdateEntry> entries)
    {
        var grouped = new Dictionary<string, Dictionary<string, List<IUpdateEntry>>>();

        foreach (var entry in entries)
        {
            var entityType = entry.EntityType;
            var tableName = entityType.GetAzureTableName() ?? entityType.Name;
            
            // Get partition key value
            var partitionKeyProperty = entityType.GetPartitionKeyProperty();
            if (partitionKeyProperty == null)
            {
                throw new InvalidOperationException($"Entity type '{entityType.DisplayName()}' does not have a partition key defined.");
            }

            var partitionKeyValue = entry.GetCurrentValue((IProperty)partitionKeyProperty)?.ToString() ?? string.Empty;

            if (!grouped.ContainsKey(tableName))
            {
                grouped[tableName] = new Dictionary<string, List<IUpdateEntry>>();
            }

            if (!grouped[tableName].ContainsKey(partitionKeyValue))
            {
                grouped[tableName][partitionKeyValue] = new List<IUpdateEntry>();
            }

            grouped[tableName][partitionKeyValue].Add(entry);
        }

        return grouped;
    }

    private TableTransactionAction? CreateTableTransactionAction(IUpdateEntry entry)
    {
        var tableEntitySource = GetTableEntitySource(entry.EntityType);
        var entity = tableEntitySource.CreateTableEntity(entry);

        return entry.EntityState switch
        {
            EntityState.Added => new TableTransactionAction(TableTransactionActionType.Add, entity),
            EntityState.Modified => new TableTransactionAction(TableTransactionActionType.UpdateReplace, entity),
            EntityState.Deleted => new TableTransactionAction(TableTransactionActionType.Delete, entity),
            _ => null
        };
    }


    private void UpdateEntryAfterSave(IUpdateEntry entry, Response response)
    {
        // Create a temporary TableEntity to hold the response data
        var tableEntity = new TableEntity();
        
        // Extract ETag from response
        if (response.Headers.TryGetValue("ETag", out var etagValue))
        {
            tableEntity.ETag = new ETag(etagValue);
        }

        // Extract Timestamp from response (if available)
        if (response.Headers.TryGetValue("x-ms-date", out var timestampValue))
        {
            if (DateTimeOffset.TryParse(timestampValue, out var timestamp))
            {
                tableEntity.Timestamp = timestamp;
            }
        }

        // Use TableEntitySource to update the entry
        var tableEntitySource = GetTableEntitySource(entry.EntityType);
        tableEntitySource.UpdateEntityFromTableEntity(entry, tableEntity);
    }

    private DbUpdateException WrapUpdateException(Exception exception, IReadOnlyList<IUpdateEntry> entries)
    {
        return new DbUpdateException(
            exception.Message,
            exception,
            entries);
    }
}