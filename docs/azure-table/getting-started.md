# Getting Started with Entity Framework Core Azure Table Provider

This guide provides step-by-step instructions for getting started with the Entity Framework Core Azure Table Storage provider.

## Prerequisites

- .NET 8.0 or later
- Azure Storage Account or Azure Storage Emulator/Azurite for development
- Visual Studio 2022 or VS Code with C# extension

## Installation

Install the Azure Table provider package:

```bash
dotnet add package Microsoft.EntityFrameworkCore.AzureTable
```

## Basic Configuration

### 1. Configure Connection String

Add your Azure Table Storage connection string to your configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DefaultEndpointsProtocol=https;AccountName=your-account;AccountKey=your-key;EndpointSuffix=core.windows.net"
  }
}
```

For development with Azurite:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "UseDevelopmentStorage=true"
  }
}
```

### 2. Define Your Entities

Create your entity classes using standard EF Core conventions:

```csharp
public class Customer
{
    public string Id { get; set; } = null!;        // Automatically becomes RowKey
    public string TenantId { get; set; } = null!;  // Automatically becomes PartitionKey
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
}

public class Order
{
    public string OrderId { get; set; } = null!;   // Automatically becomes RowKey
    public string CustomerId { get; set; } = null!; // Automatically becomes PartitionKey
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered
}
```

### 3. Create DbContext

Configure your DbContext to use Azure Table Storage:

```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Simple table configuration - keys are discovered automatically
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Order>().ToTable("Orders");
    }
}
```

### 4. Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add EF Core with Azure Table Storage
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseAzureTable(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        azureOptions =>
        {
            azureOptions.TableNamePrefix("MyApp"); // Optional prefix
        }));

var app = builder.Build();
```

## Basic Operations

### Create (Insert)

```csharp
public async Task CreateCustomer(Customer customer)
{
    _context.Customers.Add(customer);
    await _context.SaveChangesAsync();
}
```

### Read (Query)

```csharp
// Point query (most efficient)
public async Task<Customer?> GetCustomer(string tenantId, string id)
{
    return await _context.Customers
        .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id);
}

// Partition query
public async Task<List<Customer>> GetCustomersByTenant(string tenantId)
{
    return await _context.Customers
        .Where(c => c.TenantId == tenantId)
        .ToListAsync();
}

// Filtered query
public async Task<List<Customer>> GetRecentCustomers(string tenantId, DateTime since)
{
    return await _context.Customers
        .Where(c => c.TenantId == tenantId && c.CreatedDate > since)
        .ToListAsync();
}
```

### Update

```csharp
public async Task UpdateCustomer(string tenantId, string id, string newEmail)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id);
    
    if (customer != null)
    {
        customer.Email = newEmail;
        await _context.SaveChangesAsync();
    }
}
```

### Delete

```csharp
public async Task DeleteCustomer(string tenantId, string id)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id);
    
    if (customer != null)
    {
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}
```

## Advanced Features

### Bulk Operations

Use the bulk operation extensions for better performance:

```csharp
public async Task BulkInsertCustomers(List<Customer> customers)
{
    // Group by partition key for bulk operations
    var partitionGroups = customers.GroupBy(c => c.TenantId);
    
    foreach (var group in partitionGroups)
    {
        var tableClient = new TableClient(connectionString, "MyAppCustomers");
        await tableClient.BulkInsertAsync(
            group,
            group.Key,
            c => c.Id,
            c => new TableEntity(c.TenantId, c.Id)
            {
                ["Name"] = c.Name,
                ["Email"] = c.Email,
                ["CreatedDate"] = c.CreatedDate
            });
    }
}
```

### Complex Types (JSON Serialization)

```csharp
public class Customer
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Address Address { get; set; } = null!; // Will be JSON serialized
}

public class Address
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string ZipCode { get; set; } = null!;
}
```

### Optimistic Concurrency

```csharp
public async Task UpdateCustomerWithConcurrency(Customer customer)
{
    try
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // Handle concurrency conflict
        // Reload entity and retry, or inform user
        throw new InvalidOperationException("Customer was modified by another process");
    }
}
```

## ASP.NET Identity Integration

The Azure Table provider now fully supports ASP.NET Identity with resolved parameter binding issues:

```csharp
// Configure ASP.NET Identity with Azure Table Storage
services.AddDbContext<IdentityDbContext>(options =>
    options.UseAzureTable(connectionString));

services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<IdentityDbContext>();

// Identity operations now work correctly
public class AccountController : Controller
{
    public async Task<IActionResult> CreateRole(string roleName)
    {
        // This now works - parameter binding is resolved correctly
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
        return Ok();
    }
    
    public async Task<IActionResult> GetUsersInRole(string roleName)
    {
        // Complex parameter queries now generate valid OData
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return Json(users);
    }
}
```

## Recent Improvements (v9.0)

### Parameter Resolution Enhancement
The provider now correctly resolves parameters in complex queries, fixing critical issues with:

- **ASP.NET Identity Integration**: `RoleExistsAsync`, `GetUsersInRoleAsync`, and other Identity operations
- **Variable Queries**: Method parameters are now properly resolved to valid OData filters
- **Complex Expressions**: Multi-parameter queries with variables work correctly

### Security Hardening
- Eliminated dynamic expression compilation vulnerabilities
- Enhanced reflection security with trusted assembly validation
- Removed cache poisoning attack vectors
- Added multi-layer security validation

## Performance Tips

1. **Use Point Queries**: Always specify both partition key and row key when possible
2. **Avoid Cross-Partition Queries**: Filter by partition key to avoid performance penalties
3. **Use Bulk Operations**: For inserting/updating multiple entities in the same partition
4. **Optimize Partition Strategy**: Design partition keys for even distribution and query patterns
5. **Limit Result Sets**: Use `Take()` to limit query results
6. **Consider Denormalization**: Azure Table doesn't support relationships, so denormalize when needed

## Next Steps

- [Configuration Guide](azure-table-provider-configuration.md)
- [Limitations and Workarounds](azure-table-provider-limitations.md)
- [Migration from Raw Azure Table SDK](azure-table-provider-migration.md)
- [Best Practices](azure-table-provider-best-practices.md)