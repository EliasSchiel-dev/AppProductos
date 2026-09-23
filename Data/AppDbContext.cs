using Microsoft.EntityFrameworkCore;
using AppProductos.Models;

namespace AppProductos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<IngresoProducto> IngresoProductos => Set<IngresoProducto>();
    public DbSet<SalidaProducto> SalidaProductos => Set<SalidaProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>().HasKey(c => c.IdCategoria);
        modelBuilder.Entity<Proveedor>().HasKey(p => p.IdProveedor);
        modelBuilder.Entity<Cliente>().HasKey(c => c.IdCliente);
        modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(p => p.IdProducto);
            entity.Property(p => p.Precio).HasPrecision(10, 2);
            entity.HasOne(p => p.Categoria)
                  .WithMany(c => c.Productos)
                  .HasForeignKey(p => p.IdCategoria);
        });

        modelBuilder.Entity<Proveedor>()
            .HasIndex(p => p.Cuit).IsUnique();

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Dni).IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<IngresoProducto>(entity =>
        {
            entity.HasKey(i => i.IdIngreso);
            entity.Property(i => i.PrecioUnitario).HasPrecision(10, 2);
            entity.HasOne(i => i.Producto).WithMany(p => p.Ingresos).HasForeignKey(i => i.IdProducto);
            entity.HasOne(i => i.Proveedor).WithMany(pr => pr.Ingresos).HasForeignKey(i => i.IdProveedor);
            entity.HasOne(i => i.Usuario).WithMany().HasForeignKey(i => i.IdUsuario);
        });

        modelBuilder.Entity<SalidaProducto>(entity =>
        {
            entity.HasKey(s => s.IdSalida);
            entity.Property(s => s.PrecioUnitario).HasPrecision(10, 2);
            entity.HasOne(s => s.Producto).WithMany(p => p.Salidas).HasForeignKey(s => s.IdProducto);
            entity.HasOne(s => s.Cliente).WithMany(c => c.Salidas).HasForeignKey(s => s.IdCliente);
            entity.HasOne(s => s.Usuario).WithMany().HasForeignKey(s => s.IdUsuario);
        });
    }
}