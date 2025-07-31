# Azure Table Storage EF Core Database Provider

The Azure Table Storage provider for Entity Framework Core allows you to use EF Core with Azure Table Storage, providing a familiar ORM experience while leveraging the scalability and performance of Azure's NoSQL table storage service.

## Quick Start

Install the provider package:

```bash
dotnet add package Microsoft.EntityFrameworkCore.AzureTable
```

Configure your DbContext:

```csharp
services.AddDbContext<MyDbContext>(options =>
    options.UseAzureTable(connectionString));
```

Define your entities:

```csharp
public class Customer
{
    public string Region { get; set; } = null!;     // Partition Key
    public string CustomerId { get; set; } = null!; // Row Key
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public string? ETag { get; set; }               // Concurrency token
}
```

Use familiar EF Core patterns:

```csharp
var customers = await context.Customers
    .Where(c => c.Region == "West")
    .ToListAsync();
```

## Documentation

- **[Getting Started](getting-started.md)** - Complete setup guide with examples
- **[Limitations](limitations.md)** - Important limitations and workarounds
- **[Configuration](configuration.md)** - Advanced configuration options
- **[Best Practices](best-practices.md)** - Performance and design recommendations
- **[Migration Guide](migration.md)** - Migrating from raw Azure Table SDK

## Key Features

### Core Capabilities
- ✅ LINQ query translation to OData filters
- ✅ Change tracking and SaveChanges support
- ✅ Optimistic concurrency with ETags
- ✅ Bulk operations within partitions
- ✅ Complex type serialization (JSON)
- ✅ Comprehensive type mapping system

### Recent Improvements (v9.0)
- ✅ **Parameter Resolution Fix** - Resolved critical ASP.NET Identity integration issues
- ✅ **Enhanced Security** - Eliminated dynamic compilation vulnerabilities
- ✅ **Performance Optimizations** - Execution-time parameter resolution
- ✅ **Robust Error Handling** - Comprehensive validation and diagnostics

## Important Considerations

### Strengths
- Massive scale and performance
- Cost-effective for large datasets
- Automatic partitioning and load balancing
- Strong consistency for point queries
- Global distribution capabilities

### Limitations
- No relationships or foreign keys
- Limited query capabilities (no joins, complex aggregations)
- Eventual consistency for filtered queries
- Transaction scope limited to single partition
- Maximum entity size of 1MB

## Supported Scenarios

### ✅ Excellent For
- High-scale web applications
- IoT data collection and telemetry
- User profiles and preferences
- Audit logs and event tracking
- Content management systems
- Simple CRUD operations

### ⚠️ Consider Alternatives For
- Complex relational data models
- Advanced querying with joins and aggregations
- Strong consistency requirements across partitions
- Applications requiring ACID transactions across multiple entities

## Community and Support

- **Issues**: [GitHub Issues](https://github.com/dotnet/efcore/issues)
- **Discussions**: [GitHub Discussions](https://github.com/dotnet/efcore/discussions)
- **Documentation**: [Microsoft Learn](https://learn.microsoft.com/ef/core/)

## Contributing

This provider is part of the Entity Framework Core project. See the [EF Core contribution guidelines](https://github.com/dotnet/efcore/blob/main/CONTRIBUTING.md) for information on contributing to the project.