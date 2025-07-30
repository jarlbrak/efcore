// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.AzureTable.Extensions;

public class AzureTableServiceCollectionExtensionsTest
{
    [Fact]
    public void AddEntityFrameworkAzureTable_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEntityFrameworkAzureTable();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<IAzureTableClientWrapper>());
        Assert.NotNull(serviceProvider.GetService<ITypeMappingSource>());
        Assert.NotNull(serviceProvider.GetService<IDatabaseProvider>());
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_RegistersServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEntityFrameworkAzureTable();

        // Assert
        var clientWrapperDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAzureTableClientWrapper));
        Assert.NotNull(clientWrapperDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, clientWrapperDescriptor.Lifetime);

        var typeMappingDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITypeMappingSource));
        Assert.NotNull(typeMappingDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, typeMappingDescriptor.Lifetime);
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEntityFrameworkAzureTable();
        services.AddEntityFrameworkAzureTable(); // Should not throw

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IAzureTableClientWrapper>());
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_WorksWithExistingEFCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEntityFramework(); // Add base EF services first
        services.AddEntityFrameworkAzureTable();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Should have both base EF services and Azure Table services
        Assert.NotNull(serviceProvider.GetService<IAzureTableClientWrapper>());
        Assert.NotNull(serviceProvider.GetService<ITypeMappingSource>());
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_RegistersAllRequiredInternalServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEntityFrameworkAzureTable();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify all key services are registered
        Assert.NotNull(serviceProvider.GetService<AzureTableTypeMappingSource>());
        Assert.NotNull(serviceProvider.GetService<AzureTableDatabaseProvider>());
        Assert.NotNull(serviceProvider.GetService<AzureTableClientWrapper>());
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEntityFrameworkAzureTable();
        services.AddEntityFrameworkAzureTable();

        // Assert
        var clientWrapperServices = services.Where(s => s.ServiceType == typeof(IAzureTableClientWrapper)).ToList();
        var typeMappingServices = services.Where(s => s.ServiceType == typeof(ITypeMappingSource)).ToList();

        // Should only have one registration for each service type (EF Core handles the deduplication)
        Assert.Single(clientWrapperServices);
        Assert.Single(typeMappingServices);
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_PreservesExistingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingService = new TestService();
        services.AddSingleton(existingService);

        // Act
        services.AddEntityFrameworkAzureTable();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Existing service should still be available
        Assert.Same(existingService, serviceProvider.GetService<TestService>());
        
        // New services should also be available
        Assert.NotNull(serviceProvider.GetService<IAzureTableClientWrapper>());
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEntityFrameworkAzureTable();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddEntityFrameworkAzureTable_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddEntityFrameworkAzureTable());
    }

    private class TestService
    {
        public string Value { get; } = "Test";
    }
}