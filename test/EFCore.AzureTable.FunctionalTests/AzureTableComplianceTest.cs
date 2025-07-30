// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.EntityFrameworkCore.AzureTable;

public class AzureTableComplianceTest : ComplianceTestBase
{
    protected override ICollection<Type> IgnoredTestBases { get; } = new HashSet<Type>
    {
        // Add test base types that should be ignored for Azure Table Storage
        // due to limitations or incompatibilities
    };

    protected override Assembly TargetAssembly => typeof(AzureTableServiceCollectionExtensions).Assembly;
}