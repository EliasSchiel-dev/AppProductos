namespace AppProductos.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Operador";
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
}