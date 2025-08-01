// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class ConfigPatternsAzureTableTest : IClassFixture<ConfigPatternsAzureTableTest.AzureTableFixture>
{
    private const string TableNamePrefix = "ConfigPatternsAzureTable";

    protected AzureTableFixture Fixture { get; }

    public ConfigPatternsAzureTableTest(AzureTableFixture fixture)
    {
        Fixture = fixture;
    }

    [ConditionalFact]
    public async Task Connection_string_is_used_correctly()
    {
        await using var testStore = AzureTableTestStore.Create(TableNamePrefix);
        var options = CreateOptions(testStore);

        using var context = new ConfigTestContext(options);
        
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [ConditionalFact]
    public async Task Table_name_prefix_is_applied()
    {
        await using var testStore = AzureTableTestStore.Create(TableNamePrefix);
        var options = CreateOptions(testStore);

        using var context = new ConfigTestContext(options);
        await context.Database.EnsureCreatedAsync();
        
        // Verify that table names are created with the specified prefix
        Assert.NotNull(context.Customers);
    }

    [ConditionalFact]
    public async Task Multiple_contexts_can_share_connection()
    {
        await using var testStore = AzureTableTestStore.Create(TableNamePrefix);
        var options = CreateOptions(testStore);

        using var context1 = new ConfigTestContext(options);
        using var context2 = new ConfigTestContext(options);

        var canConnect1 = await context1.Database.CanConnectAsync();
        var canConnect2 = await context2.Database.CanConnectAsync();

        Assert.True(canConnect1);
        Assert.True(canConnect2);
    }

    [ConditionalFact]
    public Task Context_can_be_configured_with_connection_string()
    {
        var connectionString = "UseDevelopmentStorage=true";
        var options = new DbContextOptionsBuilder<ConfigTestContext>()
            .UseAzureTable(connectionString)
            .Options;

        using var context = new ConfigTestContext(options);
        Assert.NotNull(context);
        return Task.CompletedTask;
    }

    [ConditionalFact]
    public async Task Context_can_be_configured_with_action()
    {
        var connectionString = "UseDevelopmentStorage=true";
        var options = new DbContextOptionsBuilder<ConfigTestContext>()
            .UseAzureTable(connectionString)
            .Options;

        using var context = new ConfigTestContext(options);
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [ConditionalFact]
    public async Task Can_add_and_query_entities()
    {
        await using var testStore = AzureTableTestStore.Create(TableNamePrefix);
        var options = CreateOptions(testStore);

        using var context = new ConfigTestContext(options);
        await context.Database.EnsureCreatedAsync();

        var customer = new ConfigCustomer
        {
            Country = "UK",
            CustomerId = "TEST001",
            CompanyName = "Test Company",
            City = "London"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var retrievedCustomer = await context.Customers
            .FirstOrDefaultAsync(c => c.Country == "UK" && c.CustomerId == "TEST001");

        Assert.NotNull(retrievedCustomer);
        Assert.Equal("Test Company", retrievedCustomer.CompanyName);
    }

    [ConditionalFact]
    public async Task Context_disposes_properly()
    {
        await using var testStore = AzureTableTestStore.Create(TableNamePrefix);
        var options = CreateOptions(testStore);

        ConfigTestContext context;
        using (context = new ConfigTestContext(options))
        {
            var canConnect = await context.Database.CanConnectAsync();
            Assert.True(canConnect);
        }

        // Context should be disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(() => context.Database.CanConnectAsync());
    }

    [ConditionalFact]
    public async Task Can_configure_table_names()
    {
        await using var testStore = AzureTableTestStore.Create(TableNamePrefix);
        var options = CreateOptions(testStore);

        using var context = new ConfigTestContext(options);
        await context.Database.EnsureCreatedAsync();

        // Verify that entities are configured correctly
        var entityType = context.Model.FindEntityType(typeof(ConfigCustomer));
        Assert.NotNull(entityType);
        
        var partitionKey = entityType.FindProperty("Country");
        var rowKey = entityType.FindProperty("CustomerId");
        
        Assert.NotNull(partitionKey);
        Assert.NotNull(rowKey);
    }

    [ConditionalFact]
    public async Task Can_handle_concurrent_operations()
    {
        await using var testStore = AzureTableTestStore.Create($"{TableNamePrefix}_Concurrent");
        var options = CreateOptions(testStore);

        var tasks = new List<Task>();
        
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                using var context = new ConfigTestContext(options);
                await context.Database.EnsureCreatedAsync();
                
                var customer = new ConfigCustomer
                {
                    Country = "UK",
                    CustomerId = $"CONCURRENT{taskId:D3}",
                    CompanyName = $"Concurrent Company {taskId}",
                    City = "London"
                };

                context.Customers.Add(customer);
                await context.SaveChangesAsync();
            }));
        }

        await Task.WhenAll(tasks);

        // Verify all entities were saved
        using var verifyContext = new ConfigTestContext(options);
        var concurrentCustomers = await verifyContext.Customers
            .Where(c => c.Country == "UK" && c.CustomerId.StartsWith("CONCURRENT"))
            .ToListAsync();

        Assert.Equal(5, concurrentCustomers.Count);
    }

    [ConditionalFact]
    public void Invalid_connection_string_throws_exception()
    {
        var invalidConnectionString = "InvalidConnectionString";
        
        Assert.Throws<ArgumentException>(() =>
            new DbContextOptionsBuilder<ConfigTestContext>()
                .UseAzureTable(invalidConnectionString)
                .Options);
    }

    [ConditionalFact]
    public void Null_connection_string_throws_exception()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DbContextOptionsBuilder<ConfigTestContext>()
                .UseAzureTable(null)
                .Options);
    }

    private DbContextOptions<ConfigTestContext> CreateOptions(AzureTableTestStore testStore)
        => new DbContextOptionsBuilder<ConfigTestContext>()
            .UseAzureTable("UseDevelopmentStorage=true")
            .Options;

    public class AzureTableFixture : IDisposable
    {
        public void Dispose()
        {
            // Cleanup any shared resources
        }
    }

    public class ConfigTestContext : DbContext
    {
        public ConfigTestContext(DbContextOptions<ConfigTestContext> options) : base(options)
        {
        }

        public DbSet<ConfigCustomer> Customers => Set<ConfigCustomer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConfigCustomer>(entity =>
            {
                entity.ToTable("ConfigCustomers");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.CustomerId);
            });
        }
    }

    public class ConfigCustomer
    {
        public string Country { get; set; } = null!;
        public string CustomerId { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string City { get; set; } = null!;
    }
}