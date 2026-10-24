using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppProductos.Data;
using AppProductos.Dtos;
using AppProductos.Models;
using AppProductos.Common;
using AppProductos.Services;

namespace AppProductos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // consultar el catálogo: cualquier usuario logueado
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IImagenProductoService _imagenes;

    public ProductosController(AppDbContext context, IImagenProductoService imagenes)
    {
        _context = context;
        _imagenes = imagenes;
    }

    // Una sola definición de "cómo se convierte un Producto en ProductoDto" para toda la clase
    private static readonly Expression<Func<Producto, ProductoDto>> AProductoDto = p => new ProductoDto
    {
        IdProducto = p.IdProducto,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Precio = p.Precio,
        Stock = p.Stock,
        IdCategoria = p.IdCategoria,
        CategoriaNombre = p.Categoria!.Nombre,
        ImagenUrl = p.ImagenUrl,
        Activo = p.Activo
    };

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductoDto>>> GetAll(
        int page = 1, int pageSize = 10, int? idCategoria = null, string? busqueda = null, bool soloDisponibles = false)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Productos.AsNoTracking().Where(p => p.Activo);

        if (idCategoria.HasValue)
            query = query.Where(p => p.IdCategoria == idCategoria);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda));

        // El catálogo de la app puede pedir solo lo que tiene stock
        if (soloDisponibles)
            query = query.Where(p => p.Stock > 0);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AProductoDto)
            .ToListAsync();

        return Ok(new PagedResult<ProductoDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoDto>> GetById(int id)
    {
        var query = _context.Productos.AsNoTracking().Where(p => p.IdProducto == id);

        // Un cliente no puede ver productos dados de baja (el personal sí, para poder reactivarlos)
        if (User.IsInRole(NombresRol.Cliente))
            query = query.Where(p => p.Activo);

        var producto = await query.Select(AProductoDto).FirstOrDefaultAsync();
        return producto is null ? NotFound() : Ok(producto);
    }

    [HttpPost]
    [Authorize(Roles = NombresRol.AdminOperador)]
    public async Task<ActionResult<ProductoDto>> Create(ProductoCreateDto dto)
    {
        var categoriaExiste = await _context.Categorias.AnyAsync(c => c.IdCategoria == dto.IdCategoria);
        if (!categoriaExiste) return BadRequest("La categoría indicada no existe.");

        if (dto.Precio < 0) return BadRequest("El precio no puede ser negativo.");

        var producto = new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            IdCategoria = dto.IdCategoria
            // Stock arranca en 0: se carga con un Ingreso
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        var creado = await _context.Productos.AsNoTracking()
            .Where(p => p.IdProducto == producto.IdProducto)
            .Select(AProductoDto)
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = producto.IdProducto }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = NombresRol.AdminOperador)]
    public async Task<IActionResult> Update(int id, ProductoUpdateDto dto)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null) return NotFound();

        if (!await _context.Categorias.AnyAsync(c => c.IdCategoria == dto.IdCategoria))
            return BadRequest("La categoría indicada no existe.");

        if (dto.Precio < 0) return BadRequest("El precio no puede ser negativo.");

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.IdCategoria = dto.IdCategoria;
        producto.Activo = dto.Activo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = NombresRol.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null) return NotFound();

        // Borrado lógico: el producto sigue existiendo para el historial de ventas e ingresos
        producto.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Sube (o reemplaza) la imagen del producto. Form-data con un campo de archivo llamado "archivo".
    /// La respuesta incluye el tamaño y el SHA-256 del archivo guardado, para poder comparar
    /// contra lo que se sube y lo que se descarga si hubiera algún problema.
    /// </summary>
    [HttpPost("{id:int}/imagen")]
    [Authorize(Roles = NombresRol.AdminOperador)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> SubirImagen(int id, IFormFile archivo, CancellationToken ct)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.IdProducto == id, ct);
        if (producto is null) return NotFound();

        var resultado = await _imagenes.GuardarAsync(archivo, ct);
        if (!resultado.Ok) return BadRequest(resultado.Error);

        var imagenAnterior = producto.ImagenUrl;
        producto.ImagenUrl = resultado.Url;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch
        {
            // Si no se pudo guardar en la base, no dejamos el archivo nuevo huérfano en el disco
            _imagenes.Eliminar(resultado.Url);
            throw;
        }

        // Recién cuando la base quedó actualizada se borra la imagen vieja
        _imagenes.Eliminar(imagenAnterior);

        return Ok(new { producto.ImagenUrl, resultado.Bytes, resultado.Sha256 });
    }
}
