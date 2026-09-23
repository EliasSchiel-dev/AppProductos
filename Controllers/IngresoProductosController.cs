using System.Security.Claims;
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
public class IngresoProductosController : ControllerBase
{
    private readonly AppDbContext _context;
    public IngresoProductosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResult<IngresoProductoDto>>> GetAll(
        int page = 1, int pageSize = 10, int? idProducto = null, int? idProveedor = null)
    {
        var query = _context.IngresoProductos
            .Include(i => i.Producto)
            .Include(i => i.Proveedor)
            .Include(i => i.Usuario)
            .AsNoTracking()
            .AsQueryable();

        if (idProducto.HasValue) query = query.Where(i => i.IdProducto == idProducto);
        if (idProveedor.HasValue) query = query.Where(i => i.IdProveedor == idProveedor);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => MapToDto(i))
            .ToListAsync();

        return Ok(new PagedResult<IngresoProductoDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IngresoProductoDto>> GetById(int id)
    {
        var ingreso = await _context.IngresoProductos
            .Include(i => i.Producto).Include(i => i.Proveedor).Include(i => i.Usuario)
            .FirstOrDefaultAsync(i => i.IdIngreso == id);

        if (ingreso is null) return NotFound();
        return Ok(MapToDto(ingreso));
    }

    [HttpPost]
    public async Task<ActionResult<IngresoProductoDto>> Create(IngresoProductoCreateDto dto)
    {
        if (dto.Cantidad <= 0)
            return BadRequest("La cantidad debe ser mayor a cero.");

        var producto = await _context.Productos.FindAsync(dto.IdProducto);
        if (producto is null || !producto.Activo)
            return BadRequest("El producto indicado no existe o está inactivo.");

        var proveedorExiste = await _context.Proveedores.AnyAsync(p => p.IdProveedor == dto.IdProveedor);
        if (!proveedorExiste)
            return BadRequest("El proveedor indicado no existe.");

        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ingreso = new IngresoProducto
        {
            IdProducto = dto.IdProducto,
            IdProveedor = dto.IdProveedor,
            IdUsuario = idUsuario,
            Cantidad = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario,
            Fecha = DateTime.Now
        };

        
        producto.Stock += dto.Cantidad;
        _context.IngresoProductos.Add(ingreso);
        await _context.SaveChangesAsync();

        await _context.Entry(ingreso).Reference(i => i.Producto).LoadAsync();
        await _context.Entry(ingreso).Reference(i => i.Proveedor).LoadAsync();
        await _context.Entry(ingreso).Reference(i => i.Usuario).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ingreso.IdIngreso }, MapToDto(ingreso));
    }

    private static IngresoProductoDto MapToDto(IngresoProducto i) => new()
    {
        IdIngreso = i.IdIngreso,
        Fecha = i.Fecha,
        IdProducto = i.IdProducto,
        ProductoNombre = i.Producto?.Nombre,
        IdProveedor = i.IdProveedor,
        ProveedorNombre = i.Proveedor?.RazonSocial,
        UsuarioNombre = i.Usuario?.Username,
        Cantidad = i.Cantidad,
        PrecioUnitario = i.PrecioUnitario
    };
}