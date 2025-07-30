// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableSingletonOptions : IAzureTableSingletonOptions
{
    /// <inheritdoc />
    public virtual string? ConnectionString { get; private set; }

    /// <inheritdoc />
    public virtual string? AccountName { get; private set; }

    /// <inheritdoc />
    public virtual string? AccountKey { get; private set; }

    /// <inheritdoc />
    public virtual Uri? ServiceUri { get; private set; }

    /// <inheritdoc />
    public virtual string? SharedAccessSignature { get; private set; }

    /// <inheritdoc />
    public virtual TokenCredential? TokenCredential { get; private set; }

    /// <inheritdoc />
    public virtual bool? EnableBatching { get; private set; }

    /// <inheritdoc />
    public virtual TimeSpan? RequestTimeout { get; private set; }

    /// <inheritdoc />
    public virtual int? MaxRetryAttempts { get; private set; }

    /// <inheritdoc />
    public virtual void Initialize(IDbContextOptions options)
    {
        var azureTableOptions = options.FindExtension<AzureTableOptionsExtension>();

        if (azureTableOptions != null)
        {
            ConnectionString = azureTableOptions.ConnectionString;
            AccountName = azureTableOptions.AccountName;
            AccountKey = azureTableOptions.AccountKey;
            ServiceUri = azureTableOptions.ServiceUri;
            SharedAccessSignature = azureTableOptions.SasToken;
            TokenCredential = azureTableOptions.TokenCredential;
            EnableBatching = azureTableOptions.EnableBatching;
            RequestTimeout = azureTableOptions.RequestTimeout;
            MaxRetryAttempts = azureTableOptions.MaxRetryAttempts;
        }
    }

    /// <inheritdoc />
    public virtual void Validate(IDbContextOptions options)
    {
        var azureTableOptions = options.FindExtension<AzureTableOptionsExtension>();

        if (azureTableOptions != null)
        {
            if (ConnectionString != azureTableOptions.ConnectionString)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(ConnectionString),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (AccountName != azureTableOptions.AccountName)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(AccountName),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (AccountKey != azureTableOptions.AccountKey)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(AccountKey),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (ServiceUri != azureTableOptions.ServiceUri)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(ServiceUri),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (SharedAccessSignature != azureTableOptions.SasToken)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(SharedAccessSignature),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (TokenCredential != azureTableOptions.TokenCredential)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(TokenCredential),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (EnableBatching != azureTableOptions.EnableBatching)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(EnableBatching),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (RequestTimeout != azureTableOptions.RequestTimeout)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(RequestTimeout),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            if (MaxRetryAttempts != azureTableOptions.MaxRetryAttempts)
            {
                throw new InvalidOperationException(
                    CoreStrings.SingletonOptionChanged(
                        nameof(MaxRetryAttempts),
                        nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }
        }
    }
}