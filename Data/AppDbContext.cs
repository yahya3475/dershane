using Microsoft.EntityFrameworkCore;
using dershane.Models;

namespace dershane.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        // Tablo gibi çalışacak model
        public DbSet<User> users { get; set; }
    }
}
