// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class ModelValidationAzureTableTest : IClassFixture<ModelValidationAzureTableTest.ModelValidationAzureTableFixture>
{
    private readonly ModelValidationAzureTableFixture _fixture;

    public ModelValidationAzureTableTest(ModelValidationAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public void Validates_partition_key_configuration()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<ValidEntity>(entity =>
        {
            entity.ToAzureTable("ValidEntities");
            entity.HasPartitionKey(e => e.PartitionKey);
            entity.HasRowKey(e => e.RowKey);
        });

        var model = modelBuilder.FinalizeModel();
        var entity = model.FindEntityType(typeof(ValidEntity));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindProperty("PartitionKey"));
        Assert.NotNull(entity.FindProperty("RowKey"));
    }

    [ConditionalFact]
    public void Throws_when_partition_key_is_missing()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<InvalidEntity>(entity =>
        {
            entity.ToAzureTable("InvalidEntities");
            // Missing partition key configuration
            entity.HasRowKey(e => e.Id);
        });

        var exception = Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel());
        Assert.Contains("partition key", exception.Message.ToLower());
    }

    [ConditionalFact]
    public void Throws_when_row_key_is_missing()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<InvalidEntity>(entity =>
        {
            entity.ToAzureTable("InvalidEntities");
            entity.HasPartitionKey(e => e.Id);
            // Missing row key configuration
        });

        var exception = Assert.Throws<InvalidOperationException>(() => modelBuilder.FinalizeModel());
        Assert.Contains("row key", exception.Message.ToLower());
    }

    [ConditionalFact]
    public void Validates_discriminator_configuration_with_inheritance()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<BaseEntity>(entity =>
        {
            entity.ToAzureTable("Entities");
            entity.HasPartitionKey(e => e.PartitionKey);
            entity.HasRowKey(e => e.RowKey);
            entity.HasDiscriminator<string>("Discriminator");
        });

        modelBuilder.Entity<DerivedEntity1>()
            .HasBaseType<BaseEntity>();

        modelBuilder.Entity<DerivedEntity2>()
            .HasBaseType<BaseEntity>();

        var model = modelBuilder.FinalizeModel();
        var baseEntity = model.FindEntityType(typeof(BaseEntity));
        var derived1 = model.FindEntityType(typeof(DerivedEntity1));
        var derived2 = model.FindEntityType(typeof(DerivedEntity2));

        Assert.NotNull(baseEntity);
        Assert.NotNull(derived1);
        Assert.NotNull(derived2);
        Assert.NotNull(baseEntity.FindDiscriminatorProperty());
        Assert.Equal("Discriminator", baseEntity.FindDiscriminatorProperty().Name);
    }

    [ConditionalFact]
    public void Validates_etag_and_timestamp_configuration()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<EntityWithConcurrency>(entity =>
        {
            entity.ToAzureTable("ConcurrencyEntities");
            entity.HasPartitionKey(e => e.PartitionKey);
            entity.HasRowKey(e => e.RowKey);
            entity.Property(e => e.ETag).IsConcurrencyToken();
            entity.Property(e => e.Timestamp).IsTimestamp();
        });

        var model = modelBuilder.FinalizeModel();
        var entityType = model.FindEntityType(typeof(EntityWithConcurrency));

        Assert.NotNull(entityType);
        
        var etagProperty = entityType.FindProperty("ETag");
        Assert.NotNull(etagProperty);
        Assert.True(etagProperty.IsConcurrencyToken);

        var timestampProperty = entityType.FindProperty("Timestamp");
        Assert.NotNull(timestampProperty);
        Assert.True(timestampProperty.IsTimestamp());
    }

    public class ModelValidationAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public ModelValidationAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("ModelValidationTest");
        }

        public void Dispose()
        {
            if (_testStore != null)
            {
                var disposeTask = _testStore.DisposeAsync();
                if (!disposeTask.IsCompleted)
                {
                    disposeTask.GetAwaiter().GetResult();
                }
            }
        }
    }

    public class ValidEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class InvalidEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public abstract class BaseEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class DerivedEntity1 : BaseEntity
    {
        public string Property1 { get; set; } = null!;
    }

    public class DerivedEntity2 : BaseEntity
    {
        public string Property2 { get; set; } = null!;
    }

    public class EntityWithConcurrency
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}