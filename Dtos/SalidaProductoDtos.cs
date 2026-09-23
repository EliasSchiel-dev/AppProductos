namespace AppProductos.Dtos;

public class SalidaProductoDto
{
    public int IdSalida { get; set; }
    public DateTime Fecha { get; set; }
    public int IdProducto { get; set; }
    public string? ProductoNombre { get; set; }
    public int IdCliente { get; set; }
    public string? ClienteNombre { get; set; }
    public string? UsuarioNombre { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class SalidaProductoCreateDto
{
    public int IdProducto { get; set; }
    public int IdCliente { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}