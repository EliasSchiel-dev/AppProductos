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
public class SalidaProductosController : ControllerBase
{
    private readonly AppDbContext _context;
    public SalidaProductosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResult<SalidaProductoDto>>> GetAll(
        int page = 1, int pageSize = 10, int? idProducto = null, int? idCliente = null)
    {
        var query = _context.SalidaProductos
            .Include(s => s.Producto)
            .Include(s => s.Cliente)
            .Include(s => s.Usuario)
            .AsNoTracking()
            .AsQueryable();

        if (idProducto.HasValue) query = query.Where(s => s.IdProducto == idProducto);
        if (idCliente.HasValue) query = query.Where(s => s.IdCliente == idCliente);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToDto(s))
            .ToListAsync();

        return Ok(new PagedResult<SalidaProductoDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalidaProductoDto>> GetById(int id)
    {
        var salida = await _context.SalidaProductos
            .Include(s => s.Producto).Include(s => s.Cliente).Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.IdSalida == id);

        if (salida is null) return NotFound();
        return Ok(MapToDto(salida));
    }

    [HttpPost]
    public async Task<ActionResult<SalidaProductoDto>> Create(SalidaProductoCreateDto dto)
    {
        if (dto.Cantidad <= 0)
            return BadRequest("La cantidad debe ser mayor a cero.");

        var producto = await _context.Productos.FindAsync(dto.IdProducto);
        if (producto is null || !producto.Activo)
            return BadRequest("El producto indicado no existe o está inactivo.");

        var clienteExiste = await _context.Clientes.AnyAsync(c => c.IdCliente == dto.IdCliente);
        if (!clienteExiste)
            return BadRequest("El cliente indicado no existe.");

        
        if (producto.Stock < dto.Cantidad)
            return BadRequest($"Stock insuficiente. Stock actual de '{producto.Nombre}': {producto.Stock}.");

        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var salida = new SalidaProducto
        {
            IdProducto = dto.IdProducto,
            IdCliente = dto.IdCliente,
            IdUsuario = idUsuario,
            Cantidad = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario,
            Fecha = DateTime.Now
        };

        producto.Stock -= dto.Cantidad;
        _context.SalidaProductos.Add(salida);
        await _context.SaveChangesAsync();

        await _context.Entry(salida).Reference(s => s.Producto).LoadAsync();
        await _context.Entry(salida).Reference(s => s.Cliente).LoadAsync();
        await _context.Entry(salida).Reference(s => s.Usuario).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = salida.IdSalida }, MapToDto(salida));
    }

    private static SalidaProductoDto MapToDto(SalidaProducto s) => new()
    {
        IdSalida = s.IdSalida,
        Fecha = s.Fecha,
        IdProducto = s.IdProducto,
        ProductoNombre = s.Producto?.Nombre,
        IdCliente = s.IdCliente,
        ClienteNombre = s.Cliente is not null ? $"{s.Cliente.Nombre} {s.Cliente.Apellido}" : null,
        UsuarioNombre = s.Usuario?.Username,
        Cantidad = s.Cantidad,
        PrecioUnitario = s.PrecioUnitario
    };
}