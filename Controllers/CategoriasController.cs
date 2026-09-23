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
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;
    public CategoriasController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoriaDto>>> GetAll(int page = 1, int pageSize = 10)
    {
        var query = _context.Categorias.AsNoTracking().OrderBy(c => c.Nombre);
        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoriaDto { IdCategoria = c.IdCategoria, Nombre = c.Nombre, Descripcion = c.Descripcion })
            .ToListAsync();

        return Ok(new PagedResult<CategoriaDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoriaDto>> GetById(int id)
    {
        var c = await _context.Categorias.FindAsync(id);
        if (c is null) return NotFound();
        return Ok(new CategoriaDto { IdCategoria = c.IdCategoria, Nombre = c.Nombre, Descripcion = c.Descripcion });
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Create(CategoriaCreateDto dto)
    {
        var categoria = new Categoria { Nombre = dto.Nombre, Descripcion = dto.Descripcion };
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = categoria.IdCategoria },
            new CategoriaDto { IdCategoria = categoria.IdCategoria, Nombre = categoria.Nombre, Descripcion = categoria.Descripcion });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CategoriaCreateDto dto)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria is null) return NotFound();

        categoria.Nombre = dto.Nombre;
        categoria.Descripcion = dto.Descripcion;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria is null) return NotFound();

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}