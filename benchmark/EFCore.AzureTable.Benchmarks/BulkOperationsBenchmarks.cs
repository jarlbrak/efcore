// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.Tables;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore.AzureTable.Metadata;
using Microsoft.EntityFrameworkCore.AzureTable.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.AzureTable.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class BulkOperationsBenchmarks
{
    private TestDbContext _efContext = null!;
    private TableClient _directTableClient = null!;
    private List<BenchmarkEntity> _entities = null!;

    [GlobalSetup]
    public void Setup()
    {
        var connectionString = "UseDevelopmentStorage=true";
        
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseAzureTable(connectionString, opt => opt.TableNamePrefix("Benchmark"))
            .Options;

        _efContext = new TestDbContext(options);
        _efContext.Database.EnsureCreated();

        _directTableClient = new TableClient(connectionString, "BenchmarkEntities");
        _directTableClient.CreateIfNotExists();

        _entities = GenerateEntities(1000);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _efContext?.Dispose();
        _directTableClient?.Delete();
    }

    [Benchmark(Baseline = true)]
    public async Task DirectTableClient_BulkInsert()
    {
        var partitionKey = $"Direct_{Guid.NewGuid():N}";
        var tableEntities = _entities.Select(e => new TableEntity(partitionKey, e.Id.ToString())
        {
            ["Name"] = e.Name,
            ["Value"] = e.Value,
            ["Data"] = e.Data
        }).ToList();

        var batches = tableEntities
            .Select((entity, index) => new { entity, index })
            .GroupBy(x => x.index / 100)
            .Select(g => g.Select(x => x.entity).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            var actions = batch.Select(e => new TableTransactionAction(TableTransactionActionType.Add, e)).ToList();
            await _directTableClient.SubmitTransactionAsync(actions);
        }
    }

    [Benchmark]
    public async Task EFCore_BulkInsert_Via_Extensions()
    {
        var partitionKey = $"EFBulk_{Guid.NewGuid():N}";
        var entities = _entities.Select(e => new BenchmarkEntity
        {
            PartitionKey = partitionKey,
            Id = e.Id,
            Name = e.Name,
            Value = e.Value,
            Data = e.Data
        }).ToList();

        var tableClient = new TableClient("UseDevelopmentStorage=true", "BenchmarkBenchmarkEntities");
        await tableClient.BulkInsertAsync(
            entities,
            partitionKey,
            e => e.Id.ToString(),
            e => new TableEntity(e.PartitionKey, e.Id.ToString())
            {
                ["Name"] = e.Name,
                ["Value"] = e.Value,
                ["Data"] = e.Data
            });
    }

    [Benchmark]
    public async Task EFCore_Traditional_AddRange()
    {
        var partitionKey = $"EFTrad_{Guid.NewGuid():N}";
        var entities = _entities.Select(e => new BenchmarkEntity
        {
            PartitionKey = partitionKey,
            Id = e.Id,
            Name = e.Name,
            Value = e.Value,
            Data = e.Data
        }).ToList();

        _efContext.Entities.AddRange(entities);
        await _efContext.SaveChangesAsync();

        // Clean up for next iteration
        _efContext.ChangeTracker.Clear();
    }

    [Params(100, 500, 1000)]
    public int EntityCount { get; set; }

    [Benchmark]
    public async Task VaryingEntityCount_BulkInsert()
    {
        var partitionKey = $"Varying_{Guid.NewGuid():N}";
        var entities = GenerateEntities(EntityCount);
        
        var tableClient = new TableClient("UseDevelopmentStorage=true", "BenchmarkBenchmarkEntities");
        await tableClient.BulkInsertAsync(
            entities,
            partitionKey,
            e => e.Id.ToString(),
            e => new TableEntity(partitionKey, e.Id.ToString())
            {
                ["Name"] = e.Name,
                ["Value"] = e.Value,
                ["Data"] = e.Data
            });
    }

    private static List<BenchmarkEntity> GenerateEntities(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new BenchmarkEntity
            {
                Id = i,
                Name = $"Entity_{i:D6}",
                Value = i * 1.5,
                Data = $"Some data for entity {i} with additional text to make it realistic"
            })
            .ToList();
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<BenchmarkEntity> Entities => Set<BenchmarkEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BenchmarkEntity>(entity =>
            {
                entity.ToTable("Entities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class BenchmarkEntity
    {
        public string PartitionKey { get; set; } = null!;
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Value { get; set; }
        public string Data { get; set; } = null!;
    }
}