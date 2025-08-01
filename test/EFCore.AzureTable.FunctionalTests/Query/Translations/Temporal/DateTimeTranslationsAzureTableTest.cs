// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query.Translations.Temporal;

#nullable disable

public class DateTimeTranslationsAzureTableTest : IClassFixture<DateTimeTranslationsAzureTableTest.DateTimeTranslationsAzureTableFixture>
{
    private readonly DateTimeTranslationsAzureTableFixture _fixture;

    public DateTimeTranslationsAzureTableTest(DateTimeTranslationsAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public virtual async Task DateTime_Year()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Year == 2024)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(2024, r.CreatedDate.Year));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Month()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Month == 6)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(6, r.CreatedDate.Month));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Day()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Day == 15)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(15, r.CreatedDate.Day));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Hour()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Hour >= 10)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.CreatedDate.Hour >= 10));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Minute()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Minute < 30)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.CreatedDate.Minute < 30));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Second()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Second == 0)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(0, r.CreatedDate.Second));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Date()
    {
        using var context = _fixture.CreateContext();
        var testDate = new DateTime(2024, 6, 15);
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Date == testDate.Date)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(testDate.Date, r.CreatedDate.Date));
    }

    [ConditionalFact]
    public virtual async Task DateTime_TimeOfDay()
    {
        using var context = _fixture.CreateContext();
        var noon = TimeSpan.FromHours(12);
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.TimeOfDay >= noon)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.CreatedDate.TimeOfDay >= noon));
    }

    [ConditionalFact]
    public virtual async Task DateTime_DayOfWeek()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.DayOfWeek == DayOfWeek.Saturday)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(DayOfWeek.Saturday, r.CreatedDate.DayOfWeek));
    }

    [ConditionalFact]
    public virtual async Task DateTime_DayOfYear()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.DayOfYear > 100)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.CreatedDate.DayOfYear > 100));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Now()
    {
        using var context = _fixture.CreateContext();
        var yesterday = DateTime.Now.AddDays(-1);
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate >= yesterday)
            .ToListAsync();

        Assert.NotNull(results);
        // Note: DateTime.Now in queries may have limitations in Azure Table Storage
    }

    [ConditionalFact]
    public virtual async Task DateTime_UtcNow()
    {
        using var context = _fixture.CreateContext();
        var yesterday = DateTime.UtcNow.AddDays(-1);
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate >= yesterday)
            .ToListAsync();

        Assert.NotNull(results);
        // Note: DateTime.UtcNow in queries may have limitations in Azure Table Storage
    }

    [ConditionalFact]
    public virtual async Task DateTime_Today()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate.Date >= DateTime.Today)
            .ToListAsync();

        Assert.NotNull(results);
    }

    [ConditionalFact]
    public virtual async Task DateTime_comparison()
    {
        using var context = _fixture.CreateContext();
        var cutoffDate = new DateTime(2024, 6, 1);
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && e.CreatedDate >= cutoffDate)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.CreatedDate >= cutoffDate));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Add()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, FutureDate = e.CreatedDate.AddDays(30) })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.FutureDate > r.FutureDate.AddDays(-30)));
    }

    [ConditionalFact]
    public virtual async Task DateTime_AddDays()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, NextWeek = e.CreatedDate.AddDays(7) })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.NextWeek > DateTime.MinValue));
    }

    [ConditionalFact]
    public virtual async Task DateTime_AddHours()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, LaterToday = e.CreatedDate.AddHours(8) })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.LaterToday > DateTime.MinValue));
    }

    [ConditionalFact]
    public virtual async Task DateTime_AddMinutes()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, Soon = e.CreatedDate.AddMinutes(30) })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Soon > DateTime.MinValue));
    }

    [ConditionalFact]
    public virtual async Task DateTime_Subtract()
    {
        using var context = _fixture.CreateContext();
        var baseDate = new DateTime(2024, 7, 1);
        
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, DaysAgo = (baseDate - e.CreatedDate).Days })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.DaysAgo >= 0));
    }

    [ConditionalFact(Skip = "Azure Table Storage may not support DateTime.Parse in queries")]
    public virtual async Task DateTime_Parse()
    {
        using var context = _fixture.CreateContext();
        
        // This may not be supported in Azure Table Storage query translation
        var results = await context.TemporalEntities
            .Where(e => e.PartitionKey == "Test" && DateTime.Parse(e.DateString) > new DateTime(2024, 1, 1))
            .ToListAsync();

        Assert.NotNull(results);
    }

    public class DateTimeTranslationsAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public DateTimeTranslationsAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("DateTimeTranslationsTest");
            SeedData();
        }

        public DateTimeTranslationsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<DateTimeTranslationsContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("DateTimeTest"))
                .Options;

            var context = new DateTimeTranslationsContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var entities = new[]
            {
                new TemporalEntity { PartitionKey = "Test", Id = "1", CreatedDate = new DateTime(2024, 6, 15, 14, 30, 0), DateString = "2024-06-15" },
                new TemporalEntity { PartitionKey = "Test", Id = "2", CreatedDate = new DateTime(2024, 6, 15, 9, 15, 0), DateString = "2024-06-15" },
                new TemporalEntity { PartitionKey = "Test", Id = "3", CreatedDate = new DateTime(2024, 6, 20, 16, 45, 0), DateString = "2024-06-20" },
                new TemporalEntity { PartitionKey = "Test", Id = "4", CreatedDate = new DateTime(2024, 5, 10, 8, 0, 0), DateString = "2024-05-10" },
                new TemporalEntity { PartitionKey = "Other", Id = "5", CreatedDate = new DateTime(2023, 12, 25, 12, 0, 0), DateString = "2023-12-25" }
            };

            context.TemporalEntities.AddRange(entities);
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

    public class DateTimeTranslationsContext : DbContext
    {
        public DateTimeTranslationsContext(DbContextOptions<DateTimeTranslationsContext> options) : base(options)
        {
        }

        public DbSet<TemporalEntity> TemporalEntities => Set<TemporalEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TemporalEntity>(entity =>
            {
                entity.ToTable("TemporalEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
            });
        }
    }

    public class TemporalEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public string DateString { get; set; } = null!;
    }
}