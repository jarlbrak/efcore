using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TestParameterIssue
{
    // Simple test to reproduce the parameter binding issue
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Testing Azure Table parameter binding issue...");
            
            var options = new DbContextOptionsBuilder<TestContext>()
                .UseAzureTable("UseDevelopmentStorage=true")
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine)
                .Options;

            using var context = new TestContext(options);
            await context.Database.EnsureCreatedAsync();

            // Seed test data
            context.Roles.Add(new Role { Country = "Global", RoleId = "ADMIN", Name = "Admin" });
            context.Roles.Add(new Role { Country = "Global", RoleId = "USER", Name = "User" });
            await context.SaveChangesAsync();

            Console.WriteLine("\n=== Testing literal query (should work) ===");
            try
            {
                var literalResult = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Admin");
                Console.WriteLine($"Literal query result: {literalResult?.Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Literal query failed: {ex.Message}");
            }

            Console.WriteLine("\n=== Testing variable query (currently broken) ===");
            try
            {
                string roleName = "Admin";
                var variableResult = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName);
                Console.WriteLine($"Variable query result: {variableResult?.Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Variable query failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }
        
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToAzureTable("TestRoles");
                entity.HasPartitionKey(e => e.Country);
                entity.HasRowKey(e => e.RoleId);
            });
        }
    }

    public class Role
    {
        public string Country { get; set; } = null!;
        public string RoleId { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}