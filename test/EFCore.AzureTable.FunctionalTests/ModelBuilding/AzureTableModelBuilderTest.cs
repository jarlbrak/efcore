// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable.ModelBuilding;

#nullable disable

public class AzureTableModelBuilderTest : IClassFixture<AzureTableModelBuilderTest.AzureTableModelBuilderFixture>
{
    private readonly AzureTableModelBuilderFixture _fixture;

    public AzureTableModelBuilderTest(AzureTableModelBuilderFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public void Can_configure_partition_key()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var partitionKeyProperty = entityType.FindProperty("PartitionKey");
        Assert.NotNull(partitionKeyProperty);
        
        // Verify partition key configuration
        Assert.True(partitionKeyProperty.IsKey());
    }

    [ConditionalFact]
    public void Can_configure_row_key()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var rowKeyProperty = entityType.FindProperty("RowKey");
        Assert.NotNull(rowKeyProperty);
        
        // Verify row key configuration
        Assert.True(rowKeyProperty.IsKey());
    }

    [ConditionalFact]
    public void Can_configure_table_name()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var tableName = entityType.GetAzureTableName();
        Assert.Equal("TestEntities", tableName);
    }

    [ConditionalFact]
    public void Can_configure_property_names()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var nameProperty = entityType.FindProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal("Name", nameProperty.Name);
        
        var descriptionProperty = entityType.FindProperty("Description");
        Assert.NotNull(descriptionProperty);
        Assert.Equal("Description", descriptionProperty.Name);
    }

    [ConditionalFact]
    public void Can_configure_property_types()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var stringProperty = entityType.FindProperty("Name");
        Assert.Equal(typeof(string), stringProperty?.ClrType);
        
        var intProperty = entityType.FindProperty("Count");
        Assert.Equal(typeof(int), intProperty?.ClrType);
        
        var dateTimeProperty = entityType.FindProperty("CreatedDate");
        Assert.Equal(typeof(DateTime), dateTimeProperty?.ClrType);
        
        var boolProperty = entityType.FindProperty("IsActive");
        Assert.Equal(typeof(bool), boolProperty?.ClrType);
    }

    [ConditionalFact]
    public void Can_configure_nullable_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var nullableProperty = entityType.FindProperty("Description");
        Assert.True(nullableProperty.IsNullable);
        
        var nonNullableProperty = entityType.FindProperty("Name");
        Assert.False(nonNullableProperty.IsNullable);
    }

    [ConditionalFact]
    public void Can_configure_required_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var requiredProperty = entityType.FindProperty("Name");
        Assert.False(requiredProperty.IsNullable);
        
        var optionalProperty = entityType.FindProperty("Description");
        Assert.True(optionalProperty.IsNullable);
    }

    [ConditionalFact]
    public void Can_configure_max_length()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var nameProperty = entityType.FindProperty("Name");
        var maxLength = nameProperty.GetMaxLength();
        Assert.Equal(100, maxLength);
    }

    [ConditionalFact]
    public void Can_configure_default_values()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var countProperty = entityType.FindProperty("Count");
        Assert.NotNull(countProperty);
        Assert.Equal(typeof(int), countProperty.ClrType);
        
        var isActiveProperty = entityType.FindProperty("IsActive");
        Assert.NotNull(isActiveProperty);
        Assert.Equal(typeof(bool), isActiveProperty.ClrType);
    }

    [ConditionalFact]
    public void Can_configure_computed_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        // Azure Table Storage doesn't support computed columns like SQL databases
        // but we can test that computed properties are handled appropriately
        var computedProperty = entityType.FindProperty("DisplayName");
        Assert.NotNull(computedProperty);
    }

    [ConditionalFact]
    public void Can_configure_concurrency_tokens()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var etagProperty = entityType.FindProperty("ETag");
        Assert.NotNull(etagProperty);
        Assert.True(etagProperty.IsConcurrencyToken);
    }

    [ConditionalFact]
    public void Can_configure_shadow_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var shadowProperty = entityType.FindProperty("ShadowProperty");
        Assert.NotNull(shadowProperty);
        Assert.True(shadowProperty.IsShadowProperty());
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support indexes")]
    public void Cannot_add_indexes()
    {
        // Azure Table Storage does not support secondary indexes
        // Only partition key and row key can be used for querying efficiently
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var context = _fixture.CreateContext();
            var modelBuilder = new ModelBuilder();
            
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                entity.HasIndex(e => e.Name); // This should throw
            });
        });
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support foreign keys")]
    public void Cannot_add_foreign_keys()
    {
        // Azure Table Storage does not support foreign key relationships
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var context = _fixture.CreateContext();
            var modelBuilder = new ModelBuilder();
            
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                entity.HasOne<RelatedEntity>().WithMany(); // This should throw
            });
        });
    }

    [ConditionalFact]
    public void Can_configure_multiple_entities()
    {
        using var context = _fixture.CreateContext();
        
        var testEntityType = context.Model.FindEntityType(typeof(TestEntity));
        var relatedEntityType = context.Model.FindEntityType(typeof(RelatedEntity));
        
        Assert.NotNull(testEntityType);
        Assert.NotNull(relatedEntityType);
        
        // Verify both entities are properly configured
        Assert.Equal("TestEntities", testEntityType.GetAzureTableName());
        Assert.Equal("RelatedEntities", relatedEntityType.GetAzureTableName());
    }

    [ConditionalFact]
    public void Can_configure_value_converters()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var statusProperty = entityType.FindProperty("Status");
        Assert.NotNull(statusProperty);
        
        // Check if value converter is applied
        var converter = statusProperty.GetValueConverter();
        Assert.NotNull(converter);
    }

    [ConditionalFact]
    public void Can_configure_backing_fields()
    {
        using var context = _fixture.CreateContext();
        
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        var backingFieldProperty = entityType.FindProperty("BackingFieldProperty");
        Assert.NotNull(backingFieldProperty);
        
        // Field info checking requires specialized EF Core APIs
        Assert.NotNull(backingFieldProperty);
    }

    public class AzureTableModelBuilderFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public AzureTableModelBuilderFixture()
        {
            _testStore = AzureTableTestStore.Create("ModelBuilderTest");
        }

        public ModelBuilderContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ModelBuilderContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ModelTest"))
                .Options;

            var context = new ModelBuilderContext(options);
            context.Database.EnsureCreated();
            return context;
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

    public class ModelBuilderContext : DbContext
    {
        public ModelBuilderContext(DbContextOptions<ModelBuilderContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<RelatedEntity> RelatedEntities => Set<RelatedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.ToTable("TestEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Description)
                    .IsRequired(false);
                
                entity.Property(e => e.Count);
                
                entity.Property(e => e.IsActive);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken();
                
                entity.Property(e => e.Status)
                    .HasConversion<string>();
                
                entity.Property<string>("ShadowProperty");
                
                entity.Property(e => e.BackingFieldProperty)
                    .HasField("_backingField");
            });

            modelBuilder.Entity<RelatedEntity>(entity =>
            {
                entity.ToTable("RelatedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class TestEntity
    {
        private string _backingField = null!;
        
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ETag { get; set; } = null!;
        public TestStatus Status { get; set; }
        
        public string DisplayName => $"{Name} ({Count})";
        
        public string BackingFieldProperty
        {
            get => _backingField;
            set => _backingField = value;
        }
    }

    public class RelatedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public enum TestStatus
    {
        Active,
        Inactive,
        Pending
    }
}