namespace AppProductos.Dtos;

public class CategoriaDto
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class CategoriaCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}