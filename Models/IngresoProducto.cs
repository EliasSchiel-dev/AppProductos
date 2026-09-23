namespace AppProductos.Models;

public class IngresoProducto
{
    public int IdIngreso { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int IdProducto { get; set; }
    public int IdProveedor { get; set; }
    public int IdUsuario { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    public Producto? Producto { get; set; }
    public Proveedor? Proveedor { get; set; }
    public Usuario? Usuario { get; set; }
}