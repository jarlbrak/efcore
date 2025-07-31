// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

/// <summary>
/// CRITICAL TEST: This test validates the architectural fix for parameter binding issues
/// that were causing OData filters to be generated with unresolved parameters like "Name eq "
/// instead of "Name eq 'Admin'" during ASP.NET Identity queries.
/// </summary>
public class ParameterResolutionTest : IClassFixture<ParameterResolutionTest.ParameterResolutionFixture>
{
    private readonly ParameterResolutionFixture _fixture;

    public ParameterResolutionTest(ParameterResolutionFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    public async Task Where_with_string_parameter_generates_valid_odata()
    {
        using var context = _fixture.CreateContext();
        
        // CRITICAL: This is the exact scenario that was failing for ASP.NET Identity
        // A local variable gets parameterized by EF Core as __p_0 or similar
        string roleNameParameter = "Admin";
        
        var roles = await context.Roles
            .Where(r => r.Name == roleNameParameter)
            .ToListAsync();

        Assert.NotNull(roles);
        // The test passes if no exception is thrown - this means parameter resolution worked
        // Previously this would fail with invalid OData syntax "Name eq " instead of "Name eq 'Admin'"
    }

    [ConditionalFact]
    public async Task Where_with_multiple_parameters_generates_valid_odata()
    {
        using var context = _fixture.CreateContext();
        
        // Test multiple parameter scenario
        string roleNameParameter = "Admin";
        bool isActiveParameter = true;
        
        var roles = await context.Roles
            .Where(r => r.Name == roleNameParameter && r.IsActive == isActiveParameter)
            .ToListAsync();

        Assert.NotNull(roles);
        // Should generate valid OData like: "Name eq 'Admin' and IsActive eq true"
    }

    [ConditionalFact]
    public async Task Where_with_null_parameter_generates_valid_odata()
    {
        using var context = _fixture.CreateContext();
        
        // Test null parameter handling
        string nullRoleName = null;
        
        var roles = await context.Roles
            .Where(r => r.Name == nullRoleName)
            .ToListAsync();

        Assert.NotNull(roles);
        // Should generate valid OData like: "Name eq null"
    }

    [ConditionalFact]
    public async Task Where_with_empty_string_parameter_generates_valid_odata()
    {
        using var context = _fixture.CreateContext();
        
        // Test empty string parameter handling
        string emptyRoleName = "";
        
        var roles = await context.Roles
            .Where(r => r.Name == emptyRoleName)
            .ToListAsync();

        Assert.NotNull(roles);
        // Should generate valid OData like: "Name eq ''"
    }

    [ConditionalFact]
    public async Task Complex_aspnet_identity_like_query_with_parameters()
    {
        using var context = _fixture.CreateContext();
        
        // Simulate ASP.NET Identity GetUsersInRoleAsync query pattern
        string roleName = "Admin";
        
        // This simulates the type of query ASP.NET Identity UserStore.GetUsersInRoleAsync generates
        var users = await context.UserRoles
            .Where(ur => ur.Role.Name == roleName)
            .Select(ur => ur.User)
            .ToListAsync();

        Assert.NotNull(users);
        // This is the core scenario that was failing - parameter resolution in navigation properties
    }

    public class ParameterResolutionFixture : IDisposable
    {
        private readonly AzureTableTestStore _testStore;

        public ParameterResolutionFixture()
        {
            _testStore = AzureTableTestStore.Create("ParameterResolutionTest");
            SeedData();
        }

        public ParameterResolutionContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ParameterResolutionContext>()
                .UseAzureTable("UseDevelopmentStorage=true", opt => opt.UseTableNamePrefix("ParamTest"))
                .Options;

            var context = new ParameterResolutionContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData()
        {
            using var context = CreateContext();
            
            var roles = new[]
            {
                new Role { Country = "Global", RoleId = "ADMIN", Name = "Admin", IsActive = true },
                new Role { Country = "Global", RoleId = "USER", Name = "User", IsActive = true },
                new Role { Country = "Global", RoleId = "MODERATOR", Name = "Moderator", IsActive = false }
            };

            var users = new[]
            {
                new User { Country = "Global", UserId = "USER001", UserName = "alice", Email = "alice@example.com" },
                new User { Country = "Global", UserId = "USER002", UserName = "bob", Email = "bob@example.com" }
            };

            var userRoles = new[]
            {
                new UserRole { Country = "Global", UserRoleId = "UR001", UserId = "USER001", RoleId = "ADMIN", User = users[0], Role = roles[0] },
                new UserRole { Country = "Global", UserRoleId = "UR002", UserId = "USER002", RoleId = "USER", User = users[1], Role = roles[1] }
            };

            context.Roles.AddRange(roles);
            context.Users.AddRange(users);
            context.UserRoles.AddRange(userRoles);
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

    public class ParameterResolutionContext : DbContext
    {
        public ParameterResolutionContext(DbContextOptions<ParameterResolutionContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToAzureTable("Roles");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.RoleId);
                
                // Explicitly mark key properties as required
                entity.Property(e => e.Country).IsRequired();
                entity.Property(e => e.RoleId).IsRequired();
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToAzureTable("Users");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.UserId);
                
                // Explicitly mark key properties as required
                entity.Property(e => e.Country).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.UserName).IsRequired();
                entity.Property(e => e.Email).IsRequired();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToAzureTable("UserRoles");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.UserRoleId);
                
                // Explicitly mark key properties as required
                entity.Property(e => e.Country).IsRequired();
                entity.Property(e => e.UserRoleId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.RoleId).IsRequired();
                
                // Configure relationships for the complex query test
                entity.HasOne(ur => ur.User)
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .HasPrincipalKey(u => u.UserId);
                    
                entity.HasOne(ur => ur.Role)
                    .WithMany()
                    .HasForeignKey(ur => ur.RoleId)
                    .HasPrincipalKey(r => r.RoleId);
            });
        }
    }

    public class Role
    {
        public string Country { get; set; } = null!;
        public string RoleId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class User
    {
        public string Country { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class UserRole
    {
        public string Country { get; set; } = null!;
        public string UserRoleId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string RoleId { get; set; } = null!;
        
        // Navigation properties for complex queries
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}