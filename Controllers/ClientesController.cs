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
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _context;
    public ClientesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClienteDto>>> GetAll(int page = 1, int pageSize = 10, string? busqueda = null)
    {
        var query = _context.Clientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(c => c.Nombre.Contains(busqueda) || c.Apellido.Contains(busqueda));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClienteDto
            {
                IdCliente = c.IdCliente,
                Nombre = c.Nombre,
                Apellido = c.Apellido,
                Dni = c.Dni,
                Email = c.Email,
                Telefono = c.Telefono
            })
            .ToListAsync();

        return Ok(new PagedResult<ClienteDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        var c = await _context.Clientes.FindAsync(id);
        if (c is null) return NotFound();

        return Ok(new ClienteDto { IdCliente = c.IdCliente, Nombre = c.Nombre, Apellido = c.Apellido, Dni = c.Dni, Email = c.Email, Telefono = c.Telefono });
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(ClienteCreateDto dto)
    {
        var cliente = new Cliente
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Dni = dto.Dni,
            Email = dto.Email,
            Telefono = dto.Telefono
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cliente.IdCliente },
            new ClienteDto { IdCliente = cliente.IdCliente, Nombre = cliente.Nombre, Apellido = cliente.Apellido, Dni = cliente.Dni, Email = cliente.Email, Telefono = cliente.Telefono });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ClienteCreateDto dto)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        cliente.Nombre = dto.Nombre;
        cliente.Apellido = dto.Apellido;
        cliente.Dni = dto.Dni;
        cliente.Email = dto.Email;
        cliente.Telefono = dto.Telefono;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        try
        {
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict("No se puede eliminar: el cliente tiene ventas registradas.");
        }
    }
}