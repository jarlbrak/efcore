// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Azure.Core;

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableOptionsExtension : IDbContextOptionsExtension
{
    private string? _connectionString;
    private string? _accountName;
    private string? _accountKey;
    private string? _sasToken;
    private Uri? _serviceUri;
    private TokenCredential? _tokenCredential;
    private string? _tableNamePrefix;
    private bool? _enableBatching;
    private TimeSpan? _requestTimeout;
    private int? _maxRetryAttempts;
    private bool? _useUtcDateTimeConversion;
    private Func<ExecutionStrategyDependencies, IExecutionStrategy>? _executionStrategyFactory;
    private DbContextOptionsExtensionInfo? _info;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableOptionsExtension()
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected AzureTableOptionsExtension(AzureTableOptionsExtension copyFrom)
    {
        _connectionString = copyFrom._connectionString;
        _accountName = copyFrom._accountName;
        _accountKey = copyFrom._accountKey;
        _sasToken = copyFrom._sasToken;
        _serviceUri = copyFrom._serviceUri;
        _tokenCredential = copyFrom._tokenCredential;
        _tableNamePrefix = copyFrom._tableNamePrefix;
        _enableBatching = copyFrom._enableBatching;
        _requestTimeout = copyFrom._requestTimeout;
        _maxRetryAttempts = copyFrom._maxRetryAttempts;
        _useUtcDateTimeConversion = copyFrom._useUtcDateTimeConversion;
        _executionStrategyFactory = copyFrom._executionStrategyFactory;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string? ConnectionString => _connectionString;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string? AccountName => _accountName;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string? AccountKey => _accountKey;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string? SasToken => _sasToken;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Uri? ServiceUri => _serviceUri;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TokenCredential? TokenCredential => _tokenCredential;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string? TableNamePrefix => _tableNamePrefix;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual bool EnableBatching => _enableBatching ?? true;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TimeSpan? RequestTimeout => _requestTimeout;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual int MaxRetryAttempts => _maxRetryAttempts ?? 3;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual bool UseUtcDateTimeConversion => _useUtcDateTimeConversion ?? true;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Func<ExecutionStrategyDependencies, IExecutionStrategy>? ExecutionStrategyFactory
        => _executionStrategyFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithConnectionString(string? connectionString)
    {
        var extension = Clone();
        extension._connectionString = connectionString;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithAccountNameAndKey(string? accountName, string? accountKey)
    {
        var extension = Clone();
        extension._accountName = accountName;
        extension._accountKey = accountKey;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithSasToken(Uri? serviceUri, string? sasToken)
    {
        var extension = Clone();
        extension._serviceUri = serviceUri;
        extension._sasToken = sasToken;
        extension._connectionString = null;
        extension._accountName = null;
        extension._accountKey = null;
        extension._tokenCredential = null;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithTokenCredential(Uri? serviceUri, TokenCredential? tokenCredential)
    {
        var extension = Clone();
        extension._serviceUri = serviceUri;
        extension._tokenCredential = tokenCredential;
        extension._connectionString = null;
        extension._accountName = null;
        extension._accountKey = null;
        extension._sasToken = null;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithTableNamePrefix(string? tableNamePrefix)
    {
        var extension = Clone();
        extension._tableNamePrefix = tableNamePrefix;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithBatchingEnabled(bool enableBatching)
    {
        var extension = Clone();
        extension._enableBatching = enableBatching;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithRequestTimeout(TimeSpan? requestTimeout)
    {
        var extension = Clone();
        extension._requestTimeout = requestTimeout;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithMaxRetryAttempts(int maxRetryAttempts)
    {
        var extension = Clone();
        extension._maxRetryAttempts = maxRetryAttempts;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithUtcDateTimeConversion(bool useUtcDateTimeConversion)
    {
        var extension = Clone();
        extension._useUtcDateTimeConversion = useUtcDateTimeConversion;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual AzureTableOptionsExtension WithExecutionStrategyFactory(
        Func<ExecutionStrategyDependencies, IExecutionStrategy>? executionStrategyFactory)
    {
        var extension = Clone();
        extension._executionStrategyFactory = executionStrategyFactory;
        return extension;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual AzureTableOptionsExtension Clone()
        => new(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkAzureTable();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void Validate(IDbContextOptions options)
    {
        if (string.IsNullOrEmpty(_connectionString)
            && (string.IsNullOrEmpty(_accountName) || string.IsNullOrEmpty(_accountKey))
            && (_serviceUri == null || string.IsNullOrEmpty(_sasToken)))
        {
            throw new InvalidOperationException(
                AzureTable.Internal.AzureTableStrings.NoConnectionConfiguration);
        }
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private string? _logFragment;

        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        private new AzureTableOptionsExtension Extension
            => (AzureTableOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => true;

        public override string LogFragment
        {
            get
            {
                if (_logFragment == null)
                {
                    var builder = new StringBuilder();

                    if (!string.IsNullOrEmpty(Extension._connectionString))
                    {
                        builder.Append("ConnectionString=").Append(HashConnectionString(Extension._connectionString)).Append(' ');
                    }
                    else if (!string.IsNullOrEmpty(Extension._accountName))
                    {
                        builder.Append("AccountName=").Append(Extension._accountName).Append(' ');
                    }
                    else if (Extension._serviceUri != null)
                    {
                        builder.Append("ServiceUri=").Append(Extension._serviceUri.Host).Append(' ');
                    }

                    if (!string.IsNullOrEmpty(Extension._tableNamePrefix))
                    {
                        builder.Append("TableNamePrefix=").Append(Extension._tableNamePrefix).Append(' ');
                    }

                    if (Extension._enableBatching.HasValue)
                    {
                        builder.Append("EnableBatching=").Append(Extension._enableBatching.Value).Append(' ');
                    }

                    if (Extension._requestTimeout.HasValue)
                    {
                        builder.Append("RequestTimeout=").Append(Extension._requestTimeout.Value.TotalSeconds).Append("s ");
                    }

                    if (Extension._maxRetryAttempts.HasValue)
                    {
                        builder.Append("MaxRetryAttempts=").Append(Extension._maxRetryAttempts.Value).Append(' ');
                    }

                    if (Extension._useUtcDateTimeConversion.HasValue)
                    {
                        builder.Append("UseUtcDateTimeConversion=").Append(Extension._useUtcDateTimeConversion.Value).Append(' ');
                    }

                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }

        private static string HashConnectionString(string connectionString)
            => connectionString.GetHashCode().ToString("X", CultureInfo.InvariantCulture);

        public override int GetServiceProviderHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Extension._connectionString);
            hashCode.Add(Extension._accountName);
            hashCode.Add(Extension._accountKey);
            hashCode.Add(Extension._sasToken);
            hashCode.Add(Extension._serviceUri);
            hashCode.Add(Extension._tableNamePrefix);
            hashCode.Add(Extension._enableBatching);
            hashCode.Add(Extension._requestTimeout);
            hashCode.Add(Extension._maxRetryAttempts);
            hashCode.Add(Extension._useUtcDateTimeConversion);
            hashCode.Add(Extension._executionStrategyFactory);
            return hashCode.ToHashCode();
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.ConnectionString)]
                = (Extension._connectionString?.GetHashCode() ?? 0).ToString(CultureInfo.InvariantCulture);
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.AccountName)]
                = (Extension._accountName?.GetHashCode() ?? 0).ToString(CultureInfo.InvariantCulture);
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.TableNamePrefix)]
                = (Extension._tableNamePrefix?.GetHashCode() ?? 0).ToString(CultureInfo.InvariantCulture);
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.EnableBatching)]
                = Extension.EnableBatching.ToString(CultureInfo.InvariantCulture);
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.RequestTimeout)]
                = (Extension._requestTimeout?.TotalSeconds ?? 0).ToString(CultureInfo.InvariantCulture);
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.MaxRetryAttempts)]
                = Extension.MaxRetryAttempts.ToString(CultureInfo.InvariantCulture);
            debugInfo["AzureTable:" + nameof(AzureTableOptionsExtension.UseUtcDateTimeConversion)]
                = Extension.UseUtcDateTimeConversion.ToString(CultureInfo.InvariantCulture);
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo otherInfo
                && Extension._connectionString == otherInfo.Extension._connectionString
                && Extension._accountName == otherInfo.Extension._accountName
                && Extension._accountKey == otherInfo.Extension._accountKey
                && Extension._sasToken == otherInfo.Extension._sasToken
                && Extension._serviceUri == otherInfo.Extension._serviceUri
                && Extension._tableNamePrefix == otherInfo.Extension._tableNamePrefix
                && Extension._enableBatching == otherInfo.Extension._enableBatching
                && Extension._requestTimeout == otherInfo.Extension._requestTimeout
                && Extension._maxRetryAttempts == otherInfo.Extension._maxRetryAttempts
                && Extension._useUtcDateTimeConversion == otherInfo.Extension._useUtcDateTimeConversion
                && Extension._executionStrategyFactory == otherInfo.Extension._executionStrategyFactory;
    }
}