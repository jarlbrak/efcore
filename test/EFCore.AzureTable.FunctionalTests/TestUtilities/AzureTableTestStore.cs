// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.Tables;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class AzureTableTestStore : TestStore
{
    public AzureTableTestStore(string name = "AzureTableTest", bool shared = true) 
        : base(name, shared)
    {
    }

    public static AzureTableTestStore GetOrCreate(string name)
        => new(name);

    public static AzureTableTestStore Create(string name)
        => new(name, shared: false);

    protected override TestStoreIndex GetTestStoreIndex(IServiceProvider? serviceProvider)
        => serviceProvider == null
            ? base.GetTestStoreIndex(null)
            : serviceProvider.GetService<TestStoreIndex>() ?? base.GetTestStoreIndex(serviceProvider);

    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        => builder.UseAzureTable("UseDevelopmentStorage=true");

    public override Task CleanAsync(DbContext context)
    {
        // Azure Table Storage with emulator doesn't need explicit cleanup
        // Tests use unique table names or partition keys
        return Task.CompletedTask;
    }
}