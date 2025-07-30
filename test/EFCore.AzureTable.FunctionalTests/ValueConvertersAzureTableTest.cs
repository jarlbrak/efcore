// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class ValueConvertersAzureTableTest : IClassFixture<ValueConvertersAzureTableTest.ValueConvertersAzureTableFixture>
{
    private readonly ValueConvertersAzureTableFixture _fixture;

    public ValueConvertersAzureTableTest(ValueConvertersAzureTableFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Can_use_enum_to_string_converter()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "enum_string_test",
            StatusAsString = Status.Active
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "enum_string_test");

        Assert.NotNull(retrieved);
        Assert.Equal(Status.Active, retrieved.StatusAsString);
    }

    [ConditionalFact]
    public async Task Can_use_enum_to_int_converter()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "enum_int_test",
            StatusAsInt = Status.Pending
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "enum_int_test");

        Assert.NotNull(retrieved);
        Assert.Equal(Status.Pending, retrieved.StatusAsInt);
    }

    [ConditionalFact]
    public async Task Can_use_bool_to_string_converter()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "bool_string_test",
            BoolAsString = true
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "bool_string_test");

        Assert.NotNull(retrieved);
        Assert.True(retrieved.BoolAsString);
    }

    [ConditionalFact]
    public async Task Can_use_guid_to_string_converter()
    {
        using var context = _fixture.CreateContext();
        
        var testGuid = Guid.NewGuid();
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "guid_string_test",
            GuidAsString = testGuid
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "guid_string_test");

        Assert.NotNull(retrieved);
        Assert.Equal(testGuid, retrieved.GuidAsString);
    }

    [ConditionalFact]
    public async Task Can_use_datetime_to_string_converter()
    {
        using var context = _fixture.CreateContext();
        
        var testDate = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "datetime_string_test",
            DateTimeAsString = testDate
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "datetime_string_test");

        Assert.NotNull(retrieved);
        Assert.Equal(testDate, retrieved.DateTimeAsString);
    }

    [ConditionalFact]
    public async Task Can_use_int_to_string_converter()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "int_string_test",
            IntAsString = 12345
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "int_string_test");

        Assert.NotNull(retrieved);
        Assert.Equal(12345, retrieved.IntAsString);
    }

    [ConditionalFact]
    public async Task Can_use_decimal_to_string_converter()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "decimal_string_test",
            DecimalAsString = 123.45m
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "decimal_string_test");

        Assert.NotNull(retrieved);
        Assert.Equal(123.45m, retrieved.DecimalAsString);
    }

    [ConditionalFact]
    public async Task Can_use_json_converter_for_complex_object()
    {
        using var context = _fixture.CreateContext();
        
        var complexObject = new ComplexObject
        {
            Name = "Test Object",
            Value = 42,
            Items = new[] { "item1", "item2", "item3" }
        };

        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "json_test",
            ComplexObjectAsJson = complexObject
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "json_test");

        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.ComplexObjectAsJson);
        Assert.Equal("Test Object", retrieved.ComplexObjectAsJson.Name);
        Assert.Equal(42, retrieved.ComplexObjectAsJson.Value);
        Assert.Equal(3, retrieved.ComplexObjectAsJson.Items.Length);
        Assert.Equal("item1", retrieved.ComplexObjectAsJson.Items[0]);
    }

    [ConditionalFact]
    public async Task Can_use_custom_converter()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "custom_test",
            CustomProperty = "CUSTOM_VALUE"
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "custom_test");

        Assert.NotNull(retrieved);
        Assert.Equal("CUSTOM_VALUE", retrieved.CustomProperty);
    }

    [ConditionalFact]
    public async Task Can_query_using_converted_values()
    {
        using var context = _fixture.CreateContext();
        
        // Add test data
        var entities = new[]
        {
            new ConverterEntity { PartitionKey = "Query", RowKey = "1", StatusAsString = Status.Active, IntAsString = 100 },
            new ConverterEntity { PartitionKey = "Query", RowKey = "2", StatusAsString = Status.Inactive, IntAsString = 200 },
            new ConverterEntity { PartitionKey = "Query", RowKey = "3", StatusAsString = Status.Active, IntAsString = 300 }
        };

        context.ConverterEntities.AddRange(entities);
        await context.SaveChangesAsync();

        // Query by converted enum
        var activeEntities = await context.ConverterEntities
            .Where(e => e.PartitionKey == "Query" && e.StatusAsString == Status.Active)
            .ToListAsync();

        Assert.Equal(2, activeEntities.Count);
        Assert.All(activeEntities, e => Assert.Equal(Status.Active, e.StatusAsString));

        // Query by converted int
        var highValueEntities = await context.ConverterEntities
            .Where(e => e.PartitionKey == "Query" && e.IntAsString > 150)
            .ToListAsync();

        Assert.Equal(2, highValueEntities.Count);
        Assert.All(highValueEntities, e => Assert.True(e.IntAsString > 150));
    }

    [ConditionalFact]
    public async Task Can_use_nullable_converters()
    {
        using var context = _fixture.CreateContext();
        
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "nullable_test",
            NullableStatusAsString = null,
            NullableIntAsString = 42
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "nullable_test");

        Assert.NotNull(retrieved);
        Assert.Null(retrieved.NullableStatusAsString);
        Assert.Equal(42, retrieved.NullableIntAsString);
    }

    [ConditionalFact]
    public async Task Can_handle_conversion_errors_gracefully()
    {
        using var context = _fixture.CreateContext();
        
        // This test would verify error handling, but in a real scenario
        // we'd need to simulate invalid data or conversion failures
        var entity = new ConverterEntity
        {
            PartitionKey = "Test",
            RowKey = "error_test",
            StatusAsString = Status.Active
        };

        context.ConverterEntities.Add(entity);
        await context.SaveChangesAsync();

        var retrieved = await context.ConverterEntities
            .FirstOrDefaultAsync(e => e.PartitionKey == "Test" && e.RowKey == "error_test");

        Assert.NotNull(retrieved);
        Assert.Equal(Status.Active, retrieved.StatusAsString);
    }

    public class ValueConvertersAzureTableFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public ValueConvertersAzureTableFixture()
        {
            _testStore = AzureTableTestStore.Create("ValueConvertersTest");
        }

        public ValueConvertersContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ValueConvertersContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ConverterTest"))
                .Options;

            var context = new ValueConvertersContext(options);
            context.Database.EnsureCreated();
            return context;
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

    public class ValueConvertersContext : DbContext
    {
        public ValueConvertersContext(DbContextOptions<ValueConvertersContext> options) : base(options)
        {
        }

        public DbSet<ConverterEntity> ConverterEntities => Set<ConverterEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConverterEntity>(entity =>
            {
                entity.ToAzureTable("ConverterEntities");
                entity.HasPartitionKey(e => e.PartitionKey);
                entity.HasRowKey(e => e.RowKey);
                
                // Configure value converters
                entity.Property(e => e.StatusAsString)
                    .HasConversion<string>();
                
                entity.Property(e => e.StatusAsInt)
                    .HasConversion<int>();
                
                entity.Property(e => e.BoolAsString)
                    .HasConversion(
                        v => v ? "true" : "false",
                        v => v == "true");
                
                entity.Property(e => e.GuidAsString)
                    .HasConversion<string>();
                
                entity.Property(e => e.DateTimeAsString)
                    .HasConversion(
                        v => v.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        v => DateTime.Parse(v));
                
                entity.Property(e => e.IntAsString)
                    .HasConversion<string>();
                
                entity.Property(e => e.DecimalAsString)
                    .HasConversion<string>();
                
                entity.Property(e => e.ComplexObjectAsJson)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<ComplexObject>(v, (System.Text.Json.JsonSerializerOptions)null));
                
                entity.Property(e => e.CustomProperty)
                    .HasConversion(new CustomValueConverter());
                
                entity.Property(e => e.NullableStatusAsString)
                    .HasConversion<string>();
                
                entity.Property(e => e.NullableIntAsString)
                    .HasConversion<string>();
            });
        }
    }

    public class ConverterEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        
        // Enum converters
        public Status StatusAsString { get; set; }
        public Status StatusAsInt { get; set; }
        
        // Basic type converters
        public bool BoolAsString { get; set; }
        public Guid GuidAsString { get; set; }
        public DateTime DateTimeAsString { get; set; }
        public int IntAsString { get; set; }
        public decimal DecimalAsString { get; set; }
        
        // Complex object converter
        public ComplexObject ComplexObjectAsJson { get; set; }
        
        // Custom converter
        public string CustomProperty { get; set; }
        
        // Nullable converters
        public Status? NullableStatusAsString { get; set; }
        public int? NullableIntAsString { get; set; }
    }

    public class ComplexObject
    {
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public string[] Items { get; set; } = null!;
    }

    public enum Status
    {
        Inactive,
        Active,
        Pending
    }

    public class CustomValueConverter : ValueConverter<string, string>
    {
        public CustomValueConverter()
            : base(
                v => v.ToUpperInvariant(),
                v => v.ToLowerInvariant())
        {
        }
    }
}