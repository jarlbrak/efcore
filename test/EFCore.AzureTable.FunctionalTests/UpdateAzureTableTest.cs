// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class UpdateAzureTableTest : IClassFixture<UpdateAzureTableTest.UpdateAzureTableFixture>
{
    private readonly UpdateAzureTableFixture _fixture;

    public UpdateAzureTableTest(UpdateAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Update_single_property()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new UpdateEntity
        {
            PartitionKey = "UK",
            RowKey = "UPDATE001",
            Name = "Original Name",
            Value = 100,
            IsActive = true
        };

        context.UpdateEntities.Add(entity);
        await context.SaveChangesAsync();
        
        entity.Name = "Updated Name";
        await context.SaveChangesAsync();
        
        var updated = await context.UpdateEntities.FindAsync("UK", "UPDATE001");
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(100, updated.Value); // Other properties unchanged
        Assert.True(updated.IsActive);
    }

    [ConditionalFact]
    public async Task Update_multiple_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new UpdateEntity
        {
            PartitionKey = "UK",
            RowKey = "UPDATE002",
            Name = "Original",
            Value = 50,
            IsActive = true,
            Description = "Original Description"
        };

        context.UpdateEntities.Add(entity);
        await context.SaveChangesAsync();
        
        entity.Name = "Updated";
        entity.Value = 200;
        entity.IsActive = false;
        entity.Description = "Updated Description";
        
        await context.SaveChangesAsync();
        
        var updated = await context.UpdateEntities.FindAsync("UK", "UPDATE002");
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(200, updated.Value);
        Assert.False(updated.IsActive);
        Assert.Equal("Updated Description", updated.Description);
    }

    [ConditionalFact]
    public async Task Update_with_null_values()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new UpdateEntity
        {
            PartitionKey = "UK",
            RowKey = "UPDATE003",
            Name = "Test",
            Description = "Has Description",
            OptionalValue = 123
        };

        context.UpdateEntities.Add(entity);
        await context.SaveChangesAsync();
        
        entity.Description = null;
        entity.OptionalValue = null;
        
        await context.SaveChangesAsync();
        
        var updated = await context.UpdateEntities.FindAsync("UK", "UPDATE003");
        Assert.Null(updated.Description);
        Assert.Null(updated.OptionalValue);
    }

    [ConditionalFact]
    public async Task Update_tracked_entity()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new UpdateEntity
        {
            PartitionKey = "UK",
            RowKey = "UPDATE004",
            Name = "Tracked",
            Value = 10
        };

        context.UpdateEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Entity is still tracked
        entity.Value = 20;
        var entry = context.Entry(entity);
        
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.Property(e => e.Value).IsModified);
        Assert.False(entry.Property(e => e.Name).IsModified);
        
        await context.SaveChangesAsync();
        Assert.Equal(20, entity.Value);
    }

    [ConditionalFact]
    public async Task Update_detached_entity()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new UpdateEntity
        {
            PartitionKey = "UK",
            RowKey = "UPDATE005",
            Name = "Detached",
            Value = 30
        };

        context.UpdateEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Detach the entity
        context.Entry(entity).State = EntityState.Detached;
        
        // Modify and reattach
        entity.Value = 40;
        context.Update(entity);
        
        Assert.Equal(EntityState.Modified, context.Entry(entity).State);
        
        await context.SaveChangesAsync();
        Assert.Equal(40, entity.Value);
    }

    [ConditionalFact]
    public async Task Update_with_concurrency_token()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConcurrentEntity
        {
            PartitionKey = "UK",
            RowKey = "CONCUR001",
            Name = "Concurrent",
            Version = 0
        };

        context.ConcurrentEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var originalETag = entity.ETag;
        var originalVersion = entity.Version;
        
        entity.Name = "Updated Concurrent";
        await context.SaveChangesAsync();
        
        Assert.NotEqual(originalETag, entity.ETag);
        Assert.Equal(originalVersion + 1, entity.Version);
    }

    [ConditionalFact]
    public async Task Update_batch_entities()
    {
        using var context = _fixture.CreateContext();
        
        var entities = new List<UpdateEntity>();
        for (int i = 0; i < 10; i++)
        {
            entities.Add(new UpdateEntity
            {
                PartitionKey = "BATCH",
                RowKey = $"ITEM{i:D3}",
                Name = $"Item {i}",
                Value = i
            });
        }

        context.UpdateEntities.AddRange(entities);
        await context.SaveChangesAsync();
        
        // Update all entities
        foreach (var entity in entities)
        {
            entity.Value *= 2;
            entity.IsActive = true;
        }
        
        var updateCount = await context.SaveChangesAsync();
        Assert.Equal(10, updateCount);
        
        // Verify updates
        for (int i = 0; i < 10; i++)
        {
            var updated = await context.UpdateEntities.FindAsync("BATCH", $"ITEM{i:D3}");
            Assert.Equal(i * 2, updated.Value);
            Assert.True(updated.IsActive);
        }
    }

    [ConditionalFact]
    public async Task Update_only_modified_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new UpdateEntity
        {
            PartitionKey = "UK",
            RowKey = "UPDATE006",
            Name = "Original",
            Value = 100,
            IsActive = true,
            Description = "Original Description"
        };

        context.UpdateEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Only modify one property
        entity.Value = 200;
        
        var entry = context.Entry(entity);
        Assert.True(entry.Property(e => e.Value).IsModified);
        Assert.False(entry.Property(e => e.Name).IsModified);
        Assert.False(entry.Property(e => e.IsActive).IsModified);
        Assert.False(entry.Property(e => e.Description).IsModified);
        
        await context.SaveChangesAsync();
    }

    [ConditionalFact]
    public async Task Update_with_computed_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ComputedEntity
        {
            PartitionKey = "UK",
            RowKey = "COMPUTED001",
            FirstName = "John",
            LastName = "Doe"
        };

        context.ComputedEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var originalModified = entity.LastModified;
        
        // Wait a bit to ensure timestamp difference
        await Task.Delay(100);
        
        entity.FirstName = "Jane";
        await context.SaveChangesAsync();
        
        Assert.Equal("Jane Doe", entity.FullName);
        Assert.True(entity.LastModified > originalModified);
    }

    [ConditionalFact]
    public async Task Update_preserves_unchanged_properties()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ComplexEntity
        {
            PartitionKey = "UK",
            RowKey = "COMPLEX001",
            Name = "Complex",
            IntValue = 42,
            DoubleValue = 3.14,
            BoolValue = true,
            DateValue = new DateTime(2024, 1, 1),
            GuidValue = Guid.NewGuid(),
            EnumValue = TestEnum.Option2
        };

        context.ComplexEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var originalGuid = entity.GuidValue;
        var originalDate = entity.DateValue;
        
        // Only update one property
        entity.Name = "Updated Complex";
        await context.SaveChangesAsync();
        
        var updated = await context.ComplexEntities.FindAsync("UK", "COMPLEX001");
        Assert.Equal("Updated Complex", updated.Name);
        Assert.Equal(42, updated.IntValue);
        Assert.Equal(3.14, updated.DoubleValue);
        Assert.True(updated.BoolValue);
        Assert.Equal(originalDate, updated.DateValue);
        Assert.Equal(originalGuid, updated.GuidValue);
        Assert.Equal(TestEnum.Option2, updated.EnumValue);
    }

    public class UpdateAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public UpdateAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("UpdateTest");
        }

        public UpdateContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<UpdateContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("Update"))
                .Options;

            var context = new UpdateContext(options);
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

    public class UpdateContext : DbContext
    {
        public UpdateContext(DbContextOptions<UpdateContext> options) : base(options)
        {
        }

        public DbSet<UpdateEntity> UpdateEntities => Set<UpdateEntity>();
        public DbSet<ConcurrentEntity> ConcurrentEntities => Set<ConcurrentEntity>();
        public DbSet<ComputedEntity> ComputedEntities => Set<ComputedEntity>();
        public DbSet<ComplexEntity> ComplexEntities => Set<ComplexEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UpdateEntity>(entity =>
            {
                entity.ToAzureTable("UpdateEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
            });

            modelBuilder.Entity<ConcurrentEntity>(entity =>
            {
                entity.ToAzureTable("ConcurrentEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
                    
                entity.Property(e => e.Version)
                    .IsConcurrencyToken();
            });

            modelBuilder.Entity<ComputedEntity>(entity =>
            {
                entity.ToAzureTable("ComputedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                // Azure Table doesn't support computed columns
                entity.Ignore(e => e.FullName);
                    
                entity.Property(e => e.LastModified)
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<ComplexEntity>(entity =>
            {
                entity.ToAzureTable("ComplexEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.EnumValue)
                    .HasConversion<string>();
            });
        }
    }

    public class UpdateEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public int? OptionalValue { get; set; }
    }

    public class ConcurrentEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ETag { get; set; } = null!;
        public int Version { get; set; }
    }

    public class ComputedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        public DateTime LastModified { get; set; }
    }

    public class ComplexEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        public DateTime DateValue { get; set; }
        public Guid GuidValue { get; set; }
        public TestEnum EnumValue { get; set; }
    }

    public enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }
}