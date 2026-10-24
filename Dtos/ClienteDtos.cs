using System.ComponentModel.DataAnnotations;

namespace AppProductos.Dtos;

public class ClienteDto
{
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public string? Username { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Email { get; set; }      // viene de Usuarios (no se guarda dos veces)
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Datos que se pueden modificar de un cliente. El alta se hace en /api/Auth/registro.</summary>
public class ClienteUpdateDto
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Dni { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }
}
