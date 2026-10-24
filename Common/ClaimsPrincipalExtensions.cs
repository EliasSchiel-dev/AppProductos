using System.Security.Claims;

namespace AppProductos.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario logueado, tomado del token JWT (claim NameIdentifier).</summary>
    public static int ObtenerIdUsuario(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
