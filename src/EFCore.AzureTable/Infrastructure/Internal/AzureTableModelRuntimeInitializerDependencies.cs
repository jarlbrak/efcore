// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

/// <summary>
///     Service dependencies parameter class for <see cref="AzureTableModelRuntimeInitializer" />
/// </summary>
/// <remarks>
///     This type is typically used by database providers (and other extensions). It is generally
///     not used in application code.
/// </remarks>
public sealed record AzureTableModelRuntimeInitializerDependencies
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableModelRuntimeInitializerDependencies()
    {
    }
}