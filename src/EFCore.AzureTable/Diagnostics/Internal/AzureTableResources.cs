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
public static class AzureTableResources
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, string, string> LogQueryExecuting(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.QueryExecuting,
            LogLevel.Debug,
            "AzureTableQueryExecuting",
            level => LoggerMessage.Define<string, string, string>(
                level,
                AzureTableEventId.QueryExecuting,
                "Executing query against table '{TableName}' with filter '{Filter}' and max results '{MaxResults}'"));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, string, int, double> LogQueryExecuted(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.QueryExecuted,
            LogLevel.Information,
            "AzureTableQueryExecuted",
            level => LoggerMessage.Define<string, string, int, double>(
                level,
                AzureTableEventId.QueryExecuted,
                "Executed query against table '{TableName}' with filter '{Filter}'. Returned {ResultCount} results in {ElapsedMs}ms"));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, string> LogCrossPartitionQuery(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.CrossPartitionQuery,
            LogLevel.Warning,
            "AzureTableCrossPartitionQuery",
            level => LoggerMessage.Define<string, string>(
                level,
                AzureTableEventId.CrossPartitionQuery,
                "Query against table '{TableName}' will scan multiple partitions: '{QueryDescription}'. This may impact performance."));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, string, int, int> LogBulkOperationExecuting(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.BulkInsertExecuting,
            LogLevel.Information,
            "AzureTableBulkOperationExecuting",
            level => LoggerMessage.Define<string, string, int, int>(
                level,
                AzureTableEventId.BulkInsertExecuting,
                "Executing bulk {Operation} against table '{TableName}' with {EntityCount} entities in {BatchCount} batches"));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, string, int, double> LogBulkOperationExecuted(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.BulkInsertExecuted,
            LogLevel.Information,
            "AzureTableBulkOperationExecuted",
            level => LoggerMessage.Define<string, string, int, double>(
                level,
                AzureTableEventId.BulkInsertExecuted,
                "Executed bulk {Operation} against table '{TableName}'. Processed {ProcessedCount} entities in {ElapsedMs}ms"));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, int, double> LogServiceThrottled(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.ServiceThrottled,
            LogLevel.Warning,
            "AzureTableServiceThrottled",
            level => LoggerMessage.Define<string, int, double>(
                level,
                AzureTableEventId.ServiceThrottled,
                "Azure Table Storage service throttled operations on table '{TableName}'. Retry #{RetryCount} will occur after {DelaySeconds} seconds"));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static EventDefinition<string, string, double, double> LogSlowQuery(IDiagnosticsLogger logger)
        => new(
            logger.Options,
            AzureTableEventId.SlowQuery,
            LogLevel.Warning,
            "AzureTableSlowQuery",
            level => LoggerMessage.Define<string, string, double, double>(
                level,
                AzureTableEventId.SlowQuery,
                "Slow query detected on table '{TableName}' with filter '{Filter}'. Execution time: {ElapsedMs}ms (threshold: {ThresholdMs}ms)"));
}