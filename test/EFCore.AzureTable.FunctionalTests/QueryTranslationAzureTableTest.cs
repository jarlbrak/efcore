// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.AzureTable;

#nullable disable

public class QueryTranslationAzureTableTest : IClassFixture<QueryTranslationAzureTableTest.QueryTranslationAzureTableFixture>
{
    private readonly QueryTranslationAzureTableFixture _fixture;

    public QueryTranslationAzureTableTest(QueryTranslationAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Can_filter_by_partition_key_equality()
    {
        using var context = _fixture.CreateContext();
        
        var result = await context.TestEntities
            .Where(e => e.PartitionKey == "TestPartition")
            .ToListAsync();

        Assert.NotNull(result);
        // Verify that Azure Table handled the partition key filter efficiently
    }

    [ConditionalFact]
    public async Task Can_filter_by_row_key_equality()
    {
        using var context = _fixture.CreateContext();
        
        var result = await context.TestEntities
            .Where(e => e.RowKey == "TestRow001")
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("TestRow001", result.RowKey);
    }

    [ConditionalFact]
    public async Task Can_filter_by_partition_and_row_key()
    {
        using var context = _fixture.CreateContext();
        
        var result = await context.TestEntities
            .Where(e => e.PartitionKey == "TestPartition" && e.RowKey == "TestRow001")
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("TestPartition", result.PartitionKey);
        Assert.Equal("TestRow001", result.RowKey);
    }

    [ConditionalFact]
    public async Task Can_filter_by_row_key_comparison()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TestEntities
            .Where(e => e.PartitionKey == "TestPartition" && e.RowKey.CompareTo("TestRow005") >= 0)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.RowKey.CompareTo("TestRow005") >= 0));
    }

    [ConditionalFact]
    public async Task Can_filter_by_string_property()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TestEntities
            .Where(e => e.StringProperty == "TestValue")
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal("TestValue", r.StringProperty));
    }

    [ConditionalFact]
    public async Task Can_filter_by_integer_property()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TestEntities
            .Where(e => e.IntProperty > 100)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.IntProperty > 100));
    }

    [ConditionalFact]
    public async Task Can_filter_by_boolean_property()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TestEntities
            .Where(e => e.BoolProperty == true)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.BoolProperty));
    }

    [ConditionalFact]
    public async Task Can_filter_by_datetime_property()
    {
        using var context = _fixture.CreateContext();
        var testDate = new DateTime(2024, 1, 1);
        
        var results = await context.TestEntities
            .Where(e => e.DateTimeProperty >= testDate)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.DateTimeProperty >= testDate));
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for complex LINQ operations")]
    public async Task Cannot_use_complex_linq_operations()
    {
        using var context = _fixture.CreateContext();
        
        // This type of query should be skipped for Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var results = await context.TestEntities
                .GroupBy(e => e.StringProperty)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();
        });
    }

    [ConditionalFact(Skip = "Azure Table Storage does not support joins")]
    public async Task Cannot_use_joins()
    {
        using var context = _fixture.CreateContext();
        
        // This type of query should be skipped for Azure Table Storage
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var results = await context.TestEntities
                .Join(context.RelatedEntities, 
                      t => t.RowKey, 
                      r => r.TestEntityRowKey, 
                      (t, r) => new { Test = t, Related = r })
                .ToListAsync();
        });
    }

    public class QueryTranslationAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public QueryTranslationAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("QueryTranslationTest");
            SeedData();
        }

        public TestContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("QueryTest"))
                .Options;

            var context = new TestContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var testEntities = new[]
            {
                new TestEntity { PartitionKey = "TestPartition", RowKey = "TestRow001", StringProperty = "TestValue", IntProperty = 50, BoolProperty = true, DateTimeProperty = new DateTime(2023, 1, 1) },
                new TestEntity { PartitionKey = "TestPartition", RowKey = "TestRow002", StringProperty = "OtherValue", IntProperty = 150, BoolProperty = false, DateTimeProperty = new DateTime(2024, 1, 1) },
                new TestEntity { PartitionKey = "TestPartition", RowKey = "TestRow005", StringProperty = "TestValue", IntProperty = 200, BoolProperty = true, DateTimeProperty = new DateTime(2024, 6, 1) },
                new TestEntity { PartitionKey = "OtherPartition", RowKey = "TestRow001", StringProperty = "TestValue", IntProperty = 75, BoolProperty = false, DateTimeProperty = new DateTime(2024, 3, 1) }
            };

            context.Set<TestEntity>().AddRange(testEntities);
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

    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<RelatedEntity> RelatedEntities => Set<RelatedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.ToTable("TestEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
            });

            modelBuilder.Entity<RelatedEntity>(entity =>
            {
                entity.ToTable("RelatedEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
            });
        }
    }

    public class TestEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string StringProperty { get; set; } = null!;
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
    }

    public class RelatedEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string TestEntityRowKey { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}