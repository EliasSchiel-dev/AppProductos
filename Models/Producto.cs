namespace AppProductos.Models;

public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int IdCategoria { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public Categoria? Categoria { get; set; }
    public ICollection<IngresoProducto> Ingresos { get; set; } = new List<IngresoProducto>();
    public ICollection<SalidaProducto> Salidas { get; set; } = new List<SalidaProducto>();
}