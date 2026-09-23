namespace AppProductos.Models;

public class Cliente
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }

    public ICollection<SalidaProducto> Salidas { get; set; } = new List<SalidaProducto>();
}