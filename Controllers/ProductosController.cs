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
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductosController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductoDto>>> GetAll(
        int page = 1, int pageSize = 10, int? idCategoria = null, string? busqueda = null)
    {
        var query = _context.Productos.AsNoTracking().Where(p => p.Activo);

        if (idCategoria.HasValue)
            query = query.Where(p => p.IdCategoria == idCategoria);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductoDto
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
            })
            .ToListAsync();

        return Ok(new PagedResult<ProductoDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductoDto>> GetById(int id)
    {
        var p = await _context.Productos.Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.IdProducto == id);
        if (p is null) return NotFound();

        return Ok(new ProductoDto
        {
            IdProducto = p.IdProducto,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            Stock = p.Stock,
            IdCategoria = p.IdCategoria,
            CategoriaNombre = p.Categoria?.Nombre,
            ImagenUrl = p.ImagenUrl,
            Activo = p.Activo
        });
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create(ProductoCreateDto dto)
    {
        var categoriaExiste = await _context.Categorias.AnyAsync(c => c.IdCategoria == dto.IdCategoria);
        if (!categoriaExiste) return BadRequest("La categoría indicada no existe.");

        var producto = new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Stock = dto.Stock,
            IdCategoria = dto.IdCategoria
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = producto.IdProducto }, producto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProductoUpdateDto dto)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null) return NotFound();

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.IdCategoria = dto.IdCategoria;
        producto.Activo = dto.Activo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null) return NotFound();

        
        producto.Activo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/imagen")]
    public async Task<IActionResult> SubirImagen(int id, IFormFile archivo)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null) return NotFound();

        if (archivo is null || archivo.Length == 0)
            return BadRequest("No se envió ningún archivo.");

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(extension))
            return BadRequest("Formato de imagen no permitido.");

        var carpetaUploads = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(carpetaUploads);

        var nombreArchivo = $"{Guid.NewGuid()}{extension}";
        var rutaFisica = Path.Combine(carpetaUploads, nombreArchivo);

        using (var stream = new FileStream(rutaFisica, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        producto.ImagenUrl = $"/uploads/{nombreArchivo}";
        await _context.SaveChangesAsync();

        return Ok(new { producto.ImagenUrl });
    }
}