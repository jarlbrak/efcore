// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure;
using Microsoft.EntityFrameworkCore.AzureTable.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Extensions;

public class AzureTableDbContextOptionsExtensionsTest
{
    [Fact]
    public void UseAzureTable_WithConnectionString_ConfiguresOptions()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString);

        // Assert
        var options = optionsBuilder.Options;
        var extension = options.FindExtension<AzureTableOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Equal(connectionString, extension.ConnectionString);
    }

    [Fact]
    public void UseAzureTable_WithConnectionStringAndAction_ConfiguresOptions()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString, options =>
        {
            options.UseTableNamePrefix("MyApp");
        });

        // Assert
        var dbOptions = optionsBuilder.Options;
        var extension = dbOptions.FindExtension<AzureTableOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Equal(connectionString, extension.ConnectionString);
        Assert.Equal("MyApp", extension.TableNamePrefix);
    }

    [Fact]
    public void UseAzureTable_ConfiguresProviderName()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString);

        // Assert
        var options = optionsBuilder.Options;
        Assert.Contains(options.Extensions, e => e is AzureTableOptionsExtension);
    }

    [Fact]
    public void UseAzureTable_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => optionsBuilder.UseAzureTable((string)null!));
    }

    [Fact]
    public void UseAzureTable_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => optionsBuilder.UseAzureTable(string.Empty));
    }

    [Fact]
    public void UseAzureTable_CanChainMultipleConfigurations()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder
            .UseAzureTable(connectionString, options =>
            {
                options.UseTableNamePrefix("MyApp");
            })
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();

        // Assert
        var options = optionsBuilder.Options;
        var extension = options.FindExtension<AzureTableOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Equal(connectionString, extension.ConnectionString);
        Assert.Equal("MyApp", extension.TableNamePrefix);
        Assert.True(options.IsSensitiveDataLoggingEnabled);
        Assert.True(options.IsDetailedErrorsEnabled);
    }

    [Fact]
    public void UseAzureTable_MultipleCallsOverridePreviousConfiguration()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString1 = "DefaultEndpointsProtocol=https;AccountName=test1;AccountKey=key;EndpointSuffix=core.windows.net";
        var connectionString2 = "DefaultEndpointsProtocol=https;AccountName=test2;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString1, options => options.UseTableNamePrefix("App1"));
        optionsBuilder.UseAzureTable(connectionString2, options => options.UseTableNamePrefix("App2"));

        // Assert
        var options = optionsBuilder.Options;
        var extension = options.FindExtension<AzureTableOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Equal(connectionString2, extension.ConnectionString);
        Assert.Equal("App2", extension.TableNamePrefix);
    }

    [Fact]
    public void AzureTableDbContextOptionsBuilder_TableNamePrefix_SetsCorrectly()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString, options =>
        {
            options.UseTableNamePrefix("Production");
        });

        // Assert
        var dbOptions = optionsBuilder.Options;
        var extension = dbOptions.FindExtension<AzureTableOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Equal("Production", extension.TableNamePrefix);
    }

    [Fact]
    public void AzureTableDbContextOptionsBuilder_CanClearTableNamePrefix()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString, options =>
        {
            options.UseTableNamePrefix("Initial");
            options.UseTableNamePrefix(null);
        });

        // Assert
        var dbOptions = optionsBuilder.Options;
        var extension = dbOptions.FindExtension<AzureTableOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Null(extension.TableNamePrefix);
    }

    [Fact]
    public void AzureTableDbContextOptionsBuilder_ChainsMethods()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        optionsBuilder.UseAzureTable(connectionString, options =>
        {
            var result = options.UseTableNamePrefix("MyApp");
            Assert.IsType<AzureTableDbContextOptionsBuilder>(result);
        });

        // Assert - Test passes if no exception is thrown above
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }
}