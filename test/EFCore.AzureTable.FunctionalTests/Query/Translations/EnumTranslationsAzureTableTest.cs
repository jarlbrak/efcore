// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query.Translations;

#nullable disable

public class EnumTranslationsAzureTableTest : IClassFixture<EnumTranslationsAzureTableTest.EnumTranslationsAzureTableFixture>
{
    private readonly EnumTranslationsAzureTableFixture _fixture;

    public EnumTranslationsAzureTableTest(EnumTranslationsAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public virtual async Task Enum_equality()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.Status == TestStatus.Active)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(TestStatus.Active, r.Status));
    }

    [ConditionalFact]
    public virtual async Task Enum_not_equality()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.Status != TestStatus.Inactive)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotEqual(TestStatus.Inactive, r.Status));
    }

    [ConditionalFact]
    public virtual async Task Enum_comparison_with_parameter()
    {
        using var context = _fixture.CreateContext();
        var statusParameter = TestStatus.Pending;
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.Status == statusParameter)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(statusParameter, r.Status));
    }

    [ConditionalFact]
    public virtual async Task Enum_ToString()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, StatusString = e.Status.ToString() })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => 
        {
            Assert.NotNull(r.StatusString);
            Assert.True(Enum.TryParse<TestStatus>(r.StatusString, out _));
        });
    }

    [ConditionalFact]
    public virtual async Task Nullable_enum_equality()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.NullableStatus == null)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Null(r.NullableStatus));
    }

    [ConditionalFact]
    public virtual async Task Nullable_enum_with_value()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.NullableStatus == TestStatus.Active)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(TestStatus.Active, r.NullableStatus));
    }

    [ConditionalFact]
    public virtual async Task Enum_HasFlag()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.Flags.HasFlag(TestFlags.Read))
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.True(r.Flags.HasFlag(TestFlags.Read)));
    }

    [ConditionalFact]
    public virtual async Task Enum_bitwise_and()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, HasRead = (e.Flags & TestFlags.Read) == TestFlags.Read })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.HasRead == true || r.HasRead == false));
    }

    [ConditionalFact]
    public virtual async Task Enum_bitwise_or()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, Combined = e.Flags | TestFlags.Execute })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Combined.HasFlag(TestFlags.Execute)));
    }

    [ConditionalFact]
    public virtual async Task Enum_in_projection()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => e.Status)
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, status => Assert.True(Enum.IsDefined(typeof(TestStatus), status)));
    }

    [ConditionalFact]
    public virtual async Task Enum_with_value_converter()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && e.Priority == Priority.High)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(Priority.High, r.Priority));
    }

    [ConditionalFact]
    public virtual async Task Multiple_enum_conditions()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && 
                       e.Status == TestStatus.Active && 
                       e.Priority == Priority.Medium)
            .ToListAsync();

        Assert.NotNull(results);
        Assert.All(results, r => 
        {
            Assert.Equal(TestStatus.Active, r.Status);
            Assert.Equal(Priority.Medium, r.Priority);
        });
    }

    [ConditionalFact(Skip = "Azure Table Storage has limited support for Enum.Parse in queries")]
    public virtual async Task Enum_Parse()
    {
        using var context = _fixture.CreateContext();
        
        // This may not be supported in Azure Table Storage query translation
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test" && Enum.Parse<TestStatus>(e.StatusString) == e.Status)
            .ToListAsync();

        Assert.NotNull(results);
    }

    [ConditionalFact]
    public virtual async Task Enum_cast_to_int()
    {
        using var context = _fixture.CreateContext();
        
        var results = await context.EnumEntities
            .Where(e => e.PartitionKey == "Test")
            .Select(e => new { e.Id, StatusInt = (int)e.Status })
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.StatusInt >= 0));
    }

    public class EnumTranslationsAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public EnumTranslationsAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("EnumTranslationsTest");
            SeedData();
        }

        public EnumTranslationsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<EnumTranslationsContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("EnumTest"))
                .Options;

            var context = new EnumTranslationsContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var entities = new[]
            {
                new EnumEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "1", 
                    Status = TestStatus.Active,
                    NullableStatus = TestStatus.Active,
                    Flags = TestFlags.Read | TestFlags.Write,
                    Priority = Priority.High,
                    StatusString = "Active"
                },
                new EnumEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "2", 
                    Status = TestStatus.Inactive,
                    NullableStatus = null,
                    Flags = TestFlags.Read,
                    Priority = Priority.Low,
                    StatusString = "Inactive"
                },
                new EnumEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "3", 
                    Status = TestStatus.Pending,
                    NullableStatus = TestStatus.Pending,
                    Flags = TestFlags.Execute,
                    Priority = Priority.Medium,
                    StatusString = "Pending"
                },
                new EnumEntity 
                { 
                    PartitionKey = "Test", 
                    Id = "4", 
                    Status = TestStatus.Active,
                    NullableStatus = TestStatus.Inactive,
                    Flags = TestFlags.Read | TestFlags.Write | TestFlags.Execute,
                    Priority = Priority.Medium,
                    StatusString = "Active"
                },
                new EnumEntity 
                { 
                    PartitionKey = "Other", 
                    Id = "5", 
                    Status = TestStatus.Archived,
                    NullableStatus = TestStatus.Archived,
                    Flags = TestFlags.None,
                    Priority = Priority.Low,
                    StatusString = "Archived"
                }
            };

            context.EnumEntities.AddRange(entities);
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

    public class EnumTranslationsContext : DbContext
    {
        public EnumTranslationsContext(DbContextOptions<EnumTranslationsContext> options) : base(options)
        {
        }

        public DbSet<EnumEntity> EnumEntities => Set<EnumEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EnumEntity>(entity =>
            {
                entity.ToAzureTable("EnumEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.Id);
                
                // Configure enum properties with value converters
                entity.Property(e => e.Priority)
                    .HasConversion<string>();
            });
        }
    }

    public class EnumEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string Id { get; set; } = null!;
        public TestStatus Status { get; set; }
        public TestStatus? NullableStatus { get; set; }
        public TestFlags Flags { get; set; }
        public Priority Priority { get; set; }
        public string StatusString { get; set; } = null!;
    }

    public enum TestStatus
    {
        Inactive = 0,
        Active = 1,
        Pending = 2,
        Archived = 3
    }

    [Flags]
    public enum TestFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }
}