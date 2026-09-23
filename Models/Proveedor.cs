namespace AppProductos.Models;

public class Proveedor
{
    public int IdProveedor { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }

    public ICollection<IngresoProducto> Ingresos { get; set; } = new List<IngresoProducto>();
}