// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.AzureTable.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class AzureTableLoggerExtensions
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void QueryExecuting(
        this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics,
        string tableName,
        string? filter,
        int? maxResults,
        TimeSpan executionTime)
    {
        var definition = AzureTableResources.LogQueryExecuting(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, filter ?? "none", maxResults?.ToString() ?? "unlimited");
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableQueryEventData(
                definition,
                AzureTableEventId.QueryExecuting,
                tableName,
                filter,
                maxResults,
                executionTime);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void QueryExecuted(
        this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics,
        string tableName,
        string? filter,
        int resultCount,
        TimeSpan executionTime)
    {
        var definition = AzureTableResources.LogQueryExecuted(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, filter ?? "none", resultCount, executionTime.TotalMilliseconds);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableQueryEventData(
                definition,
                AzureTableEventId.QueryExecuted,
                tableName,
                filter,
                resultCount,
                executionTime);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void CrossPartitionQuery(
        this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics,
        string tableName,
        string queryDescription)
    {
        var definition = AzureTableResources.LogCrossPartitionQuery(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, queryDescription);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableQueryEventData(
                definition,
                AzureTableEventId.CrossPartitionQuery,
                tableName,
                queryDescription,
                null,
                TimeSpan.Zero);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void BulkOperationExecuting(
        this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
        string operation,
        string tableName,
        int entityCount,
        int batchCount)
    {
        var definition = AzureTableResources.LogBulkOperationExecuting(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, operation, tableName, entityCount, batchCount);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableBulkOperationEventData(
                definition,
                AzureTableEventId.BulkInsertExecuting,
                operation,
                tableName,
                entityCount,
                batchCount,
                TimeSpan.Zero);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void BulkOperationExecuted(
        this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics,
        string operation,
        string tableName,
        int processedCount,
        TimeSpan executionTime)
    {
        var definition = AzureTableResources.LogBulkOperationExecuted(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, operation, tableName, processedCount, executionTime.TotalMilliseconds);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableBulkOperationEventData(
                definition,
                AzureTableEventId.BulkInsertExecuted,
                operation,
                tableName,
                processedCount,
                0,
                executionTime);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void ServiceThrottled(
        this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics,
        string tableName,
        int retryCount,
        TimeSpan delay)
    {
        var definition = AzureTableResources.LogServiceThrottled(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, retryCount, delay.TotalSeconds);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableInfrastructureEventData(
                definition,
                AzureTableEventId.ServiceThrottled,
                tableName,
                retryCount,
                delay);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void SlowQuery(
        this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics,
        string tableName,
        string? filter,
        TimeSpan executionTime,
        TimeSpan threshold)
    {
        var definition = AzureTableResources.LogSlowQuery(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, filter ?? "none", executionTime.TotalMilliseconds, threshold.TotalMilliseconds);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new AzureTableQueryEventData(
                definition,
                AzureTableEventId.SlowQuery,
                tableName,
                filter,
                null,
                executionTime);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

}

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableQueryEventData : EventData
{
    public AzureTableQueryEventData(
        EventDefinitionBase eventDefinition,
        EventId eventId,
        string tableName,
        string? filter,
        int? resultCount,
        TimeSpan executionTime)
        : base(eventDefinition, (d, p) => ((EventDefinition<string, string, string>)d).GenerateMessage(tableName, filter ?? "none", resultCount?.ToString() ?? "0"))
    {
        EventId = eventId;
        TableName = tableName;
        Filter = filter;
        ResultCount = resultCount;
        ExecutionTime = executionTime;
    }

    public new EventId EventId { get; }
    public string TableName { get; }
    public string? Filter { get; }
    public int? ResultCount { get; }
    public TimeSpan ExecutionTime { get; }
}

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableBulkOperationEventData : EventData
{
    public AzureTableBulkOperationEventData(
        EventDefinitionBase eventDefinition,
        EventId eventId,
        string operation,
        string tableName,
        int entityCount,
        int batchCount,
        TimeSpan executionTime)
        : base(eventDefinition, (d, p) => ((EventDefinition<string, string, int, int>)d).GenerateMessage(operation, tableName, entityCount, batchCount))
    {
        EventId = eventId;
        Operation = operation;
        TableName = tableName;
        EntityCount = entityCount;
        BatchCount = batchCount;
        ExecutionTime = executionTime;
    }

    public new EventId EventId { get; }
    public string Operation { get; }
    public string TableName { get; }
    public int EntityCount { get; }
    public int BatchCount { get; }
    public TimeSpan ExecutionTime { get; }
}

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableInfrastructureEventData : EventData
{
    public AzureTableInfrastructureEventData(
        EventDefinitionBase eventDefinition,
        EventId eventId,
        string tableName,
        int retryCount,
        TimeSpan delay)
        : base(eventDefinition, (d, p) => ((EventDefinition<string, int, double>)d).GenerateMessage(tableName, retryCount, delay.TotalSeconds))
    {
        EventId = eventId;
        TableName = tableName;
        RetryCount = retryCount;
        Delay = delay;
    }

    public new EventId EventId { get; }
    public string TableName { get; }
    public int RetryCount { get; }
    public TimeSpan Delay { get; }
}