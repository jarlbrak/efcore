// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class OptimisticConcurrencyAzureTableTest : IClassFixture<OptimisticConcurrencyAzureTableTest.OptimisticConcurrencyAzureTableFixture>
{
    private readonly OptimisticConcurrencyAzureTableFixture _fixture;

    public OptimisticConcurrencyAzureTableTest(OptimisticConcurrencyAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Concurrency_violation_throws_on_update()
    {
        using var context1 = _fixture.CreateContext();
        using var context2 = _fixture.CreateContext();

        var entity = new ConcurrencyEntity
        {
            PartitionKey = "UK",
            RowKey = "CONCUR001",
            Name = "Initial",
            Value = 100
        };

        context1.ConcurrencyEntities.Add(entity);
        await context1.SaveChangesAsync();

        // Load entity in both contexts
        var entity1 = await context1.ConcurrencyEntities.FindAsync("UK", "CONCUR001");
        var entity2 = await context2.ConcurrencyEntities.FindAsync("UK", "CONCUR001");

        // Update in first context
        entity1.Name = "Updated by Context 1";
        await context1.SaveChangesAsync();

        // Try to update in second context - should fail
        entity2.Name = "Updated by Context 2";
        
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());
        
        Assert.Single(exception.Entries);
        Assert.Same(entity2, exception.Entries.First().Entity);
    }

    [ConditionalFact]
    public async Task Concurrency_violation_throws_on_delete()
    {
        using var context1 = _fixture.CreateContext();
        using var context2 = _fixture.CreateContext();

        var entity = new ConcurrencyEntity
        {
            PartitionKey = "UK",
            RowKey = "CONCUR002",
            Name = "To Delete",
            Value = 200
        };

        context1.ConcurrencyEntities.Add(entity);
        await context1.SaveChangesAsync();

        // Load entity in both contexts
        var entity1 = await context1.ConcurrencyEntities.FindAsync("UK", "CONCUR002");
        var entity2 = await context2.ConcurrencyEntities.FindAsync("UK", "CONCUR002");

        // Update in first context
        entity1.Name = "Updated before delete";
        await context1.SaveChangesAsync();

        // Try to delete in second context - should fail
        context2.ConcurrencyEntities.Remove(entity2);
        
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());
    }

    [ConditionalFact]
    public async Task Resolving_concurrency_conflict_with_database_values()
    {
        using var context = _fixture.CreateContext();

        var entity = new ConcurrencyEntity
        {
            PartitionKey = "UK",
            RowKey = "CONCUR003",
            Name = "Original",
            Value = 300
        };

        context.ConcurrencyEntities.Add(entity);
        await context.SaveChangesAsync();

        // Simulate concurrent update
        using (var context2 = _fixture.CreateContext())
        {
            var entity2 = await context2.ConcurrencyEntities.FindAsync("UK", "CONCUR003");
            entity2.Name = "Concurrent Update";
            entity2.Value = 400;
            await context2.SaveChangesAsync();
        }

        // Try to update with original context
        entity.Name = "My Update";
        entity.Value = 500;

        try
        {
            await context.SaveChangesAsync();
            Assert.Fail("Expected DbUpdateConcurrencyException");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.Single();

            // Get database values
            var databaseValues = await entry.GetDatabaseValuesAsync();
            
            Assert.Equal("Concurrent Update", databaseValues.GetValue<string>("Name"));
            Assert.Equal(400, databaseValues.GetValue<int>("Value"));

            // Resolve by using database values for ETag
            entry.OriginalValues.SetValues(databaseValues);
            
            // Retry save
            await context.SaveChangesAsync();
            
            Assert.Equal("My Update", entity.Name);
            Assert.Equal(500, entity.Value);
        }
    }

    [ConditionalFact]
    public async Task Resolving_concurrency_conflict_with_client_wins()
    {
        using var context = _fixture.CreateContext();

        var entity = new ConcurrencyEntity
        {
            PartitionKey = "UK",
            RowKey = "CONCUR004",
            Name = "Original",
            Value = 600
        };

        context.ConcurrencyEntities.Add(entity);
        await context.SaveChangesAsync();

        // Simulate concurrent update
        using (var context2 = _fixture.CreateContext())
        {
            var entity2 = await context2.ConcurrencyEntities.FindAsync("UK", "CONCUR004");
            entity2.Name = "Concurrent Update";
            await context2.SaveChangesAsync();
        }

        // Try to update with original context
        entity.Name = "Client Wins";

        try
        {
            await context.SaveChangesAsync();
            Assert.Fail("Expected DbUpdateConcurrencyException");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.Single();

            // Client wins - update original values with database values
            var databaseValues = await entry.GetDatabaseValuesAsync();
            entry.OriginalValues.SetValues(databaseValues);

            // Retry save
            await context.SaveChangesAsync();
            
            Assert.Equal("Client Wins", entity.Name);
        }
    }

    [ConditionalFact]
    public async Task Multiple_concurrency_tokens()
    {
        using var context = _fixture.CreateContext();

        var entity = new MultiTokenEntity
        {
            PartitionKey = "UK",
            RowKey = "MULTI001",
            Name = "Multi Token",
            Version = 1,
            RowVersion = new byte[] { 0, 0, 0, 1 }
        };

        context.MultiTokenEntities.Add(entity);
        await context.SaveChangesAsync();

        // Simulate concurrent update
        using (var context2 = _fixture.CreateContext())
        {
            var entity2 = await context2.MultiTokenEntities.FindAsync("UK", "MULTI001");
            entity2.Version = 2;
            await context2.SaveChangesAsync();
        }

        // Try to update with original context - should fail due to version mismatch
        entity.Name = "Updated";
        
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context.SaveChangesAsync());
    }

    [ConditionalFact]
    public async Task Shadow_concurrency_token()
    {
        using var context = _fixture.CreateContext();

        var entity = new ShadowConcurrencyEntity
        {
            PartitionKey = "UK",
            RowKey = "SHADOW001",
            Name = "Shadow Token"
        };

        var entry = context.ShadowConcurrencyEntities.Add(entity);
        entry.Property<int>("Version").CurrentValue = 1;
        
        await context.SaveChangesAsync();

        // Simulate concurrent update
        using (var context2 = _fixture.CreateContext())
        {
            var entity2 = await context2.ShadowConcurrencyEntities.FindAsync("UK", "SHADOW001");
            var entry2 = context2.Entry(entity2);
            entry2.Property<int>("Version").CurrentValue = 2;
            await context2.SaveChangesAsync();
        }

        // Try to update with original context
        entity.Name = "Updated Shadow";
        
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context.SaveChangesAsync());
    }

    [ConditionalFact]
    public async Task ETag_is_updated_on_successful_save()
    {
        using var context = _fixture.CreateContext();

        var entity = new ConcurrencyEntity
        {
            PartitionKey = "UK",
            RowKey = "ETAG001",
            Name = "ETag Test"
        };

        context.ConcurrencyEntities.Add(entity);
        await context.SaveChangesAsync();

        var originalETag = entity.ETag;
        Assert.NotNull(originalETag);

        entity.Name = "Updated ETag Test";
        await context.SaveChangesAsync();

        Assert.NotNull(entity.ETag);
        Assert.NotEqual(originalETag, entity.ETag);
    }

    [ConditionalFact]
    public async Task Timestamp_concurrency_tracking()
    {
        using var context = _fixture.CreateContext();

        var entity = new TimestampEntity
        {
            PartitionKey = "UK",
            RowKey = "TIME001",
            Name = "Timestamp Test"
        };

        context.TimestampEntities.Add(entity);
        await context.SaveChangesAsync();

        var originalTimestamp = entity.Timestamp;
        Assert.NotNull(originalTimestamp);

        // Small delay to ensure timestamp difference
        await Task.Delay(100);

        entity.Name = "Updated Timestamp Test";
        await context.SaveChangesAsync();

        Assert.NotNull(entity.Timestamp);
        Assert.True(entity.Timestamp > originalTimestamp);
    }

    [ConditionalFact]
    public async Task Batch_operations_with_concurrency()
    {
        using var context = _fixture.CreateContext();

        var entities = new List<ConcurrencyEntity>();
        for (int i = 0; i < 5; i++)
        {
            entities.Add(new ConcurrencyEntity
            {
                PartitionKey = "BATCH",
                RowKey = $"ITEM{i:D3}",
                Name = $"Item {i}",
                Value = i * 100
            });
        }

        context.ConcurrencyEntities.AddRange(entities);
        await context.SaveChangesAsync();

        // Simulate concurrent update of one entity
        using (var context2 = _fixture.CreateContext())
        {
            var entity2 = await context2.ConcurrencyEntities.FindAsync("BATCH", "ITEM002");
            entity2.Name = "Concurrent Update";
            await context2.SaveChangesAsync();
        }

        // Update all entities
        foreach (var entity in entities)
        {
            entity.Value += 50;
        }

        // SaveChanges should fail due to concurrency conflict on one entity
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context.SaveChangesAsync());
    }

    public class OptimisticConcurrencyAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public OptimisticConcurrencyAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("OptimisticConcurrencyTest");
        }

        public OptimisticConcurrencyContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<OptimisticConcurrencyContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("OptConcur"))
                .Options;

            var context = new OptimisticConcurrencyContext(options);
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

    public class OptimisticConcurrencyContext : DbContext
    {
        public OptimisticConcurrencyContext(DbContextOptions<OptimisticConcurrencyContext> options) : base(options)
        {
        }

        public DbSet<ConcurrencyEntity> ConcurrencyEntities => Set<ConcurrencyEntity>();
        public DbSet<MultiTokenEntity> MultiTokenEntities => Set<MultiTokenEntity>();
        public DbSet<ShadowConcurrencyEntity> ShadowConcurrencyEntities => Set<ShadowConcurrencyEntity>();
        public DbSet<TimestampEntity> TimestampEntities => Set<TimestampEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConcurrencyEntity>(entity =>
            {
                entity.ToAzureTable("ConcurrencyEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<MultiTokenEntity>(entity =>
            {
                entity.ToAzureTable("MultiTokenEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
                    
                entity.Property(e => e.Version)
                    .IsConcurrencyToken();
                    
                entity.Property(e => e.RowVersion)
                    .IsConcurrencyToken()
                    .HasConversion<string>(
                        v => Convert.ToBase64String(v),
                        v => Convert.FromBase64String(v));
            });

            modelBuilder.Entity<ShadowConcurrencyEntity>(entity =>
            {
                entity.ToAzureTable("ShadowConcurrencyEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property<int>("Version")
                    .IsConcurrencyToken();
                    
                entity.Property(e => e.ETag)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<TimestampEntity>(entity =>
            {
                entity.ToAzureTable("TimestampEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                entity.Property(e => e.Timestamp)
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate();
            });
        }
    }

    public class ConcurrencyEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public string ETag { get; set; } = null!;
    }

    public class MultiTokenEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ETag { get; set; } = null!;
        public int Version { get; set; }
        public byte[] RowVersion { get; set; } = null!;
    }

    public class ShadowConcurrencyEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ETag { get; set; } = null!;
    }

    public class TimestampEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
    }
}