using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Bar_QR.Data;

#nullable disable

namespace Bar_QR.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("Bar_QR.Models.Producto", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<string>("Categoria").HasColumnType("TEXT");

                b.Property<int>("Destino").HasColumnType("INTEGER");

                b.Property<string>("Nombre").IsRequired().HasColumnType("TEXT");

                b.Property<decimal>("Precio").HasColumnType("decimal(10,2)");

                b.HasKey("Id");

                b.ToTable("Productos");
            });

            modelBuilder.Entity("Bar_QR.Models.Mesa", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<int>("Numero").HasColumnType("INTEGER");

                b.Property<bool>("Ocupada").HasColumnType("INTEGER");

                b.HasKey("Id");

                b.ToTable("Mesas");
            });

            modelBuilder.Entity("Bar_QR.Models.ProxyIp", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<string>("IpOrCidr").IsRequired().HasColumnType("TEXT");

                b.HasKey("Id");

                b.ToTable("ProxyIps");
            });

            modelBuilder.Entity("Bar_QR.Models.SiteToken", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<string>("Token").IsRequired().HasColumnType("TEXT");

                b.HasKey("Id");

                b.ToTable("SiteTokens");
            });

            modelBuilder.Entity("Bar_QR.Models.StaffEmail", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<string>("Email").IsRequired().HasColumnType("TEXT");

                b.HasKey("Id");

                b.ToTable("StaffEmails");
            });
        }
    }
}
