namespace AppProductos.Dtos;

public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Activo { get; set; }
}

public class UsuarioCreateDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "Operador";
    public string? Email { get; set; }
}

public class UsuarioUpdateDto
{
    public string Rol { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Activo { get; set; }
}