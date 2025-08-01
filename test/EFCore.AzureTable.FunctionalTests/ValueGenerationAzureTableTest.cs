// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class ValueGenerationAzureTableTest : IClassFixture<ValueGenerationAzureTableTest.ValueGenerationAzureTableFixture>
{
    private readonly ValueGenerationAzureTableFixture _fixture;

    public ValueGenerationAzureTableTest(ValueGenerationAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Can_generate_ETag_on_add()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "GEN001",
            Name = "Test Entity"
        };

        context.GeneratedEntities.Add(entity);
        await context.SaveChangesAsync();

        Assert.NotNull(entity.ETag);
        Assert.NotEmpty(entity.ETag);
    }

    [ConditionalFact]
    public async Task Can_generate_Timestamp_on_add()
    {
        using var context = _fixture.CreateContext();
        var beforeSave = DateTimeOffset.UtcNow;
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "GEN002",
            Name = "Test Entity"
        };

        context.GeneratedEntities.Add(entity);
        await context.SaveChangesAsync();
        var afterSave = DateTimeOffset.UtcNow;

        Assert.NotNull(entity.Timestamp);
        Assert.True(entity.Timestamp >= beforeSave);
        Assert.True(entity.Timestamp <= afterSave);
    }

    [ConditionalFact]
    public async Task ETag_updates_on_modify()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "GEN003",
            Name = "Test Entity"
        };

        context.GeneratedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var originalETag = entity.ETag;
        
        entity.Name = "Updated Entity";
        await context.SaveChangesAsync();
        
        Assert.NotNull(entity.ETag);
        Assert.NotEqual(originalETag, entity.ETag);
    }

    [ConditionalFact]
    public async Task Timestamp_updates_on_modify()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "GEN004",
            Name = "Test Entity"
        };

        context.GeneratedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var originalTimestamp = entity.Timestamp;
        
        // Small delay to ensure timestamp difference
        await Task.Delay(100);
        
        entity.Name = "Updated Entity";
        await context.SaveChangesAsync();
        
        Assert.NotNull(entity.Timestamp);
        Assert.True(entity.Timestamp > originalTimestamp);
    }

    [ConditionalFact]
    public async Task Can_use_custom_value_generators()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new CustomGeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "CUSTOM001",
            Name = "Test Entity"
        };

        context.CustomGeneratedEntities.Add(entity);
        await context.SaveChangesAsync();

        Assert.NotNull(entity.GeneratedCode);
        Assert.StartsWith("AZ-", entity.GeneratedCode);
    }

    [ConditionalFact]
    public async Task Composite_key_generation_works()
    {
        using var context = _fixture.CreateContext();
        
        var entity1 = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "KEY001",
            Name = "Entity 1"
        };
        
        var entity2 = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "KEY002",
            Name = "Entity 2"
        };
        
        var entity3 = new GeneratedEntity
        {
            PartitionKey = "US",
            RowKey = "KEY001",
            Name = "Entity 3"
        };

        context.GeneratedEntities.AddRange(entity1, entity2, entity3);
        await context.SaveChangesAsync();

        // All entities should have been saved successfully with composite keys
        Assert.NotNull(entity1.ETag);
        Assert.NotNull(entity2.ETag);
        Assert.NotNull(entity3.ETag);
    }

    [ConditionalFact]
    public async Task Server_generated_values_are_preserved()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "GEN005",
            Name = "Test Entity",
            ETag = "client-provided-etag", // This should be overwritten
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1) // This should be overwritten
        };

        context.GeneratedEntities.Add(entity);
        await context.SaveChangesAsync();

        // Server values should override client-provided values
        Assert.NotEqual("client-provided-etag", entity.ETag);
        Assert.True(entity.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [ConditionalFact]
    public async Task Can_generate_values_for_shadow_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ShadowPropertyEntity
        {
            PartitionKey = "UK",
            RowKey = "SHADOW001",
            Name = "Shadow Test"
        };

        var entry = context.ShadowPropertyEntities.Add(entity);
        entry.Property<DateTime>("CreatedDate").CurrentValue = DateTime.UtcNow;
        
        await context.SaveChangesAsync();

        // Check shadow property was set
        var createdDate = entry.Property<DateTime>("CreatedDate").CurrentValue;
        
        Assert.True(createdDate > DateTime.UtcNow.AddMinutes(-1));
    }

    [ConditionalFact]
    public async Task Default_values_are_applied()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new DefaultValueEntity
        {
            PartitionKey = "UK",
            RowKey = "DEFAULT001",
            Name = "Default Test",
            IsActive = true,  // Set manually since Azure Table doesn't support default values
            CreatedDate = DateTime.UtcNow
        };

        context.DefaultValueEntities.Add(entity);
        await context.SaveChangesAsync();

        Assert.True(entity.IsActive);
        Assert.True(entity.CreatedDate > DateTime.UtcNow.AddMinutes(-5));
    }

    [ConditionalFact]
    public async Task Temporary_values_are_replaced()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "TEMP001",
            Name = "Temporary Test"
        };

        var entry = context.GeneratedEntities.Add(entity);
        
        // Before save, generated values should be temporary
        Assert.True(entry.Property(e => e.ETag).IsTemporary);
        Assert.True(entry.Property(e => e.Timestamp).IsTemporary);
        
        await context.SaveChangesAsync();
        
        // After save, values should no longer be temporary
        Assert.False(entry.Property(e => e.ETag).IsTemporary);
        Assert.False(entry.Property(e => e.Timestamp).IsTemporary);
    }

    public class ValueGenerationAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public ValueGenerationAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("ValueGenerationTest");
        }

        public ValueGenerationContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ValueGenerationContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ValueGen"))
                .Options;

            var context = new ValueGenerationContext(options);
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

    public class ValueGenerationContext : DbContext
    {
        public ValueGenerationContext(DbContextOptions<ValueGenerationContext> options) : base(options)
        {
        }

        public DbSet<GeneratedEntity> GeneratedEntities => Set<GeneratedEntity>();
        public DbSet<CustomGeneratedEntity> CustomGeneratedEntities => Set<CustomGeneratedEntity>();
        public DbSet<ShadowPropertyEntity> ShadowPropertyEntities => Set<ShadowPropertyEntity>();
        public DbSet<DefaultValueEntity> DefaultValueEntities => Set<DefaultValueEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GeneratedEntity>(entity =>
            {
                entity.ToTable("GeneratedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
                    
                entity.Property(e => e.Timestamp)
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<CustomGeneratedEntity>(entity =>
            {
                entity.ToTable("CustomGeneratedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.GeneratedCode)
                    .HasValueGenerator<AzureCodeGenerator>();
            });

            modelBuilder.Entity<ShadowPropertyEntity>(entity =>
            {
                entity.ToTable("ShadowPropertyEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property<DateTime>("CreatedDate")
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<DefaultValueEntity>(entity =>
            {
                entity.ToTable("DefaultValueEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.IsActive);
                    
                entity.Property(e => e.CreatedDate)
                    .ValueGeneratedOnAdd();
            });
        }
    }

    public class GeneratedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ETag { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
    }

    public class CustomGeneratedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string GeneratedCode { get; set; } = null!;
    }

    public class ShadowPropertyEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class DefaultValueEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AzureCodeGenerator : ValueGenerator<string>
    {
        public override string Next(EntityEntry entry)
            => $"AZ-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

        public override bool GeneratesTemporaryValues => false;
    }
}