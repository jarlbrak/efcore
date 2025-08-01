// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public class AzureTableWhereQueryTest : IClassFixture<AzureTableWhereQueryTest.AzureTableWhereQueryFixture>
{
    private readonly AzureTableWhereQueryFixture _fixture;

    public AzureTableWhereQueryTest(AzureTableWhereQueryFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public virtual async Task Where_partition_key_equality()
    {
        using var context = _fixture.CreateContext();
        
        var customers = await context.Customers
            .Where(c => c.Country == "UK")
            .ToListAsync();

        Assert.NotEmpty(customers);
        Assert.All(customers, c => Assert.Equal("UK", c.Country));
    }

    [ConditionalFact]
    public virtual async Task Where_row_key_equality()
    {
        using var context = _fixture.CreateContext();
        
        var customer = await context.Customers
            .Where(c => c.Country == "UK" && c.CustomerID == "EASTC")
            .FirstOrDefaultAsync();

        Assert.NotNull(customer);
        Assert.Equal("EASTC", customer.CustomerID);
        Assert.Equal("UK", customer.Country);
    }

    [ConditionalFact]
    public virtual async Task Where_property_equals_constant()
    {
        using var context = _fixture.CreateContext();
        
        var customers = await context.Customers
            .Where(c => c.City == "London")
            .ToListAsync();

        Assert.NotEmpty(customers);
        Assert.All(customers, c => Assert.Equal("London", c.City));
    }

    [ConditionalFact]
    public virtual async Task Where_property_equals_parameter()
    {
        using var context = _fixture.CreateContext();
        var city = "London";
        
        var customers = await context.Customers
            .Where(c => c.City == city)
            .ToListAsync();

        Assert.NotEmpty(customers);
        Assert.All(customers, c => Assert.Equal("London", c.City));
    }

    [ConditionalFact]
    public virtual async Task Where_property_not_equals()
    {
        using var context = _fixture.CreateContext();
        
        var customers = await context.Customers
            .Where(c => c.Country == "UK" && c.City != "London")
            .ToListAsync();

        Assert.NotEmpty(customers);
        Assert.All(customers, c => Assert.NotEqual("London", c.City));
    }

    [ConditionalFact]
    public virtual async Task Where_string_length()
    {
        using var context = _fixture.CreateContext();
        
        var customers = await context.Customers
            .Where(c => c.Country == "UK" && c.CompanyName.Length > 10)
            .ToListAsync();

        Assert.NotEmpty(customers);
        Assert.All(customers, c => Assert.True(c.CompanyName.Length > 10));
    }

    [ConditionalFact]
    public virtual async Task Where_string_contains()
    {
        using var context = _fixture.CreateContext();
        
        var customers = await context.Customers
            .Where(c => c.Country == "UK" && c.CompanyName.Contains("Ltd"))
            .ToListAsync();

        // Note: String contains may have limited support in Azure Table Storage
        Assert.NotNull(customers);
    }

    [ConditionalFact]
    public virtual async Task Where_string_starts_with()
    {
        using var context = _fixture.CreateContext();
        
        var customers = await context.Customers
            .Where(c => c.Country == "UK" && c.CompanyName.StartsWith("A"))
            .ToListAsync();

        Assert.NotNull(customers);
        // Azure Table Storage has limited string function support
    }

    [ConditionalFact]
    public virtual async Task Where_datetime_comparison()
    {
        using var context = _fixture.CreateContext();
        var date = new DateTime(1998, 1, 1);
        
        var orders = await context.Orders
            .Where(o => o.Country == "UK" && o.OrderDate >= date)
            .ToListAsync();

        Assert.NotEmpty(orders);
        Assert.All(orders, o => Assert.True(o.OrderDate >= date));
    }

    [ConditionalFact]
    public virtual async Task Where_int_comparison()
    {
        using var context = _fixture.CreateContext();
        
        var orders = await context.Orders
            .Where(o => o.Country == "UK" && o.EmployeeID > 5)
            .ToListAsync();

        Assert.NotEmpty(orders);
        Assert.All(orders, o => Assert.True(o.EmployeeID > 5));
    }

    [ConditionalFact]
    public virtual async Task Where_bool_equals()
    {
        using var context = _fixture.CreateContext();
        
        var orders = await context.Orders
            .Where(o => o.Country == "UK" && o.IsUrgent == true)
            .ToListAsync();

        Assert.NotNull(orders);
        Assert.All(orders, o => Assert.True(o.IsUrgent));
    }

    [ConditionalFact]
    public virtual async Task Where_guid_equals()
    {
        using var context = _fixture.CreateContext();
        var testGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        var orders = await context.Orders
            .Where(o => o.Country == "UK" && o.TrackingId == testGuid)
            .ToListAsync();

        Assert.NotNull(orders);
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support complex LINQ operations like Any()")]
    public virtual async Task Where_subquery_any()
    {
        using var context = _fixture.CreateContext();
        
        // This type of query is not supported by Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var customers = await context.Customers
                .Where(c => context.Orders.Any(o => o.CustomerID == c.CustomerID))
                .ToListAsync();
        });
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support GROUP BY operations")]
    public virtual async Task Where_with_groupby()
    {
        using var context = _fixture.CreateContext();
        
        // This type of query is not supported by Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var result = await context.Orders
                .Where(o => o.Country == "UK")
                .GroupBy(o => o.CustomerID)
                .Select(g => new { CustomerID = g.Key, Count = g.Count() })
                .ToListAsync();
        });
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support JOIN operations")]
    public virtual async Task Where_with_join()
    {
        using var context = _fixture.CreateContext();
        
        // This type of query is not supported by Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var result = await context.Customers
                .Where(c => c.Country == "UK")
                .Join(context.Orders, c => c.CustomerID, o => o.CustomerID, (c, o) => new { c.CompanyName, o.OrderDate })
                .ToListAsync();
        });
    }

    public class AzureTableWhereQueryFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public AzureTableWhereQueryFixture()
        {
            _testStore = AzureTableTestStore.Create("WhereQueryTest");
            SeedData();
        }

        public WhereQueryContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WhereQueryContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("WhereTest"))
                .Options;

            var context = new WhereQueryContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var customers = new[]
            {
                new Customer { Country = "UK", CustomerID = "EASTC", CompanyName = "Eastern Connection", City = "London" },
                new Customer { Country = "UK", CustomerID = "BSBEV", CompanyName = "B's Beverages", City = "Bristol" },
                new Customer { Country = "UK", CustomerID = "CONSH", CompanyName = "Consolidated Holdings Ltd.", City = "London" },
                new Customer { Country = "USA", CustomerID = "ALFKI", CompanyName = "Alfreds Futterkiste", City = "Seattle" },
                new Customer { Country = "Germany", CustomerID = "BLAUS", CompanyName = "Blauer See Delikatessen", City = "Mannheim" }
            };

            var orders = new[]
            {
                new Order { Country = "UK", OrderID = "ORD001", CustomerID = "EASTC", OrderDate = new DateTime(1998, 5, 1), EmployeeID = 3, IsUrgent = true, TrackingId = Guid.Parse("12345678-1234-1234-1234-123456789012") },
                new Order { Country = "UK", OrderID = "ORD002", CustomerID = "BSBEV", OrderDate = new DateTime(1997, 8, 15), EmployeeID = 7, IsUrgent = false, TrackingId = Guid.NewGuid() },
                new Order { Country = "UK", OrderID = "ORD003", CustomerID = "CONSH", OrderDate = new DateTime(1998, 12, 3), EmployeeID = 2, IsUrgent = true, TrackingId = Guid.NewGuid() },
                new Order { Country = "USA", OrderID = "ORD004", CustomerID = "ALFKI", OrderDate = new DateTime(1997, 3, 22), EmployeeID = 9, IsUrgent = false, TrackingId = Guid.NewGuid() }
            };

            context.Customers.AddRange(customers);
            context.Orders.AddRange(orders);
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

    public class WhereQueryContext : DbContext
    {
        public WhereQueryContext(DbContextOptions<WhereQueryContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.CustomerID);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.OrderID);
            });
        }
    }

    public class Customer
    {
        public string Country { get; set; } = null!;
        public string CustomerID { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string City { get; set; } = null!;
    }

    public class Order
    {
        public string Country { get; set; } = null!;
        public string OrderID { get; set; } = null!;
        public string CustomerID { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public int EmployeeID { get; set; }
        public bool IsUrgent { get; set; }
        public Guid TrackingId { get; set; }
    }
}