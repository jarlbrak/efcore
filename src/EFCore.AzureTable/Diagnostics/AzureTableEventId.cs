// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.Diagnostics;

/// <summary>
///     Event IDs for Azure Table Storage events that correspond to messages logged to an <see cref="ILogger" />
///     and events sent to a <see cref="DiagnosticSource" />.
/// </summary>
/// <remarks>
///     <para>
///         These IDs are also used with <see cref="WarningsConfigurationBuilder" /> to configure the
///         behavior of warnings.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-diagnostics">Logging, events, and diagnostics</see>, and
///         <see href="https://aka.ms/efcore-docs-azure-table">Accessing Azure Table Storage with EF Core</see> for more information and examples.
///     </para>
/// </remarks>
public static class AzureTableEventId
{
    // Warning: These values must not change between releases.
    // Only add new values to the end of sections, never in the middle.
    // Try to use <Noun><Verb> naming and be consistent with existing names.
    private enum Id
    {
        // Azure Table Storage events
        QueryExecuting = CoreEventId.ProviderBaseId,
        QueryExecuted,
        CrossPartitionQueryExecuted,
        CrossPartitionQuery,
        EntityNotFoundInTable,
        BatchOperationExecuted,
        BulkInsertExecuting,
        BulkInsertExecuted,
        ServiceThrottled,
        SlowQuery,
        TableCreated,
        TableDeleted,

        // Warning events  
        CrossPartitionQueryWarning = CoreEventId.ProviderBaseId + 100,
        LargeEntityWarning,
        UnsupportedQueryWarning,
        RelationshipIgnoredWarning,
        MultipleEntityTypesInTableWarning,
        
        // Database operation events (following Cosmos pattern)
        SyncNotSupported = CoreEventId.ProviderBaseId + 200,
        ExecutedReadItem,
        ExecutedCreateItem,
        ExecutedReplaceItem,
        ExecutedDeleteItem,
        
        // Update events
        PrimaryKeyValueNotSet = CoreEventId.ProviderBaseId + 300,
        
        // Model validation events
        NoPartitionKeyDefined = CoreEventId.ProviderBaseId + 400
    }

    /// <summary>
    ///     A query is being executed against Azure Table Storage.
    /// </summary>
    public static readonly EventId QueryExecuting
        = new((int)Id.QueryExecuting, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     A query was executed against Azure Table Storage.
    /// </summary>
    public static readonly EventId QueryExecuted
        = new((int)Id.QueryExecuted, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     A cross-partition query warning.
    /// </summary>
    public static readonly EventId CrossPartitionQuery
        = new((int)Id.CrossPartitionQuery, DbLoggerCategory.Query.Name);

    /// <summary>
    ///     A bulk insert operation is being executed.
    /// </summary>
    public static readonly EventId BulkInsertExecuting
        = new((int)Id.BulkInsertExecuting, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     A bulk insert operation was executed.
    /// </summary>
    public static readonly EventId BulkInsertExecuted
        = new((int)Id.BulkInsertExecuted, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     Azure Table Storage service was throttled.
    /// </summary>
    public static readonly EventId ServiceThrottled
        = new((int)Id.ServiceThrottled, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     A slow query was detected.
    /// </summary>  
    public static readonly EventId SlowQuery
        = new((int)Id.SlowQuery, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     A cross-partition query was executed against Azure Table Storage.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Query" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the QueryEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId CrossPartitionQueryExecuted
        = new((int)Id.CrossPartitionQueryExecuted, DbLoggerCategory.Query.Name);

    /// <summary>
    ///     An entity was not found in the Azure Table Storage table.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Query" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the EntityEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId EntityNotFoundInTable
        = new((int)Id.EntityNotFoundInTable, DbLoggerCategory.Query.Name);

    /// <summary>
    ///     A batch operation was executed against Azure Table Storage.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Update" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the BatchEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId BatchOperationExecuted
        = new((int)Id.BatchOperationExecuted, DbLoggerCategory.Update.Name);

    /// <summary>
    ///     An Azure Table Storage table was created.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Database.Command" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the TableEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId TableCreated
        = new((int)Id.TableCreated, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     An Azure Table Storage table was deleted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Database.Command" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the TableEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId TableDeleted
        = new((int)Id.TableDeleted, DbLoggerCategory.Database.Command.Name);

    /// <summary>
    ///     A cross-partition query may have poor performance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Query" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the QueryEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId CrossPartitionQueryWarning
        = new((int)Id.CrossPartitionQueryWarning, DbLoggerCategory.Query.Name);

    /// <summary>
    ///     An entity is approaching the Azure Table Storage size limit.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Update" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the EntityEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId LargeEntityWarning
        = new((int)Id.LargeEntityWarning, DbLoggerCategory.Update.Name);

    /// <summary>
    ///     A LINQ query operation is not supported by Azure Table Storage.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Query" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the QueryEventData payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId UnsupportedQueryWarning
        = new((int)Id.UnsupportedQueryWarning, DbLoggerCategory.Query.Name);

    /// <summary>
    ///     A relationship was ignored because Azure Table Storage does not support relationships.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Model.Validation" /> category.
    ///     </para>
    ///     <para>
    ///         This event uses the <see cref="NavigationEventData" /> payload when used with a <see cref="DiagnosticSource" />.
    ///     </para>
    /// </remarks>
    public static readonly EventId RelationshipIgnoredWarning
        = new((int)Id.RelationshipIgnoredWarning, DbLoggerCategory.Model.Validation.Name);

    /// <summary>
    ///     Multiple entity types are mapped to the same table but no discriminator property is configured.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is in the <see cref="DbLoggerCategory.Model.Validation" /> category.
    ///     </para>
    /// </remarks>
    public static readonly EventId MultipleEntityTypesInTableWarning
        = new((int)Id.MultipleEntityTypesInTableWarning, DbLoggerCategory.Model.Validation.Name);
}