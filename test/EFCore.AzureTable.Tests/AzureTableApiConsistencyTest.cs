// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.AzureTable;

public class AzureTableApiConsistencyTest : ApiConsistencyTestBase<AzureTableApiConsistencyTest.AzureTableApiConsistencyFixture>
{
    public AzureTableApiConsistencyTest(AzureTableApiConsistencyFixture fixture)
        : base(fixture)
    {
    }

    protected override void AddServices(ServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkAzureTable();

    protected override Assembly TargetAssembly
        => typeof(AzureTableServiceCollectionExtensions).Assembly;

    public class AzureTableApiConsistencyFixture : ApiConsistencyFixtureBase
    {
        public override HashSet<Type> FluentApiTypes { get; } = new()
        {
            typeof(AzureTableDbContextOptionsBuilder),
            typeof(AzureTableModelBuilderExtensions),
            typeof(AzureTablePropertyBuilderExtensions),
            typeof(AzureTableEntityTypeBuilderExtensions)
        };
    }
}