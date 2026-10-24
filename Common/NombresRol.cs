namespace AppProductos.Common;

/// <summary>
/// Nombres de roles escritos en UN solo lugar. Se usan en [Authorize(Roles = ...)],
/// así un error de tipeo ("Admn") es un error de compilación y no un 403 misterioso.
/// Deben coincidir con las filas de la tabla Roles (Database/02_DatosIniciales.sql).
/// </summary>
public static class NombresRol
{
    public const string Admin = "Admin";
    public const string Operador = "Operador";
    public const string Cliente = "Cliente";

    /// <summary>Personal del negocio (para [Authorize(Roles = ...)] con más de un rol).</summary>
    public const string AdminOperador = Admin + "," + Operador;
}

/// <summary>Nombres de las filas de la tabla EstadosVenta.</summary>
public static class NombresEstadoVenta
{
    public const string Pendiente = "Pendiente";
    public const string Pagada = "Pagada";
    public const string Cancelada = "Cancelada";
}
