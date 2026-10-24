using Microsoft.EntityFrameworkCore;
using AppProductos.Models;

namespace AppProductos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // El nombre de cada DbSet es el nombre de su tabla en SQL Server
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Ingreso> Ingresos => Set<Ingreso>();
    public DbSet<IngresoDetalle> IngresoDetalles => Set<IngresoDetalle>();
    public DbSet<EstadoVenta> EstadosVenta => Set<EstadoVenta>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Las claves se declaran a mano porque se llaman IdXxx (EF busca "Id" o "XxxId" por convención).
        // Todas las relaciones usan Restrict: la base nunca borra en cascada; los "borrados" son lógicos (Activo = 0).

        modelBuilder.Entity<Rol>(e =>
        {
            e.HasKey(r => r.IdRol);
            e.HasIndex(r => r.Nombre).IsUnique();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasKey(u => u.IdUsuario);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasOne(u => u.Rol).WithMany(r => r.Usuarios)
             .HasForeignKey(u => u.IdRol).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasKey(c => c.IdCliente);
            e.HasIndex(c => c.Dni).IsUnique();
            // 1 a 1: cada cliente es exactamente un usuario
            e.HasOne(c => c.Usuario).WithOne()
             .HasForeignKey<Cliente>(c => c.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Categoria>().HasKey(c => c.IdCategoria);

        modelBuilder.Entity<Producto>(e =>
        {
            e.HasKey(p => p.IdProducto);
            e.Property(p => p.Precio).HasPrecision(10, 2);
            e.HasOne(p => p.Categoria).WithMany(c => c.Productos)
             .HasForeignKey(p => p.IdCategoria).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proveedor>(e =>
        {
            e.HasKey(p => p.IdProveedor);
            e.HasIndex(p => p.Cuit).IsUnique();
        });

        modelBuilder.Entity<Ingreso>(e =>
        {
            e.HasKey(i => i.IdIngreso);
            e.HasOne(i => i.Proveedor).WithMany(p => p.Ingresos)
             .HasForeignKey(i => i.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Usuario).WithMany()
             .HasForeignKey(i => i.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IngresoDetalle>(e =>
        {
            e.HasKey(d => d.IdIngresoDetalle);
            e.Property(d => d.PrecioUnitario).HasPrecision(10, 2);
            e.HasIndex(d => new { d.IdIngreso, d.IdProducto }).IsUnique();
            e.HasOne(d => d.Ingreso).WithMany(i => i.Detalles)
             .HasForeignKey(d => d.IdIngreso).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Producto).WithMany()
             .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EstadoVenta>(e =>
        {
            e.HasKey(x => x.IdEstadoVenta);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.HasKey(v => v.IdVenta);
            e.HasOne(v => v.Cliente).WithMany(c => c.Ventas)
             .HasForeignKey(v => v.IdCliente).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Estado).WithMany()
             .HasForeignKey(v => v.IdEstadoVenta).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VentaDetalle>(e =>
        {
            e.HasKey(d => d.IdVentaDetalle);
            e.Property(d => d.PrecioUnitario).HasPrecision(10, 2);
            e.HasIndex(d => new { d.IdVenta, d.IdProducto }).IsUnique();
            e.HasOne(d => d.Venta).WithMany(v => v.Detalles)
             .HasForeignKey(d => d.IdVenta).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Producto).WithMany()
             .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MetodoPago>(e =>
        {
            e.HasKey(m => m.IdMetodoPago);
            e.HasIndex(m => m.Nombre).IsUnique();
        });

        modelBuilder.Entity<Pago>(e =>
        {
            e.HasKey(p => p.IdPago);
            e.Property(p => p.Monto).HasPrecision(12, 2);
            // 1 a 1: una venta tiene como máximo un pago
            e.HasOne(p => p.Venta).WithOne(v => v.Pago)
             .HasForeignKey<Pago>(p => p.IdVenta).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.MetodoPago).WithMany()
             .HasForeignKey(p => p.IdMetodoPago).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
