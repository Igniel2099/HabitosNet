using HabitosNet.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitosNet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DailyRegister> DailyRegisters => Set<DailyRegister>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DailyRegister>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Date).IsUnique();
        });
    }
}
