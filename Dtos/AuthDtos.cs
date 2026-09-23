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