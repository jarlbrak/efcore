`Microsoft.EntityFrameworkCore.AzureTable` is the EF Core database provider package for Azure Table Storage.

## Usage

Call the `UseAzureTable` method to choose the Azure Table Storage database provider for your `DbContext`. For example:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseAzureTable(connectionString);
```

## Getting started with EF Core

See [Getting started with EF Core](https://learn.microsoft.com/ef/core/get-started/overview/install) for more information about EF NuGet packages, including which to install when getting started.

## Additional documentation

See the [Azure Table Storage EF Core Database Provider documentation](../../docs/azure-table/README.md) for comprehensive guides, examples, and advanced usage patterns.

## Recent Improvements (v9.0)

### Parameter Resolution Fix
Resolved critical parameter binding issues that were blocking ASP.NET Identity integration:
- Fixed invalid OData generation from parameterized queries
- Enhanced security with execution-time parameter resolution
- Eliminated dynamic compilation vulnerabilities

### ASP.NET Identity Support
Full support for ASP.NET Identity operations:
- `RoleExistsAsync()`, `GetUsersInRoleAsync()` now work correctly
- Method parameter queries generate valid OData filters
- Complex variable queries resolve parameters properly

## Feedback

If you encounter a bug or issues with this package, you can [open an Github issue](https://github.com/dotnet/efcore/issues/new/choose). For more details, see [getting support](https://github.com/dotnet/efcore/blob/main/.github/SUPPORT.md).