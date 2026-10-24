using System.ComponentModel.DataAnnotations;

namespace AppProductos.Dtos;

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

/// <summary>
/// Alta de un cliente desde la app. Crea a la vez su cuenta (Usuario con rol Cliente)
/// y su perfil (Cliente). Los atributos [Required], [StringLength], etc. hacen que ASP.NET
/// responda 400 automáticamente si los datos no cumplen, sin escribir ifs a mano.
/// </summary>
public class RegistroClienteDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Dni { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }
}
