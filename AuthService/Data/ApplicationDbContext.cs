using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AuthService.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }

    }

}
