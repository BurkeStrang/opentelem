using Microsoft.EntityFrameworkCore;
using opentelem.Models;

namespace opentelem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
}
