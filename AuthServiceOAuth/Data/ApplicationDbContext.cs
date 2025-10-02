using Microsoft.EntityFrameworkCore;
using OAuthAuthService.Entities;

namespace OAuthAuthService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        
    }
}
