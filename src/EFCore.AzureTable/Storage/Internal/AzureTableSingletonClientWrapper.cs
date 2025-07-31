// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Data.Tables;
using Azure;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableSingletonClientWrapper : IAzureTableSingletonClientWrapper
{
    private static readonly string UserAgent = " Microsoft.EntityFrameworkCore.AzureTable/" + ProductInfo.GetVersion();
    private readonly string? _connectionString;
    private readonly string? _accountName;
    private readonly string? _accountKey;
    private readonly Uri? _serviceUri;
    private readonly string? _sharedAccessSignature;
    private readonly TokenCredential? _tokenCredential;
    private readonly bool _enableBatching;
    private readonly TimeSpan? _requestTimeout;
    private readonly int? _maxRetryAttempts;
    private TableServiceClient? _client;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableSingletonClientWrapper(IAzureTableSingletonOptions options)
    {
        _connectionString = options.ConnectionString;
        _accountName = options.AccountName;
        _accountKey = options.AccountKey;
        _serviceUri = options.ServiceUri;
        _sharedAccessSignature = options.SharedAccessSignature;
        _tokenCredential = options.TokenCredential;
        _enableBatching = options.EnableBatching ?? true;
        _requestTimeout = options.RequestTimeout;
        _maxRetryAttempts = options.MaxRetryAttempts;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TableServiceClient Client
        => _client ??= CreateClient();

    private TableServiceClient CreateClient()
    {
        var options = new TableClientOptions
        {
            EnableTenantDiscovery = true
        };

        // Set retry options if specified
        if (_maxRetryAttempts.HasValue)
        {
            options.Retry.MaxRetries = _maxRetryAttempts.Value;
        }

        // Set timeout if specified
        if (_requestTimeout.HasValue)
        {
            options.Retry.NetworkTimeout = _requestTimeout.Value;
        }

        // Create client based on available connection information
        if (!string.IsNullOrEmpty(_connectionString))
        {
            return new TableServiceClient(_connectionString, options);
        }

        if (_serviceUri != null)
        {
            if (_tokenCredential != null)
            {
                return new TableServiceClient(_serviceUri, _tokenCredential, options);
            }

            if (!string.IsNullOrEmpty(_sharedAccessSignature))
            {
                return new TableServiceClient(_serviceUri, new AzureSasCredential(_sharedAccessSignature), options);
            }
        }

        if (!string.IsNullOrEmpty(_accountName) && !string.IsNullOrEmpty(_accountKey))
        {
            var credential = new TableSharedKeyCredential(_accountName, _accountKey);
            var serviceUri = _serviceUri ?? new Uri($"https://{_accountName}.table.core.windows.net/");
            return new TableServiceClient(serviceUri, credential, options);
        }

        // Fallback: try to create with development storage if no other options are available
        if (string.IsNullOrEmpty(_connectionString) && _serviceUri == null && 
            (string.IsNullOrEmpty(_accountName) || string.IsNullOrEmpty(_accountKey)))
        {
            // Use development storage connection string as fallback
            return new TableServiceClient("UseDevelopmentStorage=true", options);
        }

        throw new InvalidOperationException(
            "Azure Table Storage connection configuration is incomplete. " +
            "Please provide either a connection string, account name with key, or service URI with credentials.");
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void Dispose()
    {
        // TableServiceClient doesn't implement IDisposable
        // But we set the reference to null to allow GC
        _client = null;
    }
}