using Microsoft.EntityFrameworkCore;

namespace OpinionesETL.Data.Models.DW;

public class OpinionesDwContext : DbContext
{
    public OpinionesDwContext(DbContextOptions<OpinionesDwContext> options)
        : base(options)
    {
    }

    public DbSet<DimCliente> DimClientes { get; set; }
    public DbSet<DimProducto> DimProducto { get; set; }
    public DbSet<DimFuente> DimFuente { get; set; }
    public DbSet<DimFecha> DimFecha { get; set; }
    public DbSet<DimClasificacion> DimClasificacion { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimCliente>(entity =>
        {
            entity.ToTable("Dim_Clientes");
            entity.HasKey(e => e.IdCliente);
            entity.Property(e => e.IdCliente).ValueGeneratedNever();
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255).IsUnicode(false);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Pais).HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<DimProducto>(entity =>
        {
            entity.ToTable("Dim_Productos");
            entity.HasKey(e => e.IdProducto);
            entity.Property(e => e.IdProducto).ValueGeneratedNever();
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Categoria).IsRequired().HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<DimFuente>(entity =>
        {
            entity.ToTable("Dim_Fuente");
            entity.HasKey(e => e.IdFuente);
            entity.Property(e => e.IdFuente).UseIdentityColumn();
            entity.Property(e => e.TipoFuente).IsRequired().HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<DimFecha>(entity =>
        {
            entity.ToTable("Dim_Fecha");
            entity.HasKey(e => e.IdFecha);
            entity.Property(e => e.IdFecha).UseIdentityColumn();
            entity.Property(e => e.Fecha).HasColumnType("date");
        });

        modelBuilder.Entity<DimClasificacion>(entity =>
        {
            entity.ToTable("Dim_Clasificacion");
            entity.HasKey(e => e.IdClasificacion);
            entity.Property(e => e.IdClasificacion).UseIdentityColumn();
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50).IsUnicode(false);
        });
    }
}