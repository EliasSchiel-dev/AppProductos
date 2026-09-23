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
public class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _context;
    public ProveedoresController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProveedorDto>>> GetAll(int page = 1, int pageSize = 10, string? busqueda = null)
    {
        var query = _context.Proveedores.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.RazonSocial.Contains(busqueda));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.RazonSocial)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProveedorDto
            {
                IdProveedor = p.IdProveedor,
                RazonSocial = p.RazonSocial,
                Cuit = p.Cuit,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion
            })
            .ToListAsync();

        return Ok(new PagedResult<ProveedorDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProveedorDto>> GetById(int id)
    {
        var p = await _context.Proveedores.FindAsync(id);
        if (p is null) return NotFound();

        return Ok(new ProveedorDto
        {
            IdProveedor = p.IdProveedor,
            RazonSocial = p.RazonSocial,
            Cuit = p.Cuit,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProveedorDto>> Create(ProveedorCreateDto dto)
    {
        var proveedor = new Proveedor
        {
            RazonSocial = dto.RazonSocial,
            Cuit = dto.Cuit,
            Telefono = dto.Telefono,
            Email = dto.Email,
            Direccion = dto.Direccion
        };

        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = proveedor.IdProveedor },
            new ProveedorDto { IdProveedor = proveedor.IdProveedor, RazonSocial = proveedor.RazonSocial, Cuit = proveedor.Cuit, Telefono = proveedor.Telefono, Email = proveedor.Email, Direccion = proveedor.Direccion });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProveedorCreateDto dto)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor is null) return NotFound();

        proveedor.RazonSocial = dto.RazonSocial;
        proveedor.Cuit = dto.Cuit;
        proveedor.Telefono = dto.Telefono;
        proveedor.Email = dto.Email;
        proveedor.Direccion = dto.Direccion;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor is null) return NotFound();

        try
        {
            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict("No se puede eliminar: el proveedor tiene ingresos de productos registrados.");
        }
    }
}