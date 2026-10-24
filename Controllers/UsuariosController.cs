using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppProductos.Data;
using AppProductos.Dtos;
using AppProductos.Models;
using AppProductos.Common;

namespace AppProductos.Controllers;

/// <summary>Gestión de usuarios del personal (Admin y Operador). Solo el Admin puede usarlo.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = NombresRol.Admin)]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsuariosController(AppDbContext context) => _context = context;

    // Proyección reutilizada: EF la traduce a un SELECT con JOIN a Roles
    private static readonly Expression<Func<Usuario, UsuarioDto>> AUsuarioDto = u => new UsuarioDto
    {
        IdUsuario = u.IdUsuario,
        Username = u.Username,
        Rol = u.Rol!.Nombre,
        Email = u.Email,
        Activo = u.Activo
    };

    [HttpGet]
    public async Task<ActionResult<PagedResult<UsuarioDto>>> GetAll(int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Usuarios.AsNoTracking().OrderBy(u => u.Username);
        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AUsuarioDto)
            .ToListAsync();

        return Ok(new PagedResult<UsuarioDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        var u = await _context.Usuarios.AsNoTracking()
            .Where(x => x.IdUsuario == id)
            .Select(AUsuarioDto)
            .FirstOrDefaultAsync();

        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Create(UsuarioCreateDto dto)
    {
        // Los clientes no se crean acá: necesitan su perfil y se registran desde la app
        if (dto.Rol == NombresRol.Cliente)
            return BadRequest("Los clientes se registran desde /api/Auth/registro.");

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == dto.Rol);
        if (rol is null)
            return BadRequest($"Rol inválido. Valores permitidos: {NombresRol.Admin}, {NombresRol.Operador}.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            return BadRequest("La contraseña debe tener al menos 8 caracteres.");

        if (await _context.Usuarios.AnyAsync(u => u.Username == dto.Username))
            return BadRequest("Ese nombre de usuario ya existe.");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Ese correo ya está registrado.");

        var usuario = new Usuario
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IdRol = rol.IdRol,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario },
            new UsuarioDto { IdUsuario = usuario.IdUsuario, Username = usuario.Username, Rol = rol.Nombre, Email = usuario.Email, Activo = usuario.Activo });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UsuarioUpdateDto dto)
    {
        var usuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.IdUsuario == id);
        if (usuario is null) return NotFound();

        // Un cliente sigue siendo cliente y el personal no puede pasar a cliente
        var eraCliente = usuario.Rol!.Nombre == NombresRol.Cliente;
        if (eraCliente != (dto.Rol == NombresRol.Cliente))
            return BadRequest("No se puede cambiar un usuario desde o hacia el rol Cliente.");

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == dto.Rol);
        if (rol is null)
            return BadRequest("Rol inválido.");

        // Evita que el Admin se deje a sí mismo sin acceso
        if (id == User.ObtenerIdUsuario() && (!dto.Activo || rol.Nombre != NombresRol.Admin))
            return BadRequest("No puedes quitarte el rol de Admin ni desactivar tu propia cuenta.");

        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            await _context.Usuarios.AnyAsync(u => u.Email == dto.Email && u.IdUsuario != id))
            return BadRequest("Ese correo ya está registrado.");

        usuario.IdRol = rol.IdRol;
        usuario.Rol = rol;
        usuario.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email;
        usuario.Activo = dto.Activo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == User.ObtenerIdUsuario())
            return BadRequest("No puedes desactivar tu propia cuenta.");

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        // Borrado lógico: el usuario queda en la base (sus ventas e ingresos siguen apuntándole)
        usuario.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
