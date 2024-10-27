using Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    
    public DbSet<Ticket> Tickets { get; set; }
}
