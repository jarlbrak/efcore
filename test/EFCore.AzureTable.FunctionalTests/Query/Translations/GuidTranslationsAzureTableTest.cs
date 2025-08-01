// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query.Translations;

#nullable disable

public class GuidTranslationsAzureTableTest : IClassFixture<GuidTranslationsAzureTableTest.GuidTranslationsAzureTableFixture>
{
    private readonly GuidTranslationsAzureTableFixture _fixture;

    public GuidTranslationsAzureTableTest(GuidTranslationsAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public virtual async Task Guid_NewGuid()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, NewGuid = Guid.NewGuid() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotEqual(Guid.Empty, r.NewGuid));
    }

    [ConditionalFact]
    public virtual async Task Guid_Empty()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && e.GuidValue == Guid.Empty)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(Guid.Empty, r.GuidValue));
    }

    [ConditionalFact]
    public virtual async Task Guid_equality()
    {
        using var context = _fixture.CreateContext();
        var testGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && e.GuidValue == testGuid)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(testGuid, r.GuidValue));
    }

    [ConditionalFact]
    public virtual async Task Guid_not_equality()
    {
        using var context = _fixture.CreateContext();
        var testGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && e.GuidValue != testGuid)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.NotEqual(testGuid, r.GuidValue));
    }

    [ConditionalFact]
    public virtual async Task Guid_ToString()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, GuidString = e.GuidValue.ToString() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => 
        {
            Assert.NotNull(r.GuidString);
            Assert.True(Guid.TryParse(r.GuidString, out _));
        });
    }

    [ConditionalFact]
    public virtual async Task Guid_ToString_with_format()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, GuidString = e.GuidValue.ToString("D") })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => 
        {
            Assert.NotNull(r.GuidString);
            Assert.Equal(36, r.GuidString.Length); // Format "D" produces 36 characters
            Assert.True(Guid.TryParse(r.GuidString, out _));
        });
    }

    [ConditionalFact]
    public virtual async Task Guid_comparison_with_parameter()
    {
        using var context = _fixture.CreateContext();
        var parameterGuid = Guid.Parse("87654321-4321-4321-4321-210987654321");
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && e.GuidValue == parameterGuid)
            .ToListAsync();

        Assert.NotNull(results);
    }

    [ConditionalFact]
    public virtual async Task Nullable_Guid_equality()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && e.NullableGuidValue == null)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Null(r.NullableGuidValue));
    }

    [ConditionalFact]
    public virtual async Task Nullable_Guid_with_value()
    {
        using var context = _fixture.CreateContext();
        var testGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && e.NullableGuidValue == testGuid)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(testGuid, r.NullableGuidValue));
    }

    [ConditionalFact]
    public virtual async Task Guid_in_projection()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => e.GuidValue)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, guid => Assert.NotEqual(default(Guid), guid));
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for Guid.Parse in queries")]
    public virtual async Task Guid_Parse()
    {
        using var context = _fixture.CreateContext();
        
        // This may not be supported in Azure Table Storage query translation
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test" && Guid.Parse(e.GuidString) == e.GuidValue)
            .ToListAsync();

        Assert.NotNull(results);
    }

    [ConditionalFact]
    public virtual async Task Guid_property_access()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.GuidEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new 
            { 
                e.Id, 
                e.GuidValue,
                StringRepresentation = e.GuidValue.ToString()
            })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => 
        {
            Assert.NotEqual(Guid.Empty, r.GuidValue);
            Assert.NotNull(r.StringRepresentation);
        });
    }

    public class GuidTranslationsAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public GuidTranslationsAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("GuidTranslationsTest");
            SeedData();
        }

        public GuidTranslationsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GuidTranslationsContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("GuidTest"))
                .Options;

            var context = new GuidTranslationsContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var entities = new[]
            {
                new GuidEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "1", 
                    GuidValue = Guid.Parse("12345678-1234-1234-1234-123456789012"),
                    NullableGuidValue = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    GuidString = "12345678-1234-1234-1234-123456789012"
                },
                new GuidEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "2", 
                    GuidValue = Guid.Parse("87654321-4321-4321-4321-210987654321"),
                    NullableGuidValue = null,
                    GuidString = "87654321-4321-4321-4321-210987654321"
                },
                new GuidEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "3", 
                    GuidValue = Guid.Empty,
                    NullableGuidValue = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                    GuidString = "00000000-0000-0000-0000-000000000000"
                },
                new GuidEntity 
                { 
                    PartitionKey = "Other", 
                    Id = "4", 
                    GuidValue = Guid.NewGuid(),
                    NullableGuidValue = Guid.NewGuid(),
                    GuidString = Guid.NewGuid().ToString()
                }
            };

            context.GuidEntities.AddRange(entities);
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

    public class GuidTranslationsContext : DbContext
    {
        public GuidTranslationsContext(DbContextOptions<GuidTranslationsContext> options) : base(options)
        {
        }

        public DbSet<GuidEntity> GuidEntities => Set<GuidEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidEntity>(entity =>
            {
                entity.ToTable("GuidEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class GuidEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public Guid GuidValue { get; set; }
        public Guid? NullableGuidValue { get; set; }
        public string GuidString { get; set; } = null!;
    }
}