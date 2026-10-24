using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppProductos.Data;
using AppProductos.Dtos;
using AppProductos.Models;
using AppProductos.Common;

namespace AppProductos.Controllers;

/// <summary>
/// Consulta y edición de clientes. No hay alta ni baja acá:
/// el alta es /api/Auth/registro y la baja es desactivar el usuario (UsuariosController).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _context;
    public ClientesController(AppDbContext context) => _context = context;

    private static readonly Expression<Func<Cliente, ClienteDto>> AClienteDto = c => new ClienteDto
    {
        IdCliente = c.IdCliente,
        IdUsuario = c.IdUsuario,
        Username = c.Usuario!.Username,
        Nombre = c.Nombre,
        Apellido = c.Apellido,
        Dni = c.Dni,
        Email = c.Usuario.Email,
        Telefono = c.Telefono,
        Activo = c.Usuario.Activo
    };

    // ---------- Personal del negocio ----------

    [HttpGet]
    [Authorize(Roles = NombresRol.AdminOperador)]
    public async Task<ActionResult<PagedResult<ClienteDto>>> GetAll(int page = 1, int pageSize = 10, string? busqueda = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Clientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c => c.Nombre.Contains(busqueda) || c.Apellido.Contains(busqueda));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AClienteDto)
            .ToListAsync();

        return Ok(new PagedResult<ClienteDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = NombresRol.AdminOperador)]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        var c = await _context.Clientes.AsNoTracking()
            .Where(x => x.IdCliente == id)
            .Select(AClienteDto)
            .FirstOrDefaultAsync();

        return c is null ? NotFound() : Ok(c);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = NombresRol.AdminOperador)]
    public async Task<IActionResult> Update(int id, ClienteUpdateDto dto)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        return await ActualizarAsync(cliente, dto);
    }

    // ---------- El propio cliente ("mi perfil") ----------

    [HttpGet("me")]
    [Authorize(Roles = NombresRol.Cliente)]
    public async Task<ActionResult<ClienteDto>> GetMe()
    {
        var idUsuario = User.ObtenerIdUsuario();
        var c = await _context.Clientes.AsNoTracking()
            .Where(x => x.IdUsuario == idUsuario)
            .Select(AClienteDto)
            .FirstOrDefaultAsync();

        return c is null ? NotFound() : Ok(c);
    }

    [HttpPut("me")]
    [Authorize(Roles = NombresRol.Cliente)]
    public async Task<IActionResult> UpdateMe(ClienteUpdateDto dto)
    {
        var idUsuario = User.ObtenerIdUsuario();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);
        if (cliente is null) return NotFound();
        return await ActualizarAsync(cliente, dto);
    }

    private async Task<IActionResult> ActualizarAsync(Cliente cliente, ClienteUpdateDto dto)
    {
        var dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();

        if (dni is not null &&
            await _context.Clientes.AnyAsync(c => c.Dni == dni && c.IdCliente != cliente.IdCliente))
            return Conflict("Ese DNI ya está registrado.");

        cliente.Nombre = dto.Nombre.Trim();
        cliente.Apellido = dto.Apellido.Trim();
        cliente.Dni = dni;
        cliente.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
