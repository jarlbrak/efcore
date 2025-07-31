# Azure Table Provider Limitations and Workarounds

This document outlines the limitations of the Entity Framework Core Azure Table provider and provides workarounds for common scenarios.

## Core Limitations

### 1. No Relationships / Navigation Properties

**Limitation**: Azure Table Storage doesn't support foreign keys or relationships between entities.

**Impact**: 
- No `Include()` operations
- No navigation properties
- No cascade deletes
- No referential integrity

**Workarounds**:

#### Client-Side Joins
```csharp
// Instead of: customer.Orders (navigation property)
// Do this:
var customer = await context.Customers.FirstAsync(c => c.Id == customerId);
var orders = await context.Orders.Where(o => o.CustomerId == customerId).ToListAsync();
```

#### Denormalization
```csharp
public class Order
{
    public string CustomerId { get; set; } = null!;        // Partition Key
    public string OrderId { get; set; } = null!;           // Row Key
    
    // Denormalized customer data
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
}
```

#### Lookup Tables
```csharp
// Create separate index tables for lookups
public class CustomerOrderIndex
{
    public string CustomerId { get; set; } = null!;    // Partition Key
    public string OrderId { get; set; } = null!;       // Row Key
    public DateTime OrderDate { get; set; }
}
```

### 2. Limited Query Capabilities

**Limitation**: Azure Table supports only basic OData filtering.

**Unsupported Operations**:
- `Join`
- `GroupBy`
- `Having`
- Complex `Select` projections
- Subqueries
- Aggregations (except `Count`)

**Performance Warnings**:
- Cross-partition queries are slow and expensive
- Queries without partition key filters scan entire tables

**Workarounds**:

#### Pre-compute Aggregations
```csharp
public class OrderSummary
{
    public string CustomerId { get; set; } = null!;     // Partition Key
    public string Period { get; set; } = null!;         // Row Key (e.g., "2024-01")
    
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
}

// Update summaries when orders change
```

#### Use Multiple Queries
```csharp
// Instead of complex joins, use multiple targeted queries
public async Task<CustomerOrderSummary> GetCustomerSummary(string customerId)
{
    var customer = await context.Customers
        .FirstAsync(c => c.CustomerId == customerId);
    
    var orders = await context.Orders
        .Where(o => o.CustomerId == customerId)
        .ToListAsync();
    
    return new CustomerOrderSummary
    {
        Customer = customer,
        OrderCount = orders.Count,
        TotalAmount = orders.Sum(o => o.Amount)
    };
}
```

### 3. Type System Limitations

**Supported Types**:
- `string`, `int`, `long`, `double`, `bool`
- `DateTime`, `DateTimeOffset`, `Guid`
- `byte[]` (max 64KB)

**Unsupported Types**:
- Collections (`List<T>`, `T[]`, `Dictionary<K,V>`)
- `decimal` (converted to `double` with precision loss)
- `TimeSpan` (convert to `long` ticks)

**Workarounds**:

#### Collection Storage
```csharp
public class Customer
{
    public string Region { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    
    // Store as JSON string
    public string TagsJson { get; set; } = "[]";
    
    // Computed property (not mapped)
    [NotMapped]
    public List<string> Tags
    {
        get => JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new();
        set => TagsJson = JsonSerializer.Serialize(value);
    }
}

// Configure in OnModelCreating
modelBuilder.Entity<Customer>()
    .Ignore(c => c.Tags);
```

#### Decimal Precision
```csharp
public class Product
{
    public string Category { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    
    // Store decimal as long (cents)
    public long PriceCents { get; set; }
    
    [NotMapped]
    public decimal Price
    {
        get => PriceCents / 100m;
        set => PriceCents = (long)(value * 100);
    }
}
```

### 4. Transaction Limitations

**Limitations**:
- Transactions only work within a single partition
- Maximum 100 operations per transaction
- Maximum 4MB per transaction
- No cross-table transactions

**Workarounds**:

#### Saga Pattern
```csharp
public class OrderProcessingSaga
{
    public async Task ProcessOrder(Order order)
    {
        // Step 1: Reserve inventory
        var reservationResult = await ReserveInventory(order);
        if (!reservationResult.Success)
        {
            // Handle failure
            return;
        }

        try
        {
            // Step 2: Process payment
            var paymentResult = await ProcessPayment(order);
            if (!paymentResult.Success)
            {
                // Compensate: Release inventory
                await ReleaseInventory(order);
                return;
            }

            // Step 3: Confirm order
            await ConfirmOrder(order);
        }
        catch
        {
            // Compensate: Release inventory and refund
            await ReleaseInventory(order);
            await RefundPayment(order);
            throw;
        }
    }
}
```

#### Event Sourcing
```csharp
public class OrderEvent
{
    public string OrderId { get; set; } = null!;        // Partition Key
    public string EventId { get; set; } = null!;        // Row Key
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

// Build current state from events
public class OrderAggregate
{
    public static Order BuildFromEvents(List<OrderEvent> events)
    {
        var order = new Order();
        foreach (var evt in events.OrderBy(e => e.Timestamp))
        {
            ApplyEvent(order, evt);
        }
        return order;
    }
}
```

### 5. Consistency Model

**Limitation**: Azure Table provides eventual consistency for queries.

**Impact**:
- Recently written data might not appear in queries immediately
- Different queries might return different results during propagation

**Workarounds**:

#### Point Queries for Strong Consistency
```csharp
// Point queries (PartitionKey + RowKey) are strongly consistent
var customer = await context.Customers
    .FirstOrDefaultAsync(c => c.Region == region && c.CustomerId == customerId);
```

#### Retry Logic
```csharp
public async Task<Customer?> GetCustomerWithRetry(string region, string customerId, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Region == region && c.CustomerId == customerId);
        
        if (customer != null || attempt == maxRetries - 1)
        {
            return customer;
        }
        
        await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
    }
    
    return null;
}
```

## Size Limitations

### Entity Size Limits
- Maximum entity size: 1MB
- Maximum property size: 64KB for strings and binary
- Maximum properties per entity: 255

### Performance Considerations
- Queries without partition key filters are expensive
- Cross-partition queries can be very slow
- Large result sets should use paging

**Workarounds**:

#### Large Property Storage
```csharp
public class Document
{
    public string Category { get; set; } = null!;
    public string DocumentId { get; set; } = null!;
    public string Title { get; set; } = null!;
    
    // Store large content in Blob Storage
    public string ContentBlobUrl { get; set; } = null!;
    
    // Keep metadata in table
    public int ContentLength { get; set; }
    public string ContentType { get; set; } = null!;
}
```

#### Property Splitting
```csharp
public class LargeEntity
{
    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;
    
    // Split large content across multiple properties
    public string Content1 { get; set; } = null!;
    public string Content2 { get; set; } = null!;
    public string Content3 { get; set; } = null!;
    
    [NotMapped]
    public string FullContent
    {
        get => Content1 + Content2 + Content3;
        set
        {
            const int chunkSize = 60000; // Leave room for JSON overhead
            Content1 = value.Length > 0 ? value.Substring(0, Math.Min(chunkSize, value.Length)) : "";
            Content2 = value.Length > chunkSize ? value.Substring(chunkSize, Math.Min(chunkSize, value.Length - chunkSize)) : "";
            Content3 = value.Length > chunkSize * 2 ? value.Substring(chunkSize * 2) : "";
        }
    }
}
```

## Best Practices

1. **Design partition keys carefully** - They determine scalability and query performance
2. **Use point queries when possible** - They're fastest and most efficient
3. **Denormalize data** - Azure Table works best with denormalized, self-contained entities
4. **Plan for eventual consistency** - Use point queries for strong consistency needs
5. **Monitor performance** - Use the built-in performance counters and diagnostics
6. **Consider hybrid architectures** - Use Azure Table for simple operations, other services for complex queries

## Alternative Patterns

When Azure Table limitations become too restrictive, consider:

1. **CQRS (Command Query Responsibility Segregation)**: Use Azure Table for writes, other services for complex reads
2. **Event Sourcing**: Store events in Azure Table, build read models elsewhere
3. **Hybrid Storage**: Use Azure Table for hot data, other storage for cold data or complex queries
4. **Microservices**: Break down complex domains into smaller services with simpler data needs