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
/// Entradas de mercadería (compras a proveedores). Reemplaza a IngresoProductosController:
/// ahora un ingreso es una cabecera (proveedor, fecha, usuario) con varias líneas de detalle.
/// Solo Admin y Operador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = NombresRol.AdminOperador)]
public class IngresosController : ControllerBase
{
    private readonly AppDbContext _context;
    public IngresosController(AppDbContext context) => _context = context;

    // Cabecera + líneas + total, todo calculado en una sola consulta SQL
    private static readonly Expression<Func<Ingreso, IngresoDto>> AIngresoDto = i => new IngresoDto
    {
        IdIngreso = i.IdIngreso,
        Fecha = i.Fecha,
        IdProveedor = i.IdProveedor,
        ProveedorNombre = i.Proveedor!.RazonSocial,
        UsuarioNombre = i.Usuario!.Username,
        Total = i.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario),
        Detalles = i.Detalles.Select(d => new IngresoDetalleDto
        {
            IdProducto = d.IdProducto,
            ProductoNombre = d.Producto!.Nombre,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Cantidad * d.PrecioUnitario
        }).ToList()
    };

    [HttpGet]
    public async Task<ActionResult<PagedResult<IngresoDto>>> GetAll(
        int page = 1, int pageSize = 10, int? idProducto = null, int? idProveedor = null,
        DateTime? desde = null, DateTime? hasta = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Ingresos.AsNoTracking().AsQueryable();

        if (idProveedor.HasValue) query = query.Where(i => i.IdProveedor == idProveedor);
        if (idProducto.HasValue) query = query.Where(i => i.Detalles.Any(d => d.IdProducto == idProducto));
        if (desde.HasValue) query = query.Where(i => i.Fecha >= desde.Value);
        if (hasta.HasValue)
        {
            var hastaExclusivo = hasta.Value.Date.AddDays(1);   // "hasta" incluye todo ese día
            query = query.Where(i => i.Fecha < hastaExclusivo);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.Fecha).ThenByDescending(i => i.IdIngreso)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AIngresoDto)
            .ToListAsync();

        return Ok(new PagedResult<IngresoDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngresoDto>> GetById(int id)
    {
        var ingreso = await _context.Ingresos.AsNoTracking()
            .Where(i => i.IdIngreso == id)
            .Select(AIngresoDto)
            .FirstOrDefaultAsync();

        return ingreso is null ? NotFound() : Ok(ingreso);
    }

    [HttpPost]
    public async Task<ActionResult<IngresoDto>> Create(IngresoCreateDto dto)
    {
        if (dto.Detalles is null || dto.Detalles.Count == 0)
            return BadRequest("El ingreso debe tener al menos un producto.");
        if (dto.Detalles.Any(d => d.Cantidad <= 0))
            return BadRequest("Todas las cantidades deben ser mayores a cero.");
        if (dto.Detalles.Any(d => d.PrecioUnitario < 0))
            return BadRequest("Los precios no pueden ser negativos.");
        if (dto.Detalles.GroupBy(d => d.IdProducto).Any(g => g.Count() > 1))
            return BadRequest("Un producto no puede repetirse dentro del mismo ingreso: suma las cantidades en una sola línea.");

        if (!await _context.Proveedores.AnyAsync(p => p.IdProveedor == dto.IdProveedor))
            return BadRequest("El proveedor indicado no existe.");

        var ids = dto.Detalles.Select(d => d.IdProducto).ToList();
        var activos = await _context.Productos
            .Where(p => ids.Contains(p.IdProducto) && p.Activo)
            .Select(p => p.IdProducto)
            .ToListAsync();
        var invalidos = ids.Except(activos).ToList();
        if (invalidos.Count > 0)
            return BadRequest($"Productos inexistentes o inactivos: {string.Join(", ", invalidos)}.");

        var ingreso = new Ingreso
        {
            IdProveedor = dto.IdProveedor,
            IdUsuario = User.ObtenerIdUsuario(),
            Fecha = DateTime.Now,
            Detalles = dto.Detalles.Select(d => new IngresoDetalle
            {
                IdProducto = d.IdProducto,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario
            }).ToList()
        };

        // Transacción: se guarda el ingreso Y se suma el stock, o no se hace ninguna de las dos cosas
        await using var tx = await _context.Database.BeginTransactionAsync();

        _context.Ingresos.Add(ingreso);
        await _context.SaveChangesAsync();

        foreach (var d in ingreso.Detalles)
        {
            var idProducto = d.IdProducto;
            var cantidad = d.Cantidad;

            // UPDATE atómico en SQL: "Stock = Stock + cantidad". No leemos el stock a memoria
            // para sumarlo y volver a guardarlo, porque dos ingresos simultáneos se pisarían.
            await _context.Productos
                .Where(p => p.IdProducto == idProducto)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + cantidad));
        }

        await tx.CommitAsync();

        var creado = await _context.Ingresos.AsNoTracking()
            .Where(i => i.IdIngreso == ingreso.IdIngreso)
            .Select(AIngresoDto)
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = ingreso.IdIngreso }, creado);
    }
}
