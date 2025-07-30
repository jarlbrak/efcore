// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.AzureTable.Infrastructure;

public class AzureTableOptionsExtensionTest
{
    [Fact]
    public void Can_create_options_extension()
    {
        var extension = new AzureTableOptionsExtension();

        Assert.NotNull(extension);
        Assert.Equal("AzureTable", extension.LogFragment);
    }

    [Fact]
    public void Can_set_connection_string()
    {
        var extension = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net");

        Assert.Equal("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net", extension.ConnectionString);
    }

    [Fact]
    public void Can_set_table_name_prefix()
    {
        var extension = new AzureTableOptionsExtension()
            .WithTableNamePrefix("MyApp");

        Assert.Equal("MyApp", extension.TableNamePrefix);
    }

    [Fact]
    public void Options_extension_info_is_correctly_populated()
    {
        var extension = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net")
            .WithTableNamePrefix("MyApp");

        var info = extension.Info;

        Assert.Contains("ConnectionString", info);
        Assert.Contains("TableNamePrefix=MyApp", info);
    }

    [Fact]
    public void Service_provider_hash_is_stable()
    {
        var extension1 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net");

        var extension2 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net");

        Assert.Equal(extension1.GetServiceProviderHashCode(), extension2.GetServiceProviderHashCode());
    }

    [Fact]
    public void Service_provider_hash_changes_with_different_options()
    {
        var extension1 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test1;AccountKey=key;EndpointSuffix=core.windows.net");

        var extension2 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test2;AccountKey=key;EndpointSuffix=core.windows.net");

        Assert.NotEqual(extension1.GetServiceProviderHashCode(), extension2.GetServiceProviderHashCode());
    }

    [Fact]
    public void Can_populate_debug_info()
    {
        var extension = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net")
            .WithTableNamePrefix("MyApp");

        var debugInfo = new Dictionary<string, string>();
        extension.PopulateDebugInfo(debugInfo);

        Assert.Contains("AzureTable:ConnectionString", debugInfo.Keys);
        Assert.Contains("AzureTable:TableNamePrefix", debugInfo.Keys);
        Assert.Equal("MyApp", debugInfo["AzureTable:TableNamePrefix"]);
    }

    [Fact]
    public void Can_apply_services()
    {
        var extension = new AzureTableOptionsExtension();
        var services = new ServiceCollection();

        extension.ApplyServices(services);

        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<IAzureTableClientWrapper>());
        Assert.NotNull(serviceProvider.GetService<ITypeMappingSource>());
    }

    [Fact]
    public void Extensions_with_same_options_are_equal()
    {
        var extension1 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net")
            .WithTableNamePrefix("MyApp");

        var extension2 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net")
            .WithTableNamePrefix("MyApp");

        Assert.True(extension1.Equals(extension2));
        Assert.Equal(extension1.GetHashCode(), extension2.GetHashCode());
    }

    [Fact]
    public void Extensions_with_different_options_are_not_equal()
    {
        var extension1 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test1;AccountKey=key;EndpointSuffix=core.windows.net");

        var extension2 = new AzureTableOptionsExtension()
            .WithConnectionString("DefaultEndpointsProtocol=https;AccountName=test2;AccountKey=key;EndpointSuffix=core.windows.net");

        Assert.False(extension1.Equals(extension2));
        Assert.NotEqual(extension1.GetHashCode(), extension2.GetHashCode());
    }
}