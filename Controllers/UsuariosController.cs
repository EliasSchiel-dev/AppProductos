using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppProductos.Data;
using AppProductos.Dtos;
using AppProductos.Models;
using AppProductos.Common;

namespace AppProductos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]   
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsuariosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResult<UsuarioDto>>> GetAll(int page = 1, int pageSize = 10)
    {
        var query = _context.Usuarios.AsNoTracking().OrderBy(u => u.Username);
        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UsuarioDto { IdUsuario = u.IdUsuario, Username = u.Username, Rol = u.Rol, Email = u.Email, Activo = u.Activo })
            .ToListAsync();

        return Ok(new PagedResult<UsuarioDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        return Ok(new UsuarioDto { IdUsuario = u.IdUsuario, Username = u.Username, Rol = u.Rol, Email = u.Email, Activo = u.Activo });
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Create(UsuarioCreateDto dto)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Username == dto.Username))
            return BadRequest("Ese nombre de usuario ya existe.");

        var usuario = new Usuario
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = dto.Rol,
            Email = dto.Email
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario },
            new UsuarioDto { IdUsuario = usuario.IdUsuario, Username = usuario.Username, Rol = usuario.Rol, Email = usuario.Email, Activo = usuario.Activo });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UsuarioUpdateDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        usuario.Rol = dto.Rol;
        usuario.Email = dto.Email;
        usuario.Activo = dto.Activo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        
        usuario.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}