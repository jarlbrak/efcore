// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.Metadata;

public class AzureTableMetadataExtensionsTest
{
    [Fact]
    public void Can_get_and_set_table_name()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.ToAzureTable("CustomTable");

        var entityType = entityTypeBuilder.Metadata;
        Assert.Equal("CustomTable", entityType.GetAzureTableName());
    }

    [Fact]
    public void Can_get_and_set_partition_key()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.HasPartitionKey(e => e.PartitionKey);

        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        
        Assert.NotNull(partitionKeyProperty);
        Assert.Equal("PartitionKey", partitionKeyProperty.Name);
        Assert.True(partitionKeyProperty.IsPartitionKey());
    }

    [Fact]
    public void Can_get_and_set_row_key()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.HasRowKey(e => e.RowKey);

        var entityType = entityTypeBuilder.Metadata;
        var rowKeyProperty = entityType.GetRowKeyProperty();
        
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("RowKey", rowKeyProperty.Name);
        Assert.True(rowKeyProperty.IsRowKey());
    }

    [Fact]
    public void Can_set_both_partition_and_row_key()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder
            .HasPartitionKey(e => e.PartitionKey)
            .HasRowKey(e => e.RowKey);

        var entityType = entityTypeBuilder.Metadata;
        
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        var rowKeyProperty = entityType.GetRowKeyProperty();
        
        Assert.NotNull(partitionKeyProperty);
        Assert.NotNull(rowKeyProperty);
        Assert.Equal("PartitionKey", partitionKeyProperty.Name);
        Assert.Equal("RowKey", rowKeyProperty.Name);
        Assert.True(partitionKeyProperty.IsPartitionKey());
        Assert.True(rowKeyProperty.IsRowKey());
    }

    [Fact]
    public void Partition_key_property_is_required()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.HasPartitionKey(e => e.PartitionKey);

        var entityType = entityTypeBuilder.Metadata;
        var partitionKeyProperty = entityType.GetPartitionKeyProperty();
        
        Assert.NotNull(partitionKeyProperty);
        Assert.False(partitionKeyProperty.IsNullable);
    }

    [Fact]
    public void Row_key_property_is_required()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.HasRowKey(e => e.RowKey);

        var entityType = entityTypeBuilder.Metadata;
        var rowKeyProperty = entityType.GetRowKeyProperty();
        
        Assert.NotNull(rowKeyProperty);
        Assert.False(rowKeyProperty.IsNullable);
    }

    [Fact]
    public void Can_check_if_property_is_etag()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.Property(e => e.ETag).IsConcurrencyToken();

        var entityType = entityTypeBuilder.Metadata;
        var etagProperty = entityType.FindProperty("ETag");
        
        Assert.NotNull(etagProperty);
        Assert.True(etagProperty.IsConcurrencyToken);
    }

    [Fact]
    public void Can_check_if_property_is_timestamp()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        entityTypeBuilder.Property(e => e.Timestamp).IsTimestamp();

        var entityType = entityTypeBuilder.Metadata;
        var timestampProperty = entityType.FindProperty("Timestamp");
        
        Assert.NotNull(timestampProperty);
        Assert.True(timestampProperty.IsTimestamp());
    }

    [Fact]
    public void Throws_when_partition_key_property_not_found()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        Assert.Throws<ArgumentException>(() => 
            entityTypeBuilder.HasPartitionKey("NonExistentProperty"));
    }

    [Fact]
    public void Throws_when_row_key_property_not_found()
    {
        var modelBuilder = CreateModelBuilder();
        var entityTypeBuilder = modelBuilder.Entity<TestEntity>();

        Assert.Throws<ArgumentException>(() => 
            entityTypeBuilder.HasRowKey("NonExistentProperty"));
    }

    private static ModelBuilder CreateModelBuilder()
        => new ModelBuilder();

    private class TestEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string? ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string? Data { get; set; }
    }
}