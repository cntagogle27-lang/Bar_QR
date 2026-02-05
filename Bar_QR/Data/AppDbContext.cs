using Microsoft.EntityFrameworkCore;
using Bar_QR.Models;

namespace Bar_QR.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos { get; set; }
    public DbSet<Mesa> Mesas { get; set; }
    public DbSet<StaffEmail> StaffEmails { get; set; }
    public DbSet<ProxyIp> ProxyIps { get; set; }
    public DbSet<SiteToken> SiteTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Nombre).IsRequired();
            b.Property(p => p.Precio).HasColumnType("decimal(10,2)");
        });

        base.OnModelCreating(modelBuilder);
    }
}
