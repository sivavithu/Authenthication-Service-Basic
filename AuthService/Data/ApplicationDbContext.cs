using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using AuthService.Models;


namespace AuthService.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }

    }

}
