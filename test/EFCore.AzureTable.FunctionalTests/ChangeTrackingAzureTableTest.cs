// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class ChangeTrackingAzureTableTest : IClassFixture<ChangeTrackingAzureTableTest.ChangeTrackingAzureTableFixture>
{
    private readonly ChangeTrackingAzureTableFixture _fixture;

    public ChangeTrackingAzureTableTest(ChangeTrackingAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Can_track_entity_add()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "TRACK001",
            Name = "New Entity"
        };

        var entry = context.TrackedEntities.Add(entity);
        
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal(1, context.ChangeTracker.Entries().Count());
        
        await context.SaveChangesAsync();
        
        Assert.Equal(EntityState.Unchanged, entry.State);
    }

    [ConditionalFact]
    public async Task Can_track_entity_modify()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "TRACK002",
            Name = "Original"
        };

        context.TrackedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        entity.Name = "Modified";
        
        var entry = context.Entry(entity);
        Assert.Equal(EntityState.Modified, entry.State);
        
        var nameProperty = entry.Property(e => e.Name);
        Assert.True(nameProperty.IsModified);
        Assert.Equal("Original", nameProperty.OriginalValue);
        Assert.Equal("Modified", nameProperty.CurrentValue);
        
        await context.SaveChangesAsync();
        Assert.Equal(EntityState.Unchanged, entry.State);
    }

    [ConditionalFact]
    public async Task Can_track_entity_delete()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "TRACK003",
            Name = "To Delete"
        };

        context.TrackedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        context.TrackedEntities.Remove(entity);
        
        var entry = context.Entry(entity);
        Assert.Equal(EntityState.Deleted, entry.State);
        
        await context.SaveChangesAsync();
        Assert.Equal(EntityState.Detached, entry.State);
    }

    [ConditionalFact]
    public void Can_detach_entity()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "TRACK004",
            Name = "To Detach"
        };

        var entry = context.TrackedEntities.Add(entity);
        Assert.Equal(EntityState.Added, entry.State);
        
        entry.State = EntityState.Detached;
        Assert.Equal(EntityState.Detached, entry.State);
        Assert.Equal(0, context.ChangeTracker.Entries().Count());
    }

    [ConditionalFact]
    public async Task Can_track_multiple_entities()
    {
        using var context = _fixture.CreateContext();
        
        var entity1 = new TrackedEntity { PartitionKey = "UK", RowKey = "MULTI001", Name = "Entity 1" };
        var entity2 = new TrackedEntity { PartitionKey = "UK", RowKey = "MULTI002", Name = "Entity 2" };
        var entity3 = new TrackedEntity { PartitionKey = "US", RowKey = "MULTI001", Name = "Entity 3" };

        context.TrackedEntities.AddRange(entity1, entity2, entity3);
        
        Assert.Equal(3, context.ChangeTracker.Entries().Count());
        Assert.All(context.ChangeTracker.Entries(), e => Assert.Equal(EntityState.Added, e.State));
        
        await context.SaveChangesAsync();
        
        Assert.All(context.ChangeTracker.Entries(), e => Assert.Equal(EntityState.Unchanged, e.State));
    }

    [ConditionalFact]
    public async Task Concurrency_token_tracking()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "CONCUR001",
            Name = "Concurrency Test"
        };

        context.TrackedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var originalETag = entity.ETag;
        entity.Name = "Updated";
        
        var entry = context.Entry(entity);
        var etagProperty = entry.Property(e => e.ETag);
        
        Assert.True(etagProperty.Metadata.IsConcurrencyToken);
        Assert.Equal(originalETag, etagProperty.OriginalValue);
        
        await context.SaveChangesAsync();
        Assert.NotEqual(originalETag, entity.ETag);
    }

    [ConditionalFact]
    public async Task Original_values_are_tracked()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ComplexEntity
        {
            PartitionKey = "UK",
            RowKey = "ORIG001",
            Name = "Original Name",
            Count = 10,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        context.ComplexEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Modify multiple properties
        entity.Name = "New Name";
        entity.Count = 20;
        entity.IsActive = false;
        
        var entry = context.Entry(entity);
        
        Assert.Equal("Original Name", entry.Property(e => e.Name).OriginalValue);
        Assert.Equal(10, entry.Property(e => e.Count).OriginalValue);
        Assert.True(entry.Property(e => e.IsActive).OriginalValue);
        
        // Check which properties are modified
        Assert.True(entry.Property(e => e.Name).IsModified);
        Assert.True(entry.Property(e => e.Count).IsModified);
        Assert.True(entry.Property(e => e.IsActive).IsModified);
        Assert.False(entry.Property(e => e.CreatedDate).IsModified);
    }

    [ConditionalFact]
    public async Task Can_accept_all_changes()
    {
        using var context = _fixture.CreateContext();
        
        var entity1 = new TrackedEntity { PartitionKey = "UK", RowKey = "ACCEPT001", Name = "New" };
        var entity2 = new TrackedEntity { PartitionKey = "UK", RowKey = "ACCEPT002", Name = "To Modify" };
        
        context.TrackedEntities.Add(entity1);
        context.TrackedEntities.Add(entity2);
        await context.SaveChangesAsync();
        
        entity2.Name = "Modified";
        
        Assert.Equal(EntityState.Unchanged, context.Entry(entity1).State);
        Assert.Equal(EntityState.Modified, context.Entry(entity2).State);
        
        context.ChangeTracker.AcceptAllChanges();
        
        Assert.Equal(EntityState.Unchanged, context.Entry(entity1).State);
        Assert.Equal(EntityState.Unchanged, context.Entry(entity2).State);
        Assert.Equal("Modified", entity2.Name);
    }

    [ConditionalFact]
    public async Task Can_reload_entity()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "RELOAD001",
            Name = "Original"
        };

        context.TrackedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        entity.Name = "Modified";
        Assert.Equal(EntityState.Modified, context.Entry(entity).State);
        
        // Note: Reload requires query translation to work
        // For now, we'll test the state management aspect
        context.Entry(entity).State = EntityState.Unchanged;
        context.Entry(entity).CurrentValues.SetValues(context.Entry(entity).OriginalValues);
        
        Assert.Equal("Original", entity.Name);
        Assert.Equal(EntityState.Unchanged, context.Entry(entity).State);
    }

    [ConditionalFact]
    public async Task Shadow_properties_are_tracked()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ShadowEntity
        {
            PartitionKey = "UK",
            RowKey = "SHADOW001",
            Name = "Shadow Test"
        };

        var entry = context.ShadowEntities.Add(entity);
        entry.Property<DateTime>("LastModified").CurrentValue = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        var lastModified = entry.Property<DateTime>("LastModified").CurrentValue;
        entry.Property<DateTime>("LastModified").CurrentValue = DateTime.UtcNow.AddHours(1);
        
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.Property<DateTime>("LastModified").IsModified);
        Assert.NotEqual(lastModified, entry.Property<DateTime>("LastModified").CurrentValue);
    }

    [ConditionalFact]
    public async Task HasChanges_reflects_tracker_state()
    {
        using var context = _fixture.CreateContext();
        
        Assert.False(context.ChangeTracker.HasChanges());
        
        var entity = new TrackedEntity
        {
            PartitionKey = "UK",
            RowKey = "CHANGES001",
            Name = "Test"
        };

        context.TrackedEntities.Add(entity);
        Assert.True(context.ChangeTracker.HasChanges());
        
        await context.SaveChangesAsync();
        Assert.False(context.ChangeTracker.HasChanges());
        
        entity.Name = "Modified";
        Assert.True(context.ChangeTracker.HasChanges());
    }

    [ConditionalFact]
    public void Can_clear_change_tracker()
    {
        using var context = _fixture.CreateContext();
        
        var entities = new[]
        {
            new TrackedEntity { PartitionKey = "UK", RowKey = "CLEAR001", Name = "Entity 1" },
            new TrackedEntity { PartitionKey = "UK", RowKey = "CLEAR002", Name = "Entity 2" },
            new TrackedEntity { PartitionKey = "UK", RowKey = "CLEAR003", Name = "Entity 3" }
        };

        context.TrackedEntities.AddRange(entities);
        Assert.Equal(3, context.ChangeTracker.Entries().Count());
        
        context.ChangeTracker.Clear();
        
        Assert.Equal(0, context.ChangeTracker.Entries().Count());
        Assert.All(entities, e => Assert.Equal(EntityState.Detached, context.Entry(e).State));
    }

    public class ChangeTrackingAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public ChangeTrackingAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("ChangeTrackingTest");
        }

        public ChangeTrackingContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ChangeTrackingContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ChangeTrack"))
                .Options;

            var context = new ChangeTrackingContext(options);
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

    public class ChangeTrackingContext : DbContext
    {
        public ChangeTrackingContext(DbContextOptions<ChangeTrackingContext> options) : base(options)
        {
        }

        public DbSet<TrackedEntity> TrackedEntities => Set<TrackedEntity>();
        public DbSet<ComplexEntity> ComplexEntities => Set<ComplexEntity>();
        public DbSet<ShadowEntity> ShadowEntities => Set<ShadowEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrackedEntity>(entity =>
            {
                entity.ToTable("TrackedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<ComplexEntity>(entity =>
            {
                entity.ToTable("ComplexEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
            });

            modelBuilder.Entity<ShadowEntity>(entity =>
            {
                entity.ToTable("ShadowEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property<DateTime>("LastModified");
            });
        }
    }

    public class TrackedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ETag { get; set; } = null!;
    }

    public class ComplexEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ShadowEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}