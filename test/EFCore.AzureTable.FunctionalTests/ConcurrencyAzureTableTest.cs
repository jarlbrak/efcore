// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class ConcurrencyAzureTableTest : IClassFixture<ConcurrencyAzureTableTest.ConcurrencyAzureTableFixture>
{
    private readonly ConcurrencyAzureTableFixture _fixture;

    public ConcurrencyAzureTableTest(ConcurrencyAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Can_handle_optimistic_concurrency_with_etag()
    {
        // Arrange
        using var context1 = _fixture.CreateContext();
        using var context2 = _fixture.CreateContext();

        var entity = new ConcurrencyTestEntity
        {
            PartitionKey = "Concurrency",
            RowKey = "Test001",
            Name = "Original Name",
            Value = 100
        };

        context1.ConcurrencyEntities.Add(entity);
        await context1.SaveChangesAsync();

        // Act - Load the same entity in two different contexts
        var entity1 = await context1.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "Concurrency" && e.RowKey == "Test001");
        var entity2 = await context2.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "Concurrency" && e.RowKey == "Test001");

        // Modify in both contexts
        entity1.Name = "Updated by Context 1";
        entity2.Name = "Updated by Context 2";

        // First update should succeed
        await context1.SaveChangesAsync();

        // Second update should fail with concurrency exception
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
        Assert.NotNull(exception);
    }

    [ConditionalFact]
    public async Task Can_resolve_concurrency_conflict()
    {
        // Arrange
        using var context1 = _fixture.CreateContext();
        using var context2 = _fixture.CreateContext();

        var entity = new ConcurrencyTestEntity
        {
            PartitionKey = "Concurrency",
            RowKey = "Test002",
            Name = "Original Name",
            Value = 200
        };

        context1.ConcurrencyEntities.Add(entity);
        await context1.SaveChangesAsync();

        // Act - Create conflict scenario
        var entity1 = await context1.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "Concurrency" && e.RowKey == "Test002");
        var entity2 = await context2.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "Concurrency" && e.RowKey == "Test002");

        entity1.Value = 300;
        entity2.Name = "Updated Name";

        await context1.SaveChangesAsync();

        try
        {
            await context2.SaveChangesAsync();
            Assert.Fail("Expected concurrency exception");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Resolve conflict by reloading and reapplying changes
            var entry = ex.Entries.Single();
            await entry.ReloadAsync();
            
            // Reapply our change after reload
            ((ConcurrencyTestEntity)entry.Entity).Name = "Updated Name";
            await context2.SaveChangesAsync();
        }

        // Assert - Verify final state has both changes
        using var verifyContext = _fixture.CreateContext();
        var finalEntity = await verifyContext.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "Concurrency" && e.RowKey == "Test002");

        Assert.Equal("Updated Name", finalEntity.Name);
        Assert.Equal(300, finalEntity.Value);
    }

    [ConditionalFact]
    public async Task Can_track_changes_for_updates()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var entity = new ConcurrencyTestEntity
        {
            PartitionKey = "ChangeTracking",
            RowKey = "Test001",
            Name = "Original Name",
            Value = 150
        };

        context.ConcurrencyEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act - Load and modify entity
        var trackedEntity = await context.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "ChangeTracking" && e.RowKey == "Test001");

        Assert.Equal(EntityState.Unchanged, context.Entry(trackedEntity).State);

        trackedEntity.Name = "Modified Name";
        Assert.Equal(EntityState.Modified, context.Entry(trackedEntity).State);

        await context.SaveChangesAsync();
        Assert.Equal(EntityState.Unchanged, context.Entry(trackedEntity).State);

        // Assert - Verify changes persisted
        using var verifyContext = _fixture.CreateContext();
        var persistedEntity = await verifyContext.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "ChangeTracking" && e.RowKey == "Test001");

        Assert.Equal("Modified Name", persistedEntity.Name);
        Assert.Equal(150, persistedEntity.Value);
    }

    [ConditionalFact]
    public async Task Can_detect_unchanged_entities()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var entity = new ConcurrencyTestEntity
        {
            PartitionKey = "ChangeTracking",
            RowKey = "Test002",
            Name = "Unchanged Name",
            Value = 250
        };

        context.ConcurrencyEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act - Load entity without changes
        context.ChangeTracker.Clear();
        var trackedEntity = await context.ConcurrencyEntities
            .FirstAsync(e => e.PartitionKey == "ChangeTracking" && e.RowKey == "Test002");

        Assert.Equal(EntityState.Unchanged, context.Entry(trackedEntity).State);

        // No changes made
        var changeCount = await context.SaveChangesAsync();

        // Assert - No changes should be saved
        Assert.Equal(0, changeCount);
        Assert.Equal(EntityState.Unchanged, context.Entry(trackedEntity).State);
    }

    [ConditionalFact]
    public async Task Can_handle_add_and_delete_operations()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var entityToAdd = new ConcurrencyTestEntity
        {
            PartitionKey = "Operations",
            RowKey = "Add001",
            Name = "New Entity",
            Value = 500
        };

        var entityToDelete = new ConcurrencyTestEntity
        {
            PartitionKey = "Operations",
            RowKey = "Delete001",
            Name = "Entity to Delete",
            Value = 600
        };

        // First add entity that will be deleted
        context.ConcurrencyEntities.Add(entityToDelete);
        await context.SaveChangesAsync();

        // Act - Add new entity and delete existing one
        context.ConcurrencyEntities.Add(entityToAdd);
        context.ConcurrencyEntities.Remove(entityToDelete);

        Assert.Equal(EntityState.Added, context.Entry(entityToAdd).State);
        Assert.Equal(EntityState.Deleted, context.Entry(entityToDelete).State);

        var changeCount = await context.SaveChangesAsync();

        // Assert - Both operations should succeed
        Assert.Equal(2, changeCount); // One add, one delete
        Assert.Equal(EntityState.Unchanged, context.Entry(entityToAdd).State);
        Assert.Equal(EntityState.Detached, context.Entry(entityToDelete).State);

        // Verify in fresh context
        using var verifyContext = _fixture.CreateContext();
        var addedEntity = await verifyContext.ConcurrencyEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Operations" && e.RowKey == "Add001");
        var deletedEntity = await verifyContext.ConcurrencyEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Operations" && e.RowKey == "Delete001");

        Assert.NotNull(addedEntity);
        Assert.Null(deletedEntity);
    }

    [ConditionalFact]
    public async Task ETag_values_are_updated_after_save()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var entity = new ConcurrencyTestEntity
        {
            PartitionKey = "ETag",
            RowKey = "Test001",
            Name = "ETag Test",
            Value = 300
        };

        // Act - Add entity and verify ETag is set
        context.ConcurrencyEntities.Add(entity);
        Assert.Null(entity.ETag); // Should be null before save

        await context.SaveChangesAsync();
        Assert.NotNull(entity.ETag); // Should be set after save

        var originalETag = entity.ETag;

        // Modify and save again
        entity.Name = "Updated ETag Test";
        await context.SaveChangesAsync();

        // Assert - ETag should be updated
        Assert.NotNull(entity.ETag);
        Assert.NotEqual(originalETag, entity.ETag);
    }

    public class ConcurrencyAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public ConcurrencyAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("ConcurrencyTest");
        }

        public ConcurrencyTestContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ConcurrencyTestContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ConcurrencyTest"))
                .Options;

            var context = new ConcurrencyTestContext(options);
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

    public class ConcurrencyTestContext : DbContext
    {
        public ConcurrencyTestContext(DbContextOptions<ConcurrencyTestContext> options) : base(options)
        {
        }

        public DbSet<ConcurrencyTestEntity> ConcurrencyEntities => Set<ConcurrencyTestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConcurrencyTestEntity>(entity =>
            {
                entity.ToTable("ConcurrencyTestEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                entity.Property(e => e.ETag).IsConcurrencyToken();
                entity.Property(e => e.Timestamp).IsTimestamp();
            });
        }
    }

    public class ConcurrencyTestEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public string ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}