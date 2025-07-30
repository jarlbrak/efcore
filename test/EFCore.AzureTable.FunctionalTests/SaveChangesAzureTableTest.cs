// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class SaveChangesAzureTableTest : IClassFixture<SaveChangesAzureTableTest.SaveChangesAzureTableFixture>
{
    private readonly SaveChangesAzureTableFixture _fixture;

    public SaveChangesAzureTableTest(SaveChangesAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task SaveChanges_inserts_new_entities()
    {
        using var context = _fixture.CreateContext();
        
        var entities = new[]
        {
            new SaveEntity { PartitionKey = "UK", RowKey = "SAVE001", Name = "Entity 1" },
            new SaveEntity { PartitionKey = "UK", RowKey = "SAVE002", Name = "Entity 2" },
            new SaveEntity { PartitionKey = "US", RowKey = "SAVE001", Name = "Entity 3" }
        };

        context.SaveEntities.AddRange(entities);
        var count = await context.SaveChangesAsync();
        
        Assert.Equal(3, count);
        Assert.All(entities, e => Assert.NotNull(e.ETag));
    }

    [ConditionalFact]
    public async Task SaveChanges_updates_existing_entities()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new SaveEntity { PartitionKey = "UK", RowKey = "UPDATE001", Name = "Original" };
        context.SaveEntities.Add(entity);
        await context.SaveChangesAsync();
        
        entity.Name = "Updated";
        entity.Value = 100;
        
        var count = await context.SaveChangesAsync();
        
        Assert.Equal(1, count);
    }

    [ConditionalFact]
    public async Task SaveChanges_deletes_entities()
    {
        using var context = _fixture.CreateContext();
        
        var entities = new[]
        {
            new SaveEntity { PartitionKey = "UK", RowKey = "DELETE001", Name = "To Delete 1" },
            new SaveEntity { PartitionKey = "UK", RowKey = "DELETE002", Name = "To Delete 2" }
        };

        context.SaveEntities.AddRange(entities);
        await context.SaveChangesAsync();
        
        context.SaveEntities.RemoveRange(entities);
        var count = await context.SaveChangesAsync();
        
        Assert.Equal(2, count);
    }

    [ConditionalFact]
    public async Task SaveChanges_handles_mixed_operations()
    {
        using var context = _fixture.CreateContext();
        
        // Add some initial entities
        var existing1 = new SaveEntity { PartitionKey = "UK", RowKey = "MIXED001", Name = "Existing 1" };
        var existing2 = new SaveEntity { PartitionKey = "UK", RowKey = "MIXED002", Name = "Existing 2" };
        
        context.SaveEntities.AddRange(existing1, existing2);
        await context.SaveChangesAsync();
        
        // Now perform mixed operations
        var newEntity = new SaveEntity { PartitionKey = "UK", RowKey = "MIXED003", Name = "New Entity" };
        context.SaveEntities.Add(newEntity); // Add
        
        existing1.Name = "Updated Existing 1"; // Update
        
        context.SaveEntities.Remove(existing2); // Delete
        
        var count = await context.SaveChangesAsync();
        
        Assert.Equal(3, count); // 1 insert + 1 update + 1 delete
    }

    [ConditionalFact]
    public async Task SaveChanges_returns_zero_when_no_changes()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new SaveEntity { PartitionKey = "UK", RowKey = "NOCHANGE001", Name = "No Changes" };
        context.SaveEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // No changes made
        var count = await context.SaveChangesAsync();
        
        Assert.Equal(0, count);
    }

    [ConditionalFact]
    public async Task SaveChanges_handles_concurrency_conflicts()
    {
        using var context1 = _fixture.CreateContext();
        using var context2 = _fixture.CreateContext();
        
        var entity = new SaveEntity { PartitionKey = "UK", RowKey = "CONCUR001", Name = "Concurrency Test" };
        context1.SaveEntities.Add(entity);
        await context1.SaveChangesAsync();
        
        // Load the same entity in both contexts
        var entity1 = await context1.SaveEntities.FindAsync("UK", "CONCUR001");
        var entity2 = await context2.SaveEntities.FindAsync("UK", "CONCUR001");
        
        // Modify in both contexts
        entity1.Name = "Context 1 Update";
        entity2.Name = "Context 2 Update";
        
        // First update should succeed
        await context1.SaveChangesAsync();
        
        // Second update should fail with concurrency exception
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
    }

    [ConditionalFact]
    public async Task SaveChanges_with_acceptAllChangesOnSuccess_false()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new SaveEntity { PartitionKey = "UK", RowKey = "ACCEPT001", Name = "Test" };
        var entry = context.SaveEntities.Add(entity);
        
        Assert.Equal(EntityState.Added, entry.State);
        
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);
        
        // State should still be Added since we didn't accept changes
        Assert.Equal(EntityState.Added, entry.State);
        
        // Manually accept changes
        context.ChangeTracker.AcceptAllChanges();
        Assert.Equal(EntityState.Unchanged, entry.State);
    }

    [ConditionalFact]
    public async Task SaveChanges_handles_large_batches()
    {
        using var context = _fixture.CreateContext();
        
        var entities = new List<SaveEntity>();
        for (int i = 0; i < 50; i++)
        {
            entities.Add(new SaveEntity
            {
                PartitionKey = "BATCH",
                RowKey = $"ITEM{i:D4}",
                Name = $"Batch Item {i}",
                Value = i
            });
        }

        context.SaveEntities.AddRange(entities);
        var count = await context.SaveChangesAsync();
        
        Assert.Equal(50, count);
        Assert.All(entities, e => Assert.NotNull(e.ETag));
    }

    [ConditionalFact]
    public async Task SaveChanges_respects_partition_boundaries()
    {
        using var context = _fixture.CreateContext();
        
        var entities = new[]
        {
            new SaveEntity { PartitionKey = "UK", RowKey = "PART001", Name = "UK Entity" },
            new SaveEntity { PartitionKey = "US", RowKey = "PART001", Name = "US Entity" },
            new SaveEntity { PartitionKey = "FR", RowKey = "PART001", Name = "FR Entity" }
        };

        context.SaveEntities.AddRange(entities);
        var count = await context.SaveChangesAsync();
        
        // Each partition should be handled separately
        Assert.Equal(3, count);
    }

    [ConditionalFact]
    public async Task SaveChanges_updates_generated_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new GeneratedEntity
        {
            PartitionKey = "UK",
            RowKey = "GEN001",
            Name = "Generated Test"
        };

        context.GeneratedEntities.Add(entity);
        
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.ModifiedDate);
        
        await context.SaveChangesAsync();
        
        Assert.NotNull(entity.CreatedDate);
        Assert.NotNull(entity.ModifiedDate);
        Assert.Equal(entity.CreatedDate, entity.ModifiedDate);
        
        // Update the entity
        entity.Name = "Updated";
        await context.SaveChangesAsync();
        
        Assert.NotEqual(entity.CreatedDate, entity.ModifiedDate);
    }

    [ConditionalFact]
    public async Task SaveChanges_handles_value_converters()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConvertedEntity
        {
            PartitionKey = "UK",
            RowKey = "CONV001",
            Name = "Converter Test",
            Status = EntityStatus.Active,
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };

        context.ConvertedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Verify the entity was saved with converted values
        var saved = await context.ConvertedEntities.FindAsync("UK", "CONV001");
        Assert.Equal(EntityStatus.Active, saved.Status);
        Assert.Equal(3, saved.Tags.Count);
    }

    [ConditionalFact]
    public async Task SaveChanges_rollback_on_error()
    {
        using var context = _fixture.CreateContext();
        
        var validEntity = new SaveEntity { PartitionKey = "UK", RowKey = "VALID001", Name = "Valid" };
        var invalidEntity = new SaveEntity { PartitionKey = null, RowKey = "INVALID001", Name = "Invalid" }; // Invalid - null partition key
        
        context.SaveEntities.AddRange(validEntity, invalidEntity);
        
        // SaveChanges should fail due to invalid entity
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        
        // No entities should have been saved
        Assert.Null(validEntity.ETag);
    }

    public class SaveChangesAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public SaveChangesAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("SaveChangesTest");
        }

        public SaveChangesContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SaveChangesContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("SaveChanges"))
                .Options;

            var context = new SaveChangesContext(options);
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

    public class SaveChangesContext : DbContext
    {
        public SaveChangesContext(DbContextOptions<SaveChangesContext> options) : base(options)
        {
        }

        public DbSet<SaveEntity> SaveEntities => Set<SaveEntity>();
        public DbSet<GeneratedEntity> GeneratedEntities => Set<GeneratedEntity>();
        public DbSet<ConvertedEntity> ConvertedEntities => Set<ConvertedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SaveEntity>(entity =>
            {
                entity.ToAzureTable("SaveEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<GeneratedEntity>(entity =>
            {
                entity.ToAzureTable("GeneratedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.CreatedDate)
                    .ValueGeneratedOnAdd();
                    
                entity.Property(e => e.ModifiedDate)
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<ConvertedEntity>(entity =>
            {
                entity.ToAzureTable("ConvertedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.Status)
                    .HasConversion<string>();
                    
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            });
        }
    }

    public class SaveEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public string ETag { get; set; } = null!;
    }

    public class GeneratedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class ConvertedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public EntityStatus Status { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public enum EntityStatus
    {
        Active,
        Inactive,
        Pending
    }
}