using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppProductos.Common;
using AppProductos.Data;
using AppProductos.Dtos;
using AppProductos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AppProductos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        // Include(u => u.Rol): trae también la fila de Roles para poder leer el nombre del rol
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Username == dto.Username && u.Activo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            return Unauthorized("Usuario o contraseña incorrectos.");

        return Ok(CrearToken(usuario, usuario.Rol!.Nombre));
    }

    /// <summary>
    /// Alta de un cliente desde la app. Crea la cuenta (Usuario con rol Cliente) y el perfil (Cliente)
    /// en una sola operación: o se guardan los dos o no se guarda ninguno.
    /// Devuelve el token, así que el cliente queda logueado apenas se registra.
    /// </summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Registro(RegistroClienteDto dto)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim();
        var dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();

        if (await _context.Usuarios.AnyAsync(u => u.Username == username))
            return Conflict("Ese nombre de usuario ya existe.");
        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict("Ese correo ya está registrado.");
        if (dni is not null && await _context.Clientes.AnyAsync(c => c.Dni == dni))
            return Conflict("Ese DNI ya está registrado.");

        var rolCliente = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == NombresRol.Cliente);
       

        var usuario = new Usuario
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IdRol = rolCliente.IdRol
        };

        // Al agregar el Cliente con su Usuario asociado, EF inserta primero el Usuario,
        // toma su Id y recién después inserta el Cliente. Todo dentro de un único SaveChanges.
        var cliente = new Cliente
        {
            Usuario = usuario,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = dni,
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim()
        };

        _context.Clientes.Add(cliente);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos registros simultáneos con el mismo usuario/correo/DNI: gana uno y el otro cae acá
            return Conflict("No se pudo completar el registro: el usuario, correo o DNI ya existen.");
        }

        return StatusCode(StatusCodes.Status201Created, CrearToken(usuario, rolCliente.Nombre));
    }

    private LoginResponseDto CrearToken(Usuario usuario, string rol)
    {
        var jwtKey = _config["Jwt:Key"]!;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, rol)
        };

        var expira = DateTime.UtcNow.AddMinutes(
            double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "120"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256)
        );

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expira = expira,
            Username = usuario.Username,
            Rol = rol
        };
    }
}
