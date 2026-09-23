namespace AppProductos.Dtos;

public class ProductoDto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int IdCategoria { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; }
}

public class ProductoCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int IdCategoria { get; set; }
}

public class ProductoUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int IdCategoria { get; set; }
    public bool Activo { get; set; }
}