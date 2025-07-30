// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class AzureTableTestStoreFactory : ITestStoreFactory
{
    public static AzureTableTestStoreFactory Instance { get; } = new();

    protected AzureTableTestStoreFactory()
    {
    }

    public TestStore Create(string storeName)
        => AzureTableTestStore.Create(storeName);

    public TestStore GetOrCreate(string storeName)
        => AzureTableTestStore.GetOrCreate(storeName);

    public IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkAzureTable()
            .AddSingleton<TestStoreIndex>();

    public ListLoggerFactory CreateListLoggerFactory(Func<string, bool> shouldLogCategory)
        => new(shouldLogCategory);
}