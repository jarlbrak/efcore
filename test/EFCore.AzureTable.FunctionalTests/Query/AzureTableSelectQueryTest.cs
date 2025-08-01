// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public class AzureTableSelectQueryTest : IClassFixture<AzureTableSelectQueryTest.AzureTableSelectQueryFixture>
{
    private readonly AzureTableSelectQueryFixture _fixture;

    public AzureTableSelectQueryTest(AzureTableSelectQueryFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public virtual async Task Select_single_property()
    {
        using var context = _fixture.CreateContext();
        
        var companyNames = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => c.CompanyName)
            .ToListAsync();

        Assert.NotEmpty(companyNames);
        Assert.All(companyNames, name => Assert.NotNull(name));
    }

    [ConditionalFact]
    public virtual async Task Select_multiple_properties()
    {
        using var context = _fixture.CreateContext();
        
        var customerInfo = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => new { c.CustomerID, c.CompanyName, c.City })
            .ToListAsync();

        Assert.NotEmpty(customerInfo);
        Assert.All(customerInfo, info => 
        {
            Assert.NotNull(info.CustomerID);
            Assert.NotNull(info.CompanyName);
            Assert.NotNull(info.City);
        });
    }

    [ConditionalFact]
    public virtual async Task Select_with_constant()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => new { c.CustomerID, Country = "United Kingdom" })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal("United Kingdom", r.Country));
    }

    [ConditionalFact]
    public virtual async Task Select_with_string_length()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => new { c.CustomerID, NameLength = c.CompanyName.Length })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.NameLength > 0));
    }

    [ConditionalFact]
    public virtual async Task Select_with_string_concatenation()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => new { c.CustomerID, FullInfo = c.CompanyName + " - " + c.City })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains(" - ", r.FullInfo));
    }

    [ConditionalFact]
    public virtual async Task Select_with_conditional()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.Orders
            .Where(o => o.Country == "UK")
            .Select(o => new { o.OrderID, Status = o.IsUrgent ? "Urgent" : "Normal" })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Status == "Urgent" || r.Status == "Normal"));
    }

    [ConditionalFact]
    public virtual async Task Select_with_datetime_properties()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.Orders
            .Where(o => o.Country == "UK")
            .Select(o => new { o.OrderID, o.OrderDate.Year, o.OrderDate.Month })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => 
        {
            Assert.True(r.Year > 1990);
            Assert.True(r.Month >= 1 && r.Month <= 12);
        });
    }

    [ConditionalFact]
    public virtual async Task Select_with_guid_tostring()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.Orders
            .Where(o => o.Country == "UK")
            .Select(o => new { o.OrderID, TrackingIdString = o.TrackingId.ToString() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r.TrackingIdString));
    }

    [ConditionalFact]
    public virtual async Task Select_first()
    {
        using var context = _fixture.CreateContext();
        
        var firstCustomer = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => c.CompanyName)
            .FirstAsync();

        Assert.NotNull(firstCustomer);
    }

    [ConditionalFact]
    public virtual async Task Select_first_or_default()
    {
        using var context = _fixture.CreateContext();
        
        var customer = await context.Customers
            .Where(c => c.Country == "NonExistent")
            .Select(c => c.CompanyName)
            .FirstOrDefaultAsync();

        Assert.Null(customer);
    }

    [ConditionalFact]
    public virtual async Task Select_single()
    {
        using var context = _fixture.CreateContext();
        
        var customer = await context.Customers
            .Where(c => c.Country == "UK" && c.CustomerID == "EASTC")
            .Select(c => c.CompanyName)
            .SingleAsync();

        Assert.Equal("Eastern Connection", customer);
    }

    [ConditionalFact]
    public virtual async Task Select_with_take()
    {
        using var context = _fixture.CreateContext();
        
        var topCustomers = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => c.CompanyName)
            .Take(2)
            .ToListAsync();

        Assert.True(topCustomers.Count <= 2);
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for Skip operations")]
    public virtual async Task Select_with_skip()
    {
        using var context = _fixture.CreateContext();
        
        // Skip operations may not be well supported in Azure Table Storage
        var customers = await context.Customers
            .Where(c => c.Country == "UK")
            .Select(c => c.CompanyName)
            .Skip(1)
            .ToListAsync();

        Assert.NotNull(customers);
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support Count() in SELECT projections")]
    public virtual async Task Select_with_count()
    {
        using var context = _fixture.CreateContext();
        
        // This type of operation is not supported by Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var result = await context.Customers
                .Select(c => new { c.CustomerID, OrderCount = context.Orders.Count(o => o.CustomerID == c.CustomerID) })
                .ToListAsync();
        });
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support complex projections with subqueries")]
    public virtual async Task Select_with_subquery()
    {
        using var context = _fixture.CreateContext();
        
        // This type of operation is not supported by Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var result = await context.Customers
                .Select(c => new { c.CustomerID, FirstOrder = context.Orders.FirstOrDefault(o => o.CustomerID == c.CustomerID) })
                .ToListAsync();
        });
    }

    public class AzureTableSelectQueryFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public AzureTableSelectQueryFixture()
        {
            _testStore = AzureTableTestStore.Create("SelectQueryTest");
            SeedData();
        }

        public SelectQueryContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SelectQueryContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("SelectTest"))
                .Options;

            var context = new SelectQueryContext(options);
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
                new Customer { Country = "USA", CustomerID = "ALFKI", CompanyName = "Alfreds Futterkiste", City = "Seattle" }
            };

            var orders = new[]
            {
                new Order { Country = "UK", OrderID = "ORD001", CustomerID = "EASTC", OrderDate = new DateTime(1998, 5, 1), IsUrgent = true, TrackingId = Guid.NewGuid() },
                new Order { Country = "UK", OrderID = "ORD002", CustomerID = "BSBEV", OrderDate = new DateTime(1997, 8, 15), IsUrgent = false, TrackingId = Guid.NewGuid() },
                new Order { Country = "UK", OrderID = "ORD003", CustomerID = "CONSH", OrderDate = new DateTime(1998, 12, 3), IsUrgent = true, TrackingId = Guid.NewGuid() }
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

    public class SelectQueryContext : DbContext
    {
        public SelectQueryContext(DbContextOptions<SelectQueryContext> options) : base(options)
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
        public bool IsUrgent { get; set; }
        public Guid TrackingId { get; set; }
    }
}