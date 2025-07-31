// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

/// <summary>
/// CRITICAL TEST: This test specifically validates the fix for closure variable parameter binding
/// that was failing in customer scenarios. The core issue was that EF Core's query compilation
/// creates complex expression patterns for closure variables like 'string roleName = "Admin"'
/// and our original parameter inliner was too narrow to handle these patterns.
/// </summary>
public class AzureTableParameterInliningTest : IClassFixture<AzureTableParameterInliningTest.AzureTableParameterInliningFixture>
{
    private readonly AzureTableParameterInliningFixture _fixture;

    public AzureTableParameterInliningTest(AzureTableParameterInliningFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// CRITICAL TEST: This is the exact failing scenario from the customer report.
    /// Previously this would generate invalid OData like "Name eq " instead of "Name eq 'Admin'"
    /// because the closure variable 'roleName' wasn't being resolved properly.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_closure_variable_string_equality()
    {
        using var context = _fixture.CreateContext();
        
        // CRITICAL: This pattern creates a closure variable that EF Core compiles to complex expressions
        string roleName = "Admin";
        
        var roles = await context.Roles
            .Where(r => r.Name == roleName)
            .ToListAsync();

        Assert.NotEmpty(roles);
        Assert.All(roles, r => Assert.Equal("Admin", r.Name));
    }

    /// <summary>
    /// CRITICAL TEST: Another common closure variable pattern with different data types.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_closure_variable_int_equality()
    {
        using var context = _fixture.CreateContext();
        
        int targetLevel = 5;
        
        var roles = await context.Roles
            .Where(r => r.Level == targetLevel)
            .ToListAsync();

        Assert.NotEmpty(roles);
        Assert.All(roles, r => Assert.Equal(5, r.Level));
    }

    /// <summary>
    /// CRITICAL TEST: DateTime closure variables are also commonly problematic.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_closure_variable_datetime_comparison()
    {
        using var context = _fixture.CreateContext();
        
        var cutoffDate = new DateTime(2023, 1, 1);
        
        var roles = await context.Roles
            .Where(r => r.CreatedDate >= cutoffDate)
            .ToListAsync();

        Assert.NotNull(roles);
        Assert.All(roles, r => Assert.True(r.CreatedDate >= cutoffDate));
    }

    /// <summary>
    /// CRITICAL TEST: Boolean closure variables in complex expressions.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_closure_variable_bool_with_complex_condition()
    {
        using var context = _fixture.CreateContext();
        
        bool isActive = true;
        string department = "IT";
        
        var roles = await context.Roles
            .Where(r => r.IsActive == isActive && r.Department == department)
            .ToListAsync();

        Assert.NotNull(roles);
        Assert.All(roles, r => Assert.True(r.IsActive && r.Department == "IT"));
    }

    /// <summary>
    /// CRITICAL TEST: Nested closure patterns where variables are captured in nested scopes.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_nested_closure_variables()
    {
        using var context = _fixture.CreateContext();
        
        // Create nested scope to test complex closure patterns
        string baseName = "Test";
        var roles = new List<Role>();
        
        for (int i = 1; i <= 3; i++)
        {
            string namePattern = $"{baseName}{i}";
            var foundRoles = await context.Roles
                .Where(r => r.Name.StartsWith(namePattern.Substring(0, 4))) // Complex closure usage
                .ToListAsync();
            roles.AddRange(foundRoles);
        }

        Assert.NotNull(roles);
    }

    /// <summary>
    /// CRITICAL TEST: Ensure that constant expressions (non-closure) still work correctly.
    /// This validates that our fix doesn't break existing functionality.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_constant_string_equality_still_works()
    {
        using var context = _fixture.CreateContext();
        
        var roles = await context.Roles
            .Where(r => r.Name == "Admin") // Direct constant - should still work
            .ToListAsync();

        Assert.NotEmpty(roles);
        Assert.All(roles, r => Assert.Equal("Admin", r.Name));
    }

    /// <summary>
    /// CRITICAL TEST: Mixed closure variables and constants in the same query.
    /// </summary>
    [ConditionalFact]
    public virtual async Task Where_mixed_closure_and_constant()
    {
        using var context = _fixture.CreateContext();
        
        string targetName = "Admin";
        
        var roles = await context.Roles
            .Where(r => r.Name == targetName && r.Department == "IT") // Mixed pattern
            .ToListAsync();

        Assert.NotNull(roles);
    }

    public class AzureTableParameterInliningFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public AzureTableParameterInliningFixture()
        {
            _testStore = AzureTableTestStore.Create("ParameterInliningTest");
            SeedData();
        }

        public ParameterInliningContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ParameterInliningContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ParamTest"))
                .Options;

            var context = new ParameterInliningContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var roles = new[]
            {
                new Role { Department = "IT", RoleId = "ADMIN", Name = "Admin", Level = 5, IsActive = true, CreatedDate = new DateTime(2023, 6, 1) },
                new Role { Department = "IT", RoleId = "USER", Name = "User", Level = 1, IsActive = true, CreatedDate = new DateTime(2023, 3, 15) },
                new Role { Department = "HR", RoleId = "MANAGER", Name = "Manager", Level = 3, IsActive = true, CreatedDate = new DateTime(2023, 8, 10) },
                new Role { Department = "IT", RoleId = "GUEST", Name = "Guest", Level = 0, IsActive = false, CreatedDate = new DateTime(2022, 12, 1) },
                new Role { Department = "Finance", RoleId = "ANALYST", Name = "Analyst", Level = 2, IsActive = true, CreatedDate = new DateTime(2023, 4, 20) },
                new Role { Department = "IT", RoleId = "TEST1", Name = "Test1", Level = 1, IsActive = true, CreatedDate = new DateTime(2023, 1, 1) },
                new Role { Department = "IT", RoleId = "TEST2", Name = "Test2", Level = 2, IsActive = true, CreatedDate = new DateTime(2023, 2, 1) },
                new Role { Department = "IT", RoleId = "TEST3", Name = "Test3", Level = 3, IsActive = true, CreatedDate = new DateTime(2023, 3, 1) }
            };

            context.Roles.AddRange(roles);
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

    public class ParameterInliningContext : DbContext
    {
        public ParameterInliningContext(DbContextOptions<ParameterInliningContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToAzureTable("Roles");
                entity.HasPartitionKey(e => e.Department);
                entity.HasRowKey(e => e.RoleId);
            });
        }
    }

    public class Role
    {
        public string Department { get; set; } = null!;
        public string RoleId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}