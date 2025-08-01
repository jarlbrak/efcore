# Configuring Entities for Azure Table Storage

This article shows how to configure entities when using the Azure Table provider for Entity Framework Core.

## Automatic Key Discovery

The Azure Table provider uses conventions to automatically discover partition and row keys, following standard EF Core patterns:

```csharp
public class Product
{
    public string Id { get; set; } = null!;        // Automatically becomes RowKey
    public string Category { get; set; } = null!;  // Automatically becomes PartitionKey
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>().ToTable("Products");
    // Keys discovered automatically - no explicit configuration needed
}
```

## Key Discovery Conventions

The provider follows a logical 3-phase discovery process:

### Phase 1: Explicit Names
- Properties named `PartitionKey` → partition key
- Properties named `RowKey` → row key

### Phase 2: Standard ID Patterns
**Row Key Discovery (in precedence order):**
- `Id` property
- `{EntityName}Id` property (e.g., `ProductId`)

**Partition Key Discovery (after row key is determined):**
- Any other property ending with `Id` that wasn't used for the row key
- If no suitable property is found, creates a shadow property with the entity type name as the default value

The partition key discovery specifically excludes whatever property was chosen as the row key, ensuring no conflicts.

### Examples

```csharp
// Example 1: Simple Id + other Id property
public class Customer
{
    public string Id { get; set; }          // → RowKey (Id pattern)
    public string TenantId { get; set; }    // → PartitionKey (first other property ending with 'Id')
    public string Name { get; set; }
}

// Example 2: EntityId + other Id property  
public class Document
{
    public string DocumentId { get; set; }  // → RowKey (EntityId pattern)
    public string UserId { get; set; }      // → PartitionKey (first other property ending with 'Id')
    public string CategoryId { get; set; }  // Not used (UserId already chosen)
    public string Title { get; set; }
}

// Example 3: Order matters for partition key
public class Order
{
    public string OrderId { get; set; }     // → RowKey (EntityId pattern)
    public string CustomerId { get; set; }  // → PartitionKey (first other Id property found)
    public string TenantId { get; set; }    // Not used (CustomerId already chosen)
    public decimal Amount { get; set; }
}

// Example 4: Single Id property - automatic fallback
public class Product
{
    public string Id { get; set; }          // → RowKey (Id pattern)
    public string Name { get; set; }
    public decimal Price { get; set; }
    // → PartitionKey automatically uses entity type name "Product" as default
}

// Example 5: Explicit names override everything
public class LogEntry
{
    public string RowKey { get; set; }      // → RowKey (explicit name)
    public string PartitionKey { get; set; } // → PartitionKey (explicit name)  
    public string Id { get; set; }          // Not used (explicit names take precedence)
    public string Message { get; set; }
}
```

## Partition Key Strategies

The partition key determines how data is distributed across storage nodes. Choose a partition key strategy based on your access patterns:

### Single Partition

For small datasets, use a constant partition value:

```csharp
public class Configuration
{
    public string ConfigKey { get; set; } = null!;  // → RowKey
    public string PartitionKey { get; set; } = "Default"; // → PartitionKey
    public string Value { get; set; } = null!;
}
```

### Date-Based Partitioning

For time-series data or logs:

```csharp
public class LogEntry
{
    public string Id { get; set; } = null!;      // → RowKey
    public string System { get; set; } = null!;  // → PartitionKey
    public DateTime Date { get; set; }
    public string Message { get; set; } = null!;
}
```

### Hierarchical Partitioning

For multi-tenant data:

```csharp
public class Document
{
    public string DocumentId { get; set; } = null!; // → RowKey
    public string TenantId { get; set; } = null!;   // → PartitionKey
    public string FolderId { get; set; } = null!;
    public string Title { get; set; } = null!;
}
```

## Explicit Configuration

When conventions don't match your entity design, use explicit configuration:

```csharp
public class User
{
    public Guid UserId { get; set; } = null!;
    public string Department { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .ToTable("Users")
        .HasPartitionKey(u => u.Department)
        .HasRowKey(u => u.UserId); // Guid automatically converted to string
}
```

## Value Conversions

The provider automatically converts non-string keys to strings. This works for common types like `Guid`, `int`, `long`, etc.

## Complex Types

Complex types are automatically serialized to JSON:

```csharp
public class Order
{
    public string OrderId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public ShippingAddress Address { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}

public class ShippingAddress
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
}

modelBuilder.Entity<Order>()
    .ToTable("Orders")
    .HasPartitionKey(o => o.CustomerId)
    .HasRowKey(o => o.OrderId);
```

## Concurrency Control

The provider automatically handles optimistic concurrency using ETags. No additional configuration is required:

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Handle concurrent updates
    var entry = ex.Entries.Single();
    var databaseValues = await entry.GetDatabaseValuesAsync();
    
    // Resolve conflict...
}
```

## Shadow Properties

The provider manages system properties (ETag, Timestamp) as shadow properties. Access them when needed:

```csharp
var entry = context.Entry(product);
var etag = entry.Property("ETag").CurrentValue;
var timestamp = entry.Property("Timestamp").CurrentValue;
```

## Table Name Configuration

Configure table names using the `ToTable` method:

```csharp
// Keys discovered automatically from entity properties
modelBuilder.Entity<Customer>().ToTable("Customers");
```

You can also use prefixes for all tables:

```csharp
services.AddDbContext<MyContext>(options =>
    options.UseAzureTable(connectionString, azureOptions =>
    {
        azureOptions.TableNamePrefix("MyApp");
    }));
```

## When to Use Explicit Configuration

Use explicit configuration in these scenarios:

### Complex Key Patterns
```csharp
public class TimeSeriesData
{
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = null!;
    public double Value { get; set; }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<TimeSeriesData>()
        .ToTable("Metrics")
        .HasPartitionKey(d => d.DeviceId)
        .HasRowKey(d => d.Timestamp.ToString("yyyy-MM-dd-HH-mm-ss"));
}
```

### Non-Standard Property Names
```csharp
public class LegacyEntity
{
    public string Identifier { get; set; } = null!;
    public string GroupName { get; set; } = null!;
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<LegacyEntity>()
        .ToTable("Legacy")
        .HasPartitionKey(e => e.GroupName)
        .HasRowKey(e => e.Identifier);
}
```

### Composite Row Keys
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<OrderItem>()
        .ToTable("OrderItems")
        .HasPartitionKey(oi => oi.OrderId)
        .HasRowKey(oi => $"{oi.ProductId}_{oi.Sequence:D3}");
}
```

## Best Practices

1. **Use conventions when possible** - Reduces configuration and follows EF Core patterns
2. **Choose appropriate partition keys** - Balance between query performance and write throughput
3. **Keep entities under 1MB** - Azure Table Storage limit
4. **Use point queries when possible** - Queries with both partition key and row key are most efficient
5. **Design for your access patterns** - Partition key strategy should match how you query data
6. **Consider eventual consistency** - Queries across partitions have eventual consistency