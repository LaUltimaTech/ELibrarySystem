using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ELibrarySystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        //public DbSet<UserMaster> UserMasters { get; set; }





    }
}
