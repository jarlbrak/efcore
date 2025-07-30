// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Azure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableExecutionStrategy : ExecutionStrategy
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : base(dependencies, maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool ShouldRetryOn(Exception exception)
    {
        // Azure Table-specific transient failure detection logic
        return exception switch
        {
            // General network/timeout issues
            TimeoutException => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            
            // Azure-specific transient failures
            RequestFailedException requestFailedException => IsTransientRequestFailure(requestFailedException),
            
            // Aggregate exceptions may contain transient failures
            AggregateException aggregateException => aggregateException.InnerExceptions.Any(ShouldRetryOn),
            
            _ => false
        };
    }

    private static bool IsTransientRequestFailure(RequestFailedException exception)
    {
        return exception.Status switch
        {
            // Server unavailable - retry
            (int)HttpStatusCode.InternalServerError => true,
            (int)HttpStatusCode.BadGateway => true,
            (int)HttpStatusCode.ServiceUnavailable => true,
            (int)HttpStatusCode.GatewayTimeout => true,
            
            // Request timeout - retry
            (int)HttpStatusCode.RequestTimeout => true,
            
            // Too many requests - retry with backoff
            429 => true, // HTTP 429 Too Many Requests
            
            // Temporary redirect might indicate server issues
            (int)HttpStatusCode.TemporaryRedirect => true,
            
            // Client errors that shouldn't be retried
            (int)HttpStatusCode.BadRequest => false,
            (int)HttpStatusCode.Unauthorized => false,
            (int)HttpStatusCode.Forbidden => false,
            (int)HttpStatusCode.NotFound => false,
            (int)HttpStatusCode.Conflict => false,
            (int)HttpStatusCode.PreconditionFailed => false, // Optimistic concurrency - don't retry
            
            // Default: retry on 5xx server errors, don't retry on 4xx client errors
            >= 500 and < 600 => true,
            >= 400 and < 500 => false,
            
            _ => false
        };
    }
}