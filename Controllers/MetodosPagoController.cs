using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppProductos.Data;
using AppProductos.Dtos;

namespace AppProductos.Controllers;

/// <summary>Lista de medios de pago (simulados) que la app muestra al finalizar la compra.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetodosPagoController : ControllerBase
{
    private readonly AppDbContext _context;
    public MetodosPagoController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<MetodoPagoDto>>> GetAll()
    {
        var metodos = await _context.MetodosPago.AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MetodoPagoDto { IdMetodoPago = m.IdMetodoPago, Nombre = m.Nombre })
            .ToListAsync();

        return Ok(metodos);
    }
}
