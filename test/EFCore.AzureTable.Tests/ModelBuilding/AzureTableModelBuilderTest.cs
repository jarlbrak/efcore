// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.ModelBuilding;

public class AzureTableModelBuilderTest
{
    [Fact]
    public void Can_configure_entity_with_partition_and_row_key()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        var rowKeyProperty = entityType.GetRowKeyProperty();
        
        Assert.NotNull(partitionKeyProperty);
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("Region", partitionKeyProperty.Name);
        Assert.Equal("Id", rowKeyProperty.Name);
    }

    [Fact]
    public void Can_configure_table_name()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.ToAzureTable("CustomTableName");
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        Assert.Equal("CustomTableName", entityType.GetAzureTableName());
    }

    [Fact]
    public void Can_configure_concurrency_token()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
            entity.Property(e => e.ETag).IsConcurrencyToken();
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        var etagProperty = entityType.FindProperty("ETag");
        
        Assert.NotNull(etagProperty);
        Assert.True(etagProperty.IsConcurrencyToken);
    }

    [Fact]
    public void Can_configure_timestamp_property()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsTimestamp();
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        var timestampProperty = entityType.FindProperty("Timestamp");
        
        Assert.NotNull(timestampProperty);
        Assert.True(timestampProperty.IsTimestamp());
    }

    [Fact]
    public void Can_ignore_navigation_properties()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
            entity.Ignore(e => e.RelatedEntity);
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        var navigationProperty = entityType.FindNavigation("RelatedEntity");
        
        Assert.Null(navigationProperty);
    }

    [Fact]
    public void Can_configure_complex_property_as_json()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
            entity.Property(e => e.ComplexData).HasConversion<string>();
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        var complexProperty = entityType.FindProperty("ComplexData");
        
        Assert.NotNull(complexProperty);
        Assert.NotNull(complexProperty.GetValueConverter());
    }

    [Fact]
    public void Partition_key_property_is_automatically_required()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        
        Assert.NotNull(partitionKeyProperty);
        Assert.False(partitionKeyProperty.IsNullable);
    }

    [Fact]
    public void Row_key_property_is_automatically_required()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
        });

        var model = modelBuilder.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(TestEntity));
        var rowKeyProperty = entityType.GetRowKeyProperty();
        
        Assert.NotNull(rowKeyProperty);
        Assert.False(rowKeyProperty.IsNullable);
    }

    [Fact]
    public void Can_configure_multiple_entities()
    {
        // Arrange & Act
        var modelBuilder = CreateModelBuilder();
        
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.ToAzureTable("TestEntities");
            entity.HasPartitionKey(e => e.Region);
            entity.HasRowKey(e => e.Id);
        });

        modelBuilder.Entity<AnotherEntity>(entity =>
        {
            entity.ToAzureTable("AnotherEntities");
            entity.HasPartitionKey(e => e.Category);
            entity.HasRowKey(e => e.Code);
        });

        var model = modelBuilder.Model;

        // Assert
        var testEntityType = model.FindEntityType(typeof(TestEntity));
        var anotherEntityType = model.FindEntityType(typeof(AnotherEntity));
        
        Assert.NotNull(testEntityType);
        Assert.NotNull(anotherEntityType);
        
        Assert.Equal("TestEntities", testEntityType.GetAzureTableName());
        Assert.Equal("AnotherEntities", anotherEntityType.GetAzureTableName());
    }

    private static ModelBuilder CreateModelBuilder()
        => new ModelBuilder();

    private class TestEntity
    {
        public string Id { get; set; } = null!;
        public string Region { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ComplexData? ComplexData { get; set; }
        public RelatedEntity? RelatedEntity { get; set; }
    }

    private class AnotherEntity
    {
        public string Code { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    private class RelatedEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class ComplexData
    {
        public string Value1 { get; set; } = null!;
        public int Value2 { get; set; }
    }
}