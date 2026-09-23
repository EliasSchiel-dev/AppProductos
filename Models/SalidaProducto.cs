namespace AppProductos.Models;

public class SalidaProducto
{
    public int IdSalida { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int IdProducto { get; set; }
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    public Producto? Producto { get; set; }
    public Cliente? Cliente { get; set; }
    public Usuario? Usuario { get; set; }
}