// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class FindAzureTableTest : IClassFixture<FindAzureTableTest.FindAzureTableFixture>
{
    private readonly FindAzureTableFixture _fixture;

    public FindAzureTableTest(FindAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Find_with_partition_and_row_key()
    {
        using var context = _fixture.CreateContext();
        
        var entity = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        
        Assert.NotNull(entity);
        Assert.Equal("UK", entity.Country);
        Assert.Equal("CUSTOMER001", entity.CustomerId);
        Assert.Equal("Test Company", entity.CompanyName);
    }

    [ConditionalFact]
    public async Task Find_with_null_keys_returns_null()
    {
        using var context = _fixture.CreateContext();
        
        var entity = await context.FindEntities.FindAsync(null, "CUSTOMER001");
        
        Assert.Null(entity);
    }

    [ConditionalFact]
    public async Task Find_with_non_existing_keys_returns_null()
    {
        using var context = _fixture.CreateContext();
        
        var entity = await context.FindEntities.FindAsync("NonExistent", "NonExistent");
        
        Assert.Null(entity);
    }

    [ConditionalFact]
    public async Task Find_tracked_entity_returns_same_instance()
    {
        using var context = _fixture.CreateContext();
        
        // First find - loads from database
        var entity1 = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        
        // Second find - should return tracked instance
        var entity2 = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        
        Assert.NotNull(entity1);
        Assert.NotNull(entity2);
        Assert.Same(entity1, entity2);
    }

    [ConditionalFact]
    public async Task FindAsync_with_cancellation_token()
    {
        using var context = _fixture.CreateContext();
        using var cts = new CancellationTokenSource();
        
        var entity = await context.FindEntities.FindAsync(new object[] { "UK", "CUSTOMER001" }, cts.Token);
        
        Assert.NotNull(entity);
        Assert.Equal("Test Company", entity.CompanyName);
    }

    [ConditionalFact]
    public async Task Find_with_object_array_keys()
    {
        using var context = _fixture.CreateContext();
        
        var keys = new object[] { "UK", "CUSTOMER002" };
        var entity = await context.FindEntities.FindAsync(keys);
        
        Assert.NotNull(entity);
        Assert.Equal("UK", entity.Country);
        Assert.Equal("CUSTOMER002", entity.CustomerId);
    }

    [ConditionalFact]
    public async Task Find_with_wrong_number_of_keys_throws()
    {
        using var context = _fixture.CreateContext();
        
        // Azure Table requires both partition key and row key
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await context.FindEntities.FindAsync("UK");
        });
    }

    [ConditionalFact]
    public async Task Find_with_wrong_key_type_throws()
    {
        using var context = _fixture.CreateContext();
        
        // Both keys should be strings for Azure Table
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await context.FindEntities.FindAsync(123, "CUSTOMER001");
        });
    }

    [ConditionalFact]
    public async Task Find_multiple_entities_with_different_keys()
    {
        using var context = _fixture.CreateContext();
        
        var entity1 = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        var entity2 = await context.FindEntities.FindAsync("USA", "CUSTOMER003");
        
        Assert.NotNull(entity1);
        Assert.NotNull(entity2);
        Assert.Equal("UK", entity1.Country);
        Assert.Equal("USA", entity2.Country);
        Assert.NotSame(entity1, entity2);
    }

    [ConditionalFact]
    public async Task Find_after_modification_returns_modified_entity()
    {
        using var context = _fixture.CreateContext();
        
        var entity = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        Assert.NotNull(entity);
        
        // Modify the entity
        entity.CompanyName = "Modified Company";
        
        // Find again should return the modified tracked entity
        var foundAgain = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        
        Assert.Same(entity, foundAgain);
        Assert.Equal("Modified Company", foundAgain.CompanyName);
    }

    [ConditionalFact]
    public async Task Find_deleted_entity_returns_null()
    {
        using var context = _fixture.CreateContext();
        
        var entity = await context.FindEntities.FindAsync("UK", "CUSTOMER002");
        Assert.NotNull(entity);
        
        // Delete the entity
        context.FindEntities.Remove(entity);
        
        // Find again should return null for deleted entity
        var foundAgain = await context.FindEntities.FindAsync("UK", "CUSTOMER002");
        Assert.Null(foundAgain);
    }

    [ConditionalFact]
    public async Task Find_added_entity_returns_added_entity()
    {
        using var context = _fixture.CreateContext();
        
        var newEntity = new FindEntity
        {
            Country = "Germany",
            CustomerId = "CUSTOMER004",
            CompanyName = "New German Company",
            City = "Berlin"
        };
        
        context.FindEntities.Add(newEntity);
        
        // Find should return the added entity before saving
        var found = await context.FindEntities.FindAsync("Germany", "CUSTOMER004");
        
        Assert.NotNull(found);
        Assert.Same(newEntity, found);
        Assert.Equal("New German Company", found.CompanyName);
    }

    [ConditionalFact]
    public async Task Find_works_with_different_entity_types()
    {
        using var context = _fixture.CreateContext();
        
        var findEntity = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        var orderEntity = await context.Orders.FindAsync("UK", "ORDER001");
        
        Assert.NotNull(findEntity);
        Assert.NotNull(orderEntity);
        Assert.Equal("Test Company", findEntity.CompanyName);
        Assert.Equal("CUSTOMER001", orderEntity.CustomerId);
    }

    [ConditionalFact]
    public async Task Find_with_guid_keys()
    {
        using var context = _fixture.CreateContext();
        
        var guidKey = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var entity = await context.GuidKeyEntities.FindAsync("Test", guidKey.ToString());
        
        Assert.NotNull(entity);
        Assert.Equal("Test", entity.PartitionKey);
        Assert.Equal(guidKey.ToString(), entity.Id);
    }

    [ConditionalFact]
    public async Task Find_respects_query_filters()
    {
        using var context = _fixture.CreateContext();
        
        // This test assumes there might be query filters configured
        // In this simple case, it should work normally
        var entity = await context.FindEntities.FindAsync("UK", "CUSTOMER001");
        
        Assert.NotNull(entity);
        Assert.True(entity.IsActive); // Assuming query filter only returns active entities
    }

    public class FindAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public FindAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("FindTest");
            SeedData();
        }

        public FindContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<FindContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("FindTest"))
                .Options;

            var context = new FindContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var findEntities = new[]
            {
                new FindEntity { Country = "UK", CustomerId = "CUSTOMER001", CompanyName = "Test Company", City = "London", IsActive = true },
                new FindEntity { Country = "UK", CustomerId = "CUSTOMER002", CompanyName = "Another Company", City = "Bristol", IsActive = true },
                new FindEntity { Country = "USA", CustomerId = "CUSTOMER003", CompanyName = "American Corp", City = "New York", IsActive = true },
                new FindEntity { Country = "Germany", CustomerId = "CUSTOMER005", CompanyName = "Inactive Company", City = "Munich", IsActive = false }
            };

            var orders = new[]
            {
                new Order { Country = "UK", OrderId = "ORDER001", CustomerId = "CUSTOMER001", OrderDate = DateTime.UtcNow, Amount = 100.50m },
                new Order { Country = "USA", OrderId = "ORDER002", CustomerId = "CUSTOMER003", OrderDate = DateTime.UtcNow.AddDays(-1), Amount = 250.75m }
            };

            var guidKeyEntities = new[]
            {
                new GuidKeyEntity { PartitionKey = "Test", Id = Guid.Parse("12345678-1234-1234-1234-123456789012").ToString(), Name = "Guid Entity 1" },
                new GuidKeyEntity { PartitionKey = "Test", Id = Guid.Parse("87654321-8765-4321-8765-432187654321").ToString(), Name = "Guid Entity 2" }
            };

            context.FindEntities.AddRange(findEntities);
            context.Orders.AddRange(orders);
            context.GuidKeyEntities.AddRange(guidKeyEntities);
            context.SaveChanges();
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

    public class FindContext : DbContext
    {
        public FindContext(DbContextOptions<FindContext> options) : base(options)
        {
        }

        public DbSet<FindEntity> FindEntities => Set<FindEntity>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<GuidKeyEntity> GuidKeyEntities => Set<GuidKeyEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FindEntity>(entity =>
            {
                entity.ToAzureTable("FindEntities");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.CustomerId);
                
                // Example query filter - only return active entities
                entity.HasQueryFilter(e => e.IsActive);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToAzureTable("Orders");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.OrderId);
            });

            modelBuilder.Entity<GuidKeyEntity>(entity =>
            {
                entity.ToAzureTable("GuidKeyEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class FindEntity
    {
        public string Country { get; set; } = null!;
        public string CustomerId { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string City { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class Order
    {
        public string Country { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public string CustomerId { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
    }

    public class GuidKeyEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}