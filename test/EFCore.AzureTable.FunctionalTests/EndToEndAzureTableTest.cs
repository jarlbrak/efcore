// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

public class EndToEndAzureTableTest : IClassFixture<EndToEndAzureTableTest.EndToEndFixture>
{
    private readonly EndToEndFixture _fixture;

    public EndToEndAzureTableTest(EndToEndFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Can_insert_and_query_entity()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new Customer
        {
            Region = "West",
            Id = $"CUST{Guid.NewGuid():N}",
            Name = "Test Customer",
            Email = "test@example.com"
        };

        // Act
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Assert
        var retrievedCustomer = await context.Customers
            .FirstOrDefaultAsync(c => c.Region == "West" && c.Id == customer.Id);

        Assert.NotNull(retrievedCustomer);
        Assert.Equal("Test Customer", retrievedCustomer.Name);
        Assert.Equal("test@example.com", retrievedCustomer.Email);
    }

    [ConditionalFact]
    public async Task Can_update_entity()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new Customer
        {
            Region = "East",
            Id = "CUST002",
            Name = "Original Name",
            Email = "original@example.com"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act
        customer.Name = "Updated Name";
        customer.Email = "updated@example.com";
        await context.SaveChangesAsync();

        // Assert
        var retrievedCustomer = await context.Customers
            .FirstOrDefaultAsync(c => c.Region == "East" && c.Id == "CUST002");

        Assert.NotNull(retrievedCustomer);
        Assert.Equal("Updated Name", retrievedCustomer.Name);
        Assert.Equal("updated@example.com", retrievedCustomer.Email);
    }

    [ConditionalFact]
    public async Task Can_delete_entity()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new Customer
        {
            Region = "North",
            Id = "CUST003",
            Name = "To Be Deleted",
            Email = "delete@example.com"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act
        context.Customers.Remove(customer);
        await context.SaveChangesAsync();

        // Assert
        var retrievedCustomer = await context.Customers
            .FirstOrDefaultAsync(c => c.Region == "North" && c.Id == "CUST003");

        Assert.Null(retrievedCustomer);
    }

    [ConditionalFact]
    public async Task Can_query_by_partition_key()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customers = new[]
        {
            new Customer { Region = "South", Id = "CUST004", Name = "Customer 1", Email = "c1@example.com" },
            new Customer { Region = "South", Id = "CUST005", Name = "Customer 2", Email = "c2@example.com" },
            new Customer { Region = "West", Id = "CUST006", Name = "Customer 3", Email = "c3@example.com" }
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Act
        var southCustomers = await context.Customers
            .Where(c => c.Region == "South")
            .ToListAsync();

        // Assert
        Assert.Equal(2, southCustomers.Count);
        Assert.All(southCustomers, c => Assert.Equal("South", c.Region));
    }

    [ConditionalFact]
    public async Task Can_handle_complex_types()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new Customer
        {
            Region = "Central",
            Id = "CUST007",
            Name = "Complex Customer",
            Email = "complex@example.com",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Anytown",
                State = "ST",
                ZipCode = "12345"
            }
        };

        // Act
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Assert
        var retrievedCustomer = await context.Customers
            .FirstOrDefaultAsync(c => c.Region == "Central" && c.Id == "CUST007");

        Assert.NotNull(retrievedCustomer);
        Assert.NotNull(retrievedCustomer.Address);
        Assert.Equal("123 Main St", retrievedCustomer.Address.Street);
        Assert.Equal("Anytown", retrievedCustomer.Address.City);
    }

    [ConditionalFact]
    public async Task Can_handle_batch_operations_within_partition()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customers = Enumerable.Range(1, 5).Select(i => new Customer
        {
            Region = "Batch",
            Id = $"BATCH{i:D3}",
            Name = $"Batch Customer {i}",
            Email = $"batch{i}@example.com"
        }).ToArray();

        // Act
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Assert
        var batchCustomers = await context.Customers
            .Where(c => c.Region == "Batch")
            .ToListAsync();

        Assert.Equal(5, batchCustomers.Count);
        Assert.All(batchCustomers, c => Assert.StartsWith("Batch Customer", c.Name));
    }

    [ConditionalFact]
    public async Task Can_add_update_delete_with_collection_properties()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new CustomerWithCollections
        {
            Region = "Collections",
            Id = "COLL001",
            Name = "Collection Customer",
            Tags = new List<string> { "VIP", "Premium", "Local" },
            Scores = new[] { 95, 87, 91 },
            Metadata = new Dictionary<string, string>
            {
                { "Source", "Web" },
                { "Campaign", "Summer2024" }
            }
        };

        // Act - Add
        context.Set<CustomerWithCollections>().Add(customer);
        await context.SaveChangesAsync();

        // Assert - Query
        var retrievedCustomer = await context.Set<CustomerWithCollections>()
            .FirstOrDefaultAsync(c => c.Region == "Collections" && c.Id == "COLL001");

        Assert.NotNull(retrievedCustomer);
        Assert.NotNull(retrievedCustomer.Tags);
        Assert.Equal(3, retrievedCustomer.Tags.Count);
        Assert.Contains("VIP", retrievedCustomer.Tags);
        Assert.NotNull(retrievedCustomer.Scores);
        Assert.Equal(3, retrievedCustomer.Scores.Length);
        Assert.Equal(95, retrievedCustomer.Scores[0]);
        Assert.NotNull(retrievedCustomer.Metadata);
        Assert.Equal(2, retrievedCustomer.Metadata.Count);
        Assert.Equal("Web", retrievedCustomer.Metadata["Source"]);

        // Act - Update
        retrievedCustomer.Tags.Add("Loyalty");
        retrievedCustomer.Scores = new[] { 100, 92, 88, 96 };
        retrievedCustomer.Metadata["Status"] = "Active";
        await context.SaveChangesAsync();

        // Assert - Verify Update
        context.ChangeTracker.Clear();
        var updatedCustomer = await context.Set<CustomerWithCollections>()
            .FirstOrDefaultAsync(c => c.Region == "Collections" && c.Id == "COLL001");

        Assert.NotNull(updatedCustomer);
        Assert.NotNull(updatedCustomer.Tags);
        Assert.Equal(4, updatedCustomer.Tags.Count);
        Assert.Contains("Loyalty", updatedCustomer.Tags);
        Assert.NotNull(updatedCustomer.Scores);
        Assert.Equal(4, updatedCustomer.Scores.Length);
        Assert.Equal(100, updatedCustomer.Scores[0]);
        Assert.NotNull(updatedCustomer.Metadata);
        Assert.Equal(3, updatedCustomer.Metadata.Count);
        Assert.Equal("Active", updatedCustomer.Metadata["Status"]);

        // Act - Delete
        context.Set<CustomerWithCollections>().Remove(updatedCustomer);
        await context.SaveChangesAsync();

        // Assert - Verify Deletion
        var deletedCustomer = await context.Set<CustomerWithCollections>()
            .FirstOrDefaultAsync(c => c.Region == "Collections" && c.Id == "COLL001");
        Assert.Null(deletedCustomer);
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for deeply nested complex objects")]
    public async Task Can_handle_nested_collections()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new CustomerWithNestedCollections
        {
            Region = "Nested",
            Id = "NEST001",
            Name = "Nested Collection Customer",
            OrdersByYear = new Dictionary<string, List<string>>
            {
                { "2023", new List<string> { "ORD-001", "ORD-002" } },
                { "2024", new List<string> { "ORD-003", "ORD-004", "ORD-005" } }
            },
            PreferencesByCategory = new Dictionary<string, Dictionary<string, object>>
            {
                { 
                    "Shipping", 
                    new Dictionary<string, object> 
                    { 
                        { "Method", "Express" }, 
                        { "Insurance", true } 
                    } 
                },
                { 
                    "Communication", 
                    new Dictionary<string, object> 
                    { 
                        { "Email", true }, 
                        { "SMS", false }, 
                        { "Frequency", "Weekly" } 
                    } 
                }
            }
        };

        // Act
        context.Set<CustomerWithNestedCollections>().Add(customer);
        await context.SaveChangesAsync();

        // Assert
        var retrievedCustomer = await context.Set<CustomerWithNestedCollections>()
            .FirstOrDefaultAsync(c => c.Region == "Nested" && c.Id == "NEST001");

        Assert.NotNull(retrievedCustomer);
        Assert.NotNull(retrievedCustomer.OrdersByYear);
        Assert.Equal(2, retrievedCustomer.OrdersByYear.Count);
        Assert.Equal(3, retrievedCustomer.OrdersByYear["2024"].Count);
        Assert.Contains("ORD-003", retrievedCustomer.OrdersByYear["2024"]);
        
        Assert.NotNull(retrievedCustomer.PreferencesByCategory);
        Assert.Equal(2, retrievedCustomer.PreferencesByCategory.Count);
        var shippingPrefs = retrievedCustomer.PreferencesByCategory["Shipping"];
        Assert.Equal("Express", shippingPrefs["Method"]);
        Assert.Equal(true, shippingPrefs["Insurance"]);
    }

    [ConditionalFact]
    public async Task Can_handle_null_collections()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var customer = new CustomerWithCollections
        {
            Region = "NullCollections",
            Id = "NULL001",
            Name = "Null Collection Customer",
            Tags = null,
            Scores = null,
            Metadata = null
        };

        // Act
        context.Set<CustomerWithCollections>().Add(customer);
        await context.SaveChangesAsync();

        // Assert
        var retrievedCustomer = await context.Set<CustomerWithCollections>()
            .FirstOrDefaultAsync(c => c.Region == "NullCollections" && c.Id == "NULL001");

        Assert.NotNull(retrievedCustomer);
        Assert.Null(retrievedCustomer.Tags);
        Assert.Null(retrievedCustomer.Scores);
        Assert.Null(retrievedCustomer.Metadata);

        // Act - Update with values
        retrievedCustomer.Tags = new List<string> { "New" };
        retrievedCustomer.Scores = new[] { 50 };
        retrievedCustomer.Metadata = new Dictionary<string, string> { { "Key", "Value" } };
        await context.SaveChangesAsync();

        // Assert - Verify update
        context.ChangeTracker.Clear();
        var updatedCustomer = await context.Set<CustomerWithCollections>()
            .FirstOrDefaultAsync(c => c.Region == "NullCollections" && c.Id == "NULL001");

        Assert.NotNull(updatedCustomer);
        Assert.NotNull(updatedCustomer.Tags);
        Assert.Single(updatedCustomer.Tags);
        Assert.NotNull(updatedCustomer.Scores);
        Assert.Single(updatedCustomer.Scores);
        Assert.NotNull(updatedCustomer.Metadata);
        Assert.Single(updatedCustomer.Metadata);
    }

    public class EndToEndFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public EndToEndFixture()
        {
            _testStore = AzureTableTestStore.Create("EndToEndTest");
        }

        public TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseAzureTable("UseDevelopmentStorage=true")
                .Options;

            var context = new TestDbContext(options);
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

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToAzureTable("Customers");
                entity.HasPartitionKey(c => c.Region);
                entity.HasRowKey(c => c.Id);
                entity.Property(c => c.ETag).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
                entity.Property(c => c.Timestamp).IsTimestamp().ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<CustomerWithCollections>(entity =>
            {
                entity.ToAzureTable("CustomersWithCollections");
                entity.HasPartitionKey(c => c.Region);
                entity.HasRowKey(c => c.Id);
                entity.Property(c => c.ETag).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
                entity.Property(c => c.Timestamp).IsTimestamp().ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<CustomerWithNestedCollections>(entity =>
            {
                entity.ToAzureTable("CustomersWithNestedCollections");
                entity.HasPartitionKey(c => c.Region);
                entity.HasRowKey(c => c.Id);
                entity.Property(c => c.ETag).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
                entity.Property(c => c.Timestamp).IsTimestamp().ValueGeneratedOnAddOrUpdate();
            });
        }
    }

    public class Customer
    {
        public string Region { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public Address? Address { get; set; }
        public string? ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    public class Address
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string ZipCode { get; set; } = null!;
    }

    public class CustomerWithCollections
    {
        public string Region { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public List<string>? Tags { get; set; }
        public int[]? Scores { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    public class CustomerWithNestedCollections
    {
        public string Region { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Dictionary<string, List<string>>? OrdersByYear { get; set; }
        public Dictionary<string, Dictionary<string, object>>? PreferencesByCategory { get; set; }
        public string? ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}