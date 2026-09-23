namespace AppProductos.Dtos;

public class ClienteDto
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
}

public class ClienteCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
}