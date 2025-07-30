// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class AzureTableConventionSetBuilder : ProviderConventionSetBuilder
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public AzureTableConventionSetBuilder(
        ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override ConventionSet CreateConventionSet()
    {
        var conventionSet = base.CreateConventionSet();

        // Add Azure Table-specific conventions using Cosmos pattern
        conventionSet.Add(new AzureTableTableNameConvention(Dependencies));
        conventionSet.Add(new AzureTableETagPropertyConvention());
        conventionSet.Add(new AzureTablePartitionKeyInPrimaryKeyConvention(Dependencies));

        // Replace conventions with Azure Table-specific implementations
        conventionSet.Replace<ValueGenerationConvention>(new AzureTableValueGenerationConvention(Dependencies));
        conventionSet.Replace<KeyDiscoveryConvention>(new AzureTableKeyDiscoveryConvention(Dependencies));
        conventionSet.Replace<DiscriminatorConvention>(new AzureTableDiscriminatorConvention(Dependencies));

        return conventionSet;
    }
}