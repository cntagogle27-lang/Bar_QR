using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Bar_QR.Models;

namespace Bar_QR.Data;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos { get; set; }
    public DbSet<Mesa> Mesas { get; set; }
    public DbSet<StaffEmail> StaffEmails { get; set; }
    public DbSet<ProxyIp> ProxyIps { get; set; }
    public DbSet<SiteToken> SiteTokens { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Nombre).IsRequired();
            // SQLite no soporta bien "decimal(10,2)" como tipo SQL. Usamos conversión a double para persistencia.
            b.Property(p => p.Precio).HasConversion<double>();
        });

        modelBuilder.Entity<Mesa>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.Slug).IsUnique();
            b.Property(m => m.Slug).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
