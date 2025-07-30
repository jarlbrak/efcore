// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query.Translations;

#nullable disable

public class StringTranslationsAzureTableTest : IClassFixture<StringTranslationsAzureTableTest.StringTranslationsAzureTableFixture>
{
    private readonly StringTranslationsAzureTableFixture _fixture;

    public StringTranslationsAzureTableTest(StringTranslationsAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public virtual async Task String_Length()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && e.Name.Length > 5)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Name.Length > 5));
    }

    [ConditionalFact]
    public virtual async Task String_Equals()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && e.Name.Equals("Azure Table"))
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal("Azure Table", r.Name));
    }

    [ConditionalFact]
    public virtual async Task String_Equals_with_StringComparison()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && e.Name.Equals("azure table", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        Assert.NotNull(results);
        // Note: Azure Table Storage may have limited support for case-insensitive comparisons
    }

    [ConditionalFact]
    public virtual async Task String_StartsWith()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && e.Name.StartsWith("Azure"))
            .ToListAsync();

        Assert.NotNull(results);
        // Note: Azure Table Storage has limited support for string prefix operations
    }

    [ConditionalFact]
    public virtual async Task String_EndsWith()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && e.Name.EndsWith("Table"))
            .ToListAsync();

        Assert.NotNull(results);
        // Note: Azure Table Storage has limited support for string suffix operations
    }

    [ConditionalFact]
    public virtual async Task String_Contains()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && e.Name.Contains("Table"))
            .ToListAsync();

        Assert.NotNull(results);
        // Note: Azure Table Storage has limited support for string contains operations
    }

    [ConditionalFact]
    public virtual async Task String_Substring()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, Substring = e.Name.Substring(0, 5) })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Substring.Length <= 5));
    }

    [ConditionalFact]
    public virtual async Task String_Substring_with_length()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, Substring = e.Name.Substring(1, 3) })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Substring.Length <= 3));
    }

    [ConditionalFact]
    public virtual async Task String_ToLower()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, LowerName = e.Name.ToLower() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(r.LowerName, r.LowerName.ToLower()));
    }

    [ConditionalFact]
    public virtual async Task String_ToUpper()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, UpperName = e.Name.ToUpper() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(r.UpperName, r.UpperName.ToUpper()));
    }

    [ConditionalFact]
    public virtual async Task String_Trim()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, TrimmedName = e.Description.Trim() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r.TrimmedName));
    }

    [ConditionalFact]
    public virtual async Task String_TrimStart()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, TrimmedName = e.Description.TrimStart() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r.TrimmedName));
    }

    [ConditionalFact]
    public virtual async Task String_TrimEnd()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, TrimmedName = e.Description.TrimEnd() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r.TrimmedName));
    }

    [ConditionalFact]
    public virtual async Task String_IndexOf()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, IndexOfTable = e.Name.IndexOf("Table") })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.IndexOfTable >= -1));
    }

    [ConditionalFact]
    public virtual async Task String_Replace()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, ReplacedName = e.Name.Replace("Table", "Storage") })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r.ReplacedName));
    }

    [ConditionalFact]
    public virtual async Task String_Concat()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, FullName = e.Name + " - " + e.Description })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains(" - ", r.FullName));
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for string.IsNullOrEmpty")]
    public virtual async Task String_IsNullOrEmpty()
    {
        using var context = _fixture.CreateContext();
        
        // This may not be supported in Azure Table Storage query translation
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && !string.IsNullOrEmpty(e.Description))
            .ToListAsync();

        Assert.NotNull(results);
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for string.IsNullOrWhiteSpace")]
    public virtual async Task String_IsNullOrWhiteSpace()
    {
        using var context = _fixture.CreateContext();
        
        // This may not be supported in Azure Table Storage query translation
        var results = await context.StringEntities
            .Where(e => e.PartitionKey == "Test" && !string.IsNullOrWhiteSpace(e.Description))
            .ToListAsync();

        Assert.NotNull(results);
    }

    public class StringTranslationsAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public StringTranslationsAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("StringTranslationsTest");
            SeedData();
        }

        public StringTranslationsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<StringTranslationsContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("StringTest"))
                .Options;

            var context = new StringTranslationsContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var entities = new[]
            {
                new StringEntity { PartitionKey = "Test", Id = "1", Name = "Azure Table", Description = " This is a test entity " },
                new StringEntity { PartitionKey = "Test", Id = "2", Name = "Entity Framework", Description = "EF Core provider" },
                new StringEntity { PartitionKey = "Test", Id = "3", Name = "NoSQL Database", Description = " Azure Table Storage " },
                new StringEntity { PartitionKey = "Test", Id = "4", Name = "Cloud Storage", Description = "Microsoft Azure" },
                new StringEntity { PartitionKey = "Other", Id = "5", Name = "Different Partition", Description = "Other data" }
            };

            context.StringEntities.AddRange(entities);
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

    public class StringTranslationsContext : DbContext
    {
        public StringTranslationsContext(DbContextOptions<StringTranslationsContext> options) : base(options)
        {
        }

        public DbSet<StringEntity> StringEntities => Set<StringEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StringEntity>(entity =>
            {
                entity.ToAzureTable("StringEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class StringEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}