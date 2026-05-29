using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Bar_QR.Models;

namespace Bar_QR.Data;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos { get; set; }
    public DbSet<Mesa> Mesas { get; set; }
    public DbSet<SesionMesa> SesionesMesa { get; set; }
    public DbSet<Zona> Zonas { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<StaffEmail> StaffEmails { get; set; }
    public DbSet<ProxyIp> ProxyIps { get; set; }
    public DbSet<SiteToken> SiteTokens { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<TicketPlantilla> TicketPlantillas { get; set; }
    public DbSet<TicketImagen> TicketImagenes { get; set; }
    public DbSet<PedidoMesa> PedidosMesa { get; set; }
    public DbSet<LineaPedido> LineasPedido { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Nombre).IsRequired();
            // SQLite no soporta bien "decimal(10,2)" como tipo SQL. Usamos conversi�n a double para persistencia.
            b.Property(p => p.Precio).HasConversion<double>();
            b.Property(p => p.Grupo).HasConversion<int>();
        });

        modelBuilder.Entity<Mesa>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.Slug).IsUnique();
            b.Property(m => m.Slug).IsRequired();
            b.Property(m => m.Habilitada).HasDefaultValue(true);
            b.HasOne(m => m.Zona)
             .WithMany(z => z.Mesas)
             .HasForeignKey(m => m.ZonaId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        modelBuilder.Entity<SesionMesa>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.Token).IsUnique();
            b.Property(s => s.Token).IsRequired();
            b.HasOne(s => s.Mesa)
             .WithMany()
             .HasForeignKey(s => s.MesaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PedidoMesa>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Estado).HasConversion<int>();
            b.HasOne(p => p.Mesa)
             .WithMany()
             .HasForeignKey(p => p.MesaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LineaPedido>(b =>
        {
            b.HasKey(l => l.Id);
            b.HasOne(l => l.Pedido)
             .WithMany(p => p.Lineas)
             .HasForeignKey(l => l.PedidoMesaId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(l => l.Producto)
             .WithMany()
             .HasForeignKey(l => l.ProductoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
