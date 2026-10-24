namespace AppProductos.Models;

/// <summary>
/// Datos personales de quien compra. Cada cliente tiene su cuenta (Usuario con rol Cliente);
/// el email se consulta en Usuario para no guardarlo dos veces.
/// </summary>
public class Cliente
{
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Telefono { get; set; }

    public Usuario? Usuario { get; set; }
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
