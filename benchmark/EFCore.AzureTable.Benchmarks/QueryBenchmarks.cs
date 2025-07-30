// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.Tables;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata;

namespace Microsoft.EntityFrameworkCore.AzureTable.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class QueryBenchmarks
{
    private TestDbContext _efContext = null!;
    private TableClient _directTableClient = null!;
    private const string TestPartitionKey = "QueryBenchmark";

    [GlobalSetup]
    public async Task Setup()
    {
        var connectionString = "UseDevelopmentStorage=true";
        
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseAzureTable(connectionString, opt => opt.TableNamePrefix("QueryBench"))
            .Options;

        _efContext = new TestDbContext(options);
        _efContext.Database.EnsureCreated();

        _directTableClient = new TableClient(connectionString, "QueryBenchEntities");
        _directTableClient.CreateIfNotExists();

        // Seed test data
        await SeedTestData();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _efContext?.Dispose();
        _directTableClient?.Delete();
    }

    private async Task SeedTestData()
    {
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new QueryEntity
            {
                PartitionKey = TestPartitionKey,
                Id = i.ToString("D6"),
                Name = $"Entity_{i:D6}",
                Category = $"Category_{i % 5}",
                Value = i * 1.5,
                IsActive = i % 2 == 0,
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        _efContext.Entities.AddRange(entities);
        await _efContext.SaveChangesAsync();
        _efContext.ChangeTracker.Clear();
    }

    [Benchmark(Baseline = true)]
    public async Task<List<TableEntity>> DirectTableClient_PartitionQuery()
    {
        var entities = new List<TableEntity>();
        await foreach (var entity in _directTableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{TestPartitionKey}'"))
        {
            entities.Add(entity);
        }
        return entities;
    }

    [Benchmark]
    public async Task<List<QueryEntity>> EFCore_PartitionQuery()
    {
        return await _efContext.Entities
            .Where(e => e.PartitionKey == TestPartitionKey)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<TableEntity>> DirectTableClient_FilterQuery()
    {
        var entities = new List<TableEntity>();
        await foreach (var entity in _directTableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{TestPartitionKey}' and Category eq 'Category_1'"))
        {
            entities.Add(entity);
        }
        return entities;
    }

    [Benchmark]
    public async Task<List<QueryEntity>> EFCore_FilterQuery()
    {
        return await _efContext.Entities
            .Where(e => e.PartitionKey == TestPartitionKey && e.Category == "Category_1")
            .ToListAsync();
    }

    [Benchmark]
    public async Task<QueryEntity?> DirectTableClient_PointQuery()
    {
        try
        {
            var response = await _directTableClient.GetEntityAsync<TableEntity>(TestPartitionKey, "000001");
            var entity = response.Value;
            
            return new QueryEntity
            {
                PartitionKey = entity.PartitionKey,
                Id = entity.RowKey,
                Name = entity.GetString("Name"),
                Category = entity.GetString("Category"),
                Value = entity.GetDouble("Value") ?? 0,
                IsActive = entity.GetBoolean("IsActive") ?? false,
                CreatedDate = entity.GetDateTimeOffset("CreatedDate")?.DateTime ?? DateTime.MinValue
            };
        }
        catch
        {
            return null;
        }
    }

    [Benchmark]
    public async Task<QueryEntity?> EFCore_PointQuery()
    {
        return await _efContext.Entities
            .FirstOrDefaultAsync(e => e.PartitionKey == TestPartitionKey && e.Id == "000001");
    }

    [Benchmark]
    public async Task<List<QueryEntity>> EFCore_ComplexQuery()
    {
        return await _efContext.Entities
            .Where(e => e.PartitionKey == TestPartitionKey)
            .Where(e => e.IsActive)
            .Where(e => e.Value > 100)
            .OrderBy(e => e.Name)
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<int> EFCore_CountQuery()
    {
        return await _efContext.Entities
            .Where(e => e.PartitionKey == TestPartitionKey)
            .Where(e => e.Category == "Category_2")
            .CountAsync();
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<QueryEntity> Entities => Set<QueryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueryEntity>(entity =>
            {
                entity.ToTable("Entities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class QueryEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public double Value { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}