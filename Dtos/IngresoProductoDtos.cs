namespace AppProductos.Dtos;

public class IngresoProductoDto
{
    public int IdIngreso { get; set; }
    public DateTime Fecha { get; set; }
    public int IdProducto { get; set; }
    public string? ProductoNombre { get; set; }
    public int IdProveedor { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? UsuarioNombre { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class IngresoProductoCreateDto
{
    public int IdProducto { get; set; }
    public int IdProveedor { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}