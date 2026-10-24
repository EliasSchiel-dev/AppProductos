using AppProductos.Common;
using AppProductos.Models;
using Microsoft.EntityFrameworkCore;

namespace AppProductos.Data;

/// <summary>
/// Crea el primer usuario Admin al arrancar, si no existe ninguno.
/// </summary>
public static class DbSeeder
{
    public static async Task SembrarAdminAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var rolAdmin = await db.Roles.FirstOrDefaultAsync(r => r.Nombre == NombresRol.Admin);
        if (rolAdmin is null)
        {
            logger.LogWarning("No existe el rol 'Admin'. Ejecuta Database/02_DatosIniciales.sql en la base de datos.");
            return;
        }

        if (await db.Usuarios.AnyAsync(u => u.IdRol == rolAdmin.IdRol))
            return; // ya hay al menos un administrador

        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            logger.LogWarning("No hay administradores y 'Seed:AdminPassword' no está configurada (mínimo 8 caracteres): no se crea ninguno.");
            return;
        }

        var username = config["Seed:AdminUsername"];
        var email = config["Seed:AdminEmail"];

        db.Usuarios.Add(new Usuario
        {
            Username = string.IsNullOrWhiteSpace(username) ? "admin" : username.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IdRol = rolAdmin.IdRol
        });
        await db.SaveChangesAsync();

        logger.LogInformation("Usuario administrador inicial creado.");
    }
}
