// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Microsoft.EntityFrameworkCore.AzureTable.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class AzureTableEventId
{
    // Query events (200-299)
    public static readonly EventId QueryExecuting = MakeQueryId(Id.QueryExecuting);
    public static readonly EventId QueryExecuted = MakeQueryId(Id.QueryExecuted);
    public static readonly EventId CrossPartitionQuery = MakeQueryId(Id.CrossPartitionQuery);
    public static readonly EventId ClientSideEvaluation = MakeQueryId(Id.ClientSideEvaluation);
    public static readonly EventId UnsupportedOperation = MakeQueryId(Id.UnsupportedOperation);

    // Command events (300-399)
    public static readonly EventId BulkInsertExecuting = MakeCommandId(Id.BulkInsertExecuting);
    public static readonly EventId BulkInsertExecuted = MakeCommandId(Id.BulkInsertExecuted);
    public static readonly EventId BulkDeleteExecuting = MakeCommandId(Id.BulkDeleteExecuting);
    public static readonly EventId BulkDeleteExecuted = MakeCommandId(Id.BulkDeleteExecuted);
    public static readonly EventId BulkUpsertExecuting = MakeCommandId(Id.BulkUpsertExecuting);
    public static readonly EventId BulkUpsertExecuted = MakeCommandId(Id.BulkUpsertExecuted);
    public static readonly EventId BatchOperationExecuting = MakeCommandId(Id.BatchOperationExecuting);
    public static readonly EventId BatchOperationExecuted = MakeCommandId(Id.BatchOperationExecuted);

    // Connection events (400-499)
    public static readonly EventId TableCreating = MakeConnectionId(Id.TableCreating);
    public static readonly EventId TableCreated = MakeConnectionId(Id.TableCreated);
    public static readonly EventId TableDeleting = MakeConnectionId(Id.TableDeleting);
    public static readonly EventId TableDeleted = MakeConnectionId(Id.TableDeleted);
    public static readonly EventId ConnectionOpening = MakeConnectionId(Id.ConnectionOpening);
    public static readonly EventId ConnectionOpened = MakeConnectionId(Id.ConnectionOpened);

    // Infrastructure events (500-599)
    public static readonly EventId OptimisticConcurrencyFailure = MakeInfrastructureId(Id.OptimisticConcurrencyFailure);
    public static readonly EventId RetryExecuting = MakeInfrastructureId(Id.RetryExecuting);
    public static readonly EventId TransientFailureDetected = MakeInfrastructureId(Id.TransientFailureDetected);
    public static readonly EventId ServiceThrottled = MakeInfrastructureId(Id.ServiceThrottled);

    // Performance events (600-699)
    public static readonly EventId PerformanceCounterUpdate = MakePerformanceId(Id.PerformanceCounterUpdate);
    public static readonly EventId SlowQuery = MakePerformanceId(Id.SlowQuery);
    public static readonly EventId LargeResultSet = MakePerformanceId(Id.LargeResultSet);
    public static readonly EventId InefficiateQuery = MakePerformanceId(Id.InefficiateQuery);

    private enum Id
    {
        // Query events
        QueryExecuting = 200,
        QueryExecuted = 201,
        CrossPartitionQuery = 202,
        ClientSideEvaluation = 203,
        UnsupportedOperation = 204,

        // Command events
        BulkInsertExecuting = 300,
        BulkInsertExecuted = 301,
        BulkDeleteExecuting = 302,
        BulkDeleteExecuted = 303,
        BulkUpsertExecuting = 304,
        BulkUpsertExecuted = 305,
        BatchOperationExecuting = 306,
        BatchOperationExecuted = 307,

        // Connection events
        TableCreating = 400,
        TableCreated = 401,
        TableDeleting = 402,
        TableDeleted = 403,
        ConnectionOpening = 404,
        ConnectionOpened = 405,

        // Infrastructure events
        OptimisticConcurrencyFailure = 500,
        RetryExecuting = 501,
        TransientFailureDetected = 502,
        ServiceThrottled = 503,

        // Performance events
        PerformanceCounterUpdate = 600,
        SlowQuery = 601,
        LargeResultSet = 602,
        InefficiateQuery = 603
    }

    private static EventId MakeQueryId(Id id) => new((int)id, DbLoggerCategory.Query.Name + "." + id);
    private static EventId MakeCommandId(Id id) => new((int)id, DbLoggerCategory.Database.Command.Name + "." + id);
    private static EventId MakeConnectionId(Id id) => new((int)id, DbLoggerCategory.Database.Connection.Name + "." + id);
    private static EventId MakeInfrastructureId(Id id) => new((int)id, DbLoggerCategory.Infrastructure.Name + "." + id);
    private static EventId MakePerformanceId(Id id) => new((int)id, "Microsoft.EntityFrameworkCore.AzureTable.Performance." + id);
}