// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.DataAnnotations;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata.Conventions;

public class AzureTableKeyDiscoveryConventionTest
{
    #region Phase 1 Tests - Explicit Names and Data Annotations

    [Fact]
    public void Discovers_partition_key_by_property_name()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithExplicitNames>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("PartitionKey", partitionKeyProperty.Name);
        Assert.Equal("RowKey", rowKeyProperty.Name);
        
        // Verify composite primary key
        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(2, primaryKey.Properties.Count);
        Assert.Contains(partitionKeyProperty, primaryKey.Properties);
        Assert.Contains(rowKeyProperty, primaryKey.Properties);
    }

    [Fact]
    public void Discovers_partition_key_by_case_insensitive_property_name()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithCaseInsensitiveNames>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("partitionkey", partitionKeyProperty.Name);
        Assert.Equal("rowkey", rowKeyProperty.Name);
    }

    [Fact]
    public void Discovers_keys_by_data_annotations()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithDataAnnotations>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("TenantId", partitionKeyProperty.Name);
        Assert.Equal("EntityId", rowKeyProperty.Name);
    }

    [Fact]
    public void Data_annotations_take_precedence_over_property_names()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithAnnotationPrecedence>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("CustomPartition", partitionKeyProperty.Name);
        Assert.Equal("CustomRow", rowKeyProperty.Name);
    }

    #endregion

    #region Phase 2 Tests - Domain Patterns

    [Fact]
    public void Discovers_row_key_by_id_property()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithIdProperty>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(rowKeyProperty);
        Assert.Equal("Id", rowKeyProperty.Name);
    }

    [Fact]
    public void Discovers_row_key_by_entity_name_id_pattern()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<Customer>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(rowKeyProperty);
        Assert.Equal("CustomerId", rowKeyProperty.Name);
    }

    [Fact]
    public void Id_property_takes_precedence_over_entity_name_id()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithBothIdPatterns>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var rowKeyProperty = entityType.GetRowKeyProperty();

        Assert.NotNull(rowKeyProperty);
        Assert.Equal("Id", rowKeyProperty.Name);
    }

    [Fact]
    public void Discovers_partition_key_with_highest_precedence_user_id_pattern()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithMultiplePartitionCandidates>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.Equal("UserId", partitionKeyProperty.Name);
    }

    [Fact]
    public void Discovers_partition_key_with_tenant_id_precedence()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithTenantAndRegionId>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.Equal("TenantId", partitionKeyProperty.Name);
    }

    [Fact]
    public void Discovers_partition_key_with_low_precedence_category_name()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithCategoryName>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.Equal("Category", partitionKeyProperty.Name);
    }

    [Fact]
    public void High_precedence_patterns_override_low_precedence_names()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithMixedPrecedence>();

        // Act
        ApplyConvention(modelBuilder);

        // Assert
        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();

        Assert.NotNull(partitionKeyProperty);
        Assert.Equal("TenantId", partitionKeyProperty.Name); // Should pick TenantId over Category
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Throws_helpful_error_when_no_partition_key_found()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithOnlyRowKey>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ApplyConvention(modelBuilder));
        
        Assert.Contains("EntityWithOnlyRowKey", exception.Message);
        Assert.Contains("partition key", exception.Message);
        Assert.Contains("Add a 'PartitionKey' property", exception.Message);
        Assert.Contains("UserId", exception.Message);
        Assert.Contains("TenantId", exception.Message);
        Assert.Contains("RegionId", exception.Message);
        Assert.Contains("[PartitionKey]", exception.Message);
    }

    [Fact]
    public void Throws_helpful_error_when_no_row_key_found()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithOnlyPartitionKey>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ApplyConvention(modelBuilder));
        
        Assert.Contains("EntityWithOnlyPartitionKey", exception.Message);
        Assert.Contains("row key", exception.Message);
        Assert.Contains("Add a 'RowKey' property", exception.Message);
        Assert.Contains("Add an 'Id'", exception.Message);
        Assert.Contains("EntityWithOnlyPartitionKeyId", exception.Message);
        Assert.Contains("[RowKey]", exception.Message);
    }

    [Fact]
    public void Throws_helpful_error_when_no_keys_found()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithoutKeys>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ApplyConvention(modelBuilder));
        
        Assert.Contains("EntityWithoutKeys", exception.Message);
        Assert.Contains("partition key and row key", exception.Message);
        Assert.Contains("OnModelCreating", exception.Message);
        Assert.Contains("HasKey", exception.Message);
        Assert.Contains("ToAzureTableColumn", exception.Message);
        Assert.Contains("data annotations", exception.Message);
    }

    [Fact]
    public void Error_message_includes_configuration_examples()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<EntityWithoutKeys>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ApplyConvention(modelBuilder));
        
        // Check for explicit configuration example
        Assert.Contains("modelBuilder.Entity<EntityWithoutKeys>", exception.Message);
        Assert.Contains(".HasKey(e => new { e.YourPartitionKey, e.YourRowKey })", exception.Message);
        Assert.Contains(".ToAzureTableColumn(\"PartitionKey\")", exception.Message);
        Assert.Contains(".ToAzureTableColumn(\"RowKey\")", exception.Message);
    }

    #endregion

    #region Helper Methods and Test Entities

    private ModelBuilder CreateModelBuilder()
    {
        return new ModelBuilder();
    }

    private void ApplyConvention(ModelBuilder modelBuilder)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEntityFrameworkAzureTable();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var dependencies = serviceProvider.GetRequiredService<ProviderConventionSetBuilderDependencies>();
        var convention = new AzureTableKeyDiscoveryConvention(dependencies);
        
        var model = (IConventionModel)modelBuilder.Model;
        var modelBuilder2 = model.Builder;
        
        // Create a simple convention context
        var conventionContext = new TestConventionContext<IConventionModelBuilder>(modelBuilder2);
        
        convention.ProcessModelFinalizing(modelBuilder2, conventionContext);
    }

    #endregion

    #region Test Entity Classes

    private class EntityWithExplicitNames
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class EntityWithCaseInsensitiveNames
    {
        public string partitionkey { get; set; } = null!;
        public string rowkey { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class EntityWithDataAnnotations
    {
        [PartitionKey]
        public string TenantId { get; set; } = null!;
        
        [RowKey]
        public string EntityId { get; set; } = null!;
        
        public string? Data { get; set; }
    }

    private class EntityWithAnnotationPrecedence
    {
        public string PartitionKey { get; set; } = null!; // This should be ignored
        public string RowKey { get; set; } = null!; // This should be ignored
        
        [PartitionKey]
        public string CustomPartition { get; set; } = null!;
        
        [RowKey]
        public string CustomRow { get; set; } = null!;
        
        public string? Data { get; set; }
    }

    private class EntityWithIdProperty
    {
        public string Id { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class Customer
    {
        public string CustomerId { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string? Name { get; set; }
    }

    private class EntityWithBothIdPatterns
    {
        public string Id { get; set; } = null!;
        public string EntityWithBothIdPatternsId { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class EntityWithMultiplePartitionCandidates
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string RegionId { get; set; } = null!;
        public string Category { get; set; } = null!;
    }

    private class EntityWithTenantAndRegionId
    {
        public string Id { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string RegionId { get; set; } = null!;
        public string Category { get; set; } = null!;
    }

    private class EntityWithCategoryName
    {
        public string Id { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class EntityWithMixedPrecedence
    {
        public string Id { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Department { get; set; } = null!;
    }

    private class EntityWithOnlyRowKey
    {
        public string Id { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class EntityWithOnlyPartitionKey
    {
        public string TenantId { get; set; } = null!;
        public string? Data { get; set; }
    }

    private class EntityWithoutKeys
    {
        public string? Data { get; set; }
        public int Number { get; set; }
    }

    #endregion

    private class TestConventionContext<T> : IConventionContext<T>
    {
        public TestConventionContext(T result)
        {
            Result = result;
        }

        public T Result { get; }
        public bool ShouldStopProcessing => false;

        public void StopProcessing()
        {
            // No-op for testing
        }

        public void StopProcessing(T? result)
        {
            // No-op for testing
        }

        public void StopProcessingIfChanged(object? originalResult)
        {
            // No-op for testing
        }

        public void StopProcessingIfChanged(T? originalResult)
        {
            // No-op for testing
        }

        public void PreventConvention(Type conventionType)
        {
            // No-op for testing
        }

        public void PreventConvention<TConvention>()
        {
            // No-op for testing
        }

        public IConventionBatch DelayConventions()
        {
            // No-op for testing - return a batch that does nothing
            return new TestConventionBatch();
        }

        private class TestConventionBatch : IConventionBatch
        {
            public IConventionForeignKey? Run(IConventionForeignKey foreignKey) => foreignKey;
            public IMetadataReference<IConventionForeignKey> Track(IConventionForeignKey foreignKey) => new TestMetadataReference(foreignKey);
            public void Dispose() { }
        }

        private class TestMetadataReference : IMetadataReference<IConventionForeignKey>
        {
            public TestMetadataReference(IConventionForeignKey foreignKey)
            {
                Object = foreignKey;
            }

            public IConventionForeignKey Object { get; }
            public void Dispose() { }
        }

        private class TestDisposable : IDisposable
        {
            public void Dispose()
            {
                // No-op
            }
        }
    }
}