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
/// Ventas (salidas de mercadería). Reemplaza a SalidaProductosController.
///  - El CLIENTE compra su carrito (POST) y consulta sus compras (mis-compras).
///  - El PERSONAL (Admin/Operador) consulta todas las ventas.
///  - El ADMIN puede cancelar una venta pagada (se devuelve el stock).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly AppDbContext _context;
    public VentasController(AppDbContext context) => _context = context;

    private static readonly Expression<Func<Venta, VentaDto>> AVentaDto = v => new VentaDto
    {
        IdVenta = v.IdVenta,
        Fecha = v.Fecha,
        IdCliente = v.IdCliente,
        ClienteNombre = v.Cliente!.Nombre + " " + v.Cliente.Apellido,
        Estado = v.Estado!.Nombre,
        Total = v.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario),
        Detalles = v.Detalles.Select(d => new VentaDetalleDto
        {
            IdProducto = d.IdProducto,
            ProductoNombre = d.Producto!.Nombre,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Cantidad * d.PrecioUnitario
        }).ToList(),
        Pago = v.Pago == null ? null : new PagoDto
        {
            IdPago = v.Pago.IdPago,
            MetodoPago = v.Pago.MetodoPago!.Nombre,
            Monto = v.Pago.Monto,
            Aprobado = v.Pago.Aprobado,
            CodigoAutorizacion = v.Pago.CodigoAutorizacion,
            Fecha = v.Pago.Fecha
        }
    };

    // ---------- Personal: todas las ventas ----------

    [HttpGet]
    [Authorize(Roles = NombresRol.AdminOperador)]
    public async Task<ActionResult<PagedResult<VentaDto>>> GetAll(
        int page = 1, int pageSize = 10, int? idCliente = null, int? idProducto = null,
        string? estado = null, DateTime? desde = null, DateTime? hasta = null)
    {
        var query = _context.Ventas.AsNoTracking().AsQueryable();

        if (idCliente.HasValue) query = query.Where(v => v.IdCliente == idCliente);
        if (idProducto.HasValue) query = query.Where(v => v.Detalles.Any(d => d.IdProducto == idProducto));
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(v => v.Estado!.Nombre == estado);
        if (desde.HasValue) query = query.Where(v => v.Fecha >= desde.Value);
        if (hasta.HasValue)
        {
            var hastaExclusivo = hasta.Value.Date.AddDays(1);
            query = query.Where(v => v.Fecha < hastaExclusivo);
        }

        return Ok(await PaginarAsync(query, page, pageSize));
    }

    // ---------- Cliente: sus propias compras ----------

    [HttpGet("mis-compras")]
    [Authorize(Roles = NombresRol.Cliente)]
    public async Task<ActionResult<PagedResult<VentaDto>>> MisCompras(int page = 1, int pageSize = 10)
    {
        var idUsuario = User.ObtenerIdUsuario();
        var query = _context.Ventas.AsNoTracking().Where(v => v.Cliente!.IdUsuario == idUsuario);

        return Ok(await PaginarAsync(query, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VentaDto>> GetById(int id)
    {
        var query = _context.Ventas.AsNoTracking().Where(v => v.IdVenta == id);

        // Un cliente solo puede ver SUS ventas. Si pide la de otro, respondemos 404 (no revelamos que existe)
        if (User.IsInRole(NombresRol.Cliente))
        {
            var idUsuario = User.ObtenerIdUsuario();
            query = query.Where(v => v.Cliente!.IdUsuario == idUsuario);
        }

        var venta = await query.Select(AVentaDto).FirstOrDefaultAsync();
        return venta is null ? NotFound() : Ok(venta);
    }

    // ---------- Compra (checkout del carrito) ----------

    [HttpPost]
    [Authorize(Roles = NombresRol.Cliente)]
    public async Task<ActionResult<VentaDto>> Comprar(CompraCreateDto dto)
    {
        if (dto.Detalles is null || dto.Detalles.Count == 0)
            return BadRequest("El carrito está vacío.");
        if (dto.Detalles.Any(l => l.Cantidad <= 0))
            return BadRequest("Todas las cantidades deben ser mayores a cero.");

        // Si el carrito repite un producto, sumamos las cantidades en una sola línea
        var lineas = dto.Detalles
            .GroupBy(l => l.IdProducto)
            .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
            .ToList();

        var idUsuario = User.ObtenerIdUsuario();
        var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);
        if (cliente is null)
            return BadRequest("Tu usuario no tiene un perfil de cliente asociado.");

        var metodo = await _context.MetodosPago.AsNoTracking().FirstOrDefaultAsync(m => m.IdMetodoPago == dto.IdMetodoPago);
        if (metodo is null)
            return BadRequest("El método de pago indicado no existe.");

        // Productos del carrito (con el PRECIO ACTUAL de la base: el cliente no manda precios)
        var ids = lineas.Select(l => l.IdProducto).ToList();
        var productos = await _context.Productos.AsNoTracking()
            .Where(p => ids.Contains(p.IdProducto) && p.Activo)
            .ToDictionaryAsync(p => p.IdProducto);

        // Primera verificación, para dar un mensaje claro ANTES de abrir la transacción
        var problemas = new List<string>();
        foreach (var l in lineas)
        {
            if (!productos.TryGetValue(l.IdProducto, out var p))
                problemas.Add($"El producto {l.IdProducto} no existe o no está disponible.");
            else if (p.Stock < l.Cantidad)
                problemas.Add($"Stock insuficiente de '{p.Nombre}': pediste {l.Cantidad} y quedan {p.Stock}.");
        }
        if (problemas.Count > 0)
            return BadRequest(string.Join(" ", problemas));

        var estados = await _context.EstadosVenta.AsNoTracking().ToDictionaryAsync(e => e.Nombre, e => e.IdEstadoVenta);
        var aprobado = !dto.SimularRechazo;
        var nombreEstado = aprobado ? NombresEstadoVenta.Pagada : NombresEstadoVenta.Cancelada;
        if (!estados.TryGetValue(nombreEstado, out var idEstado))
            return StatusCode(500, "Falta cargar los estados de venta (ejecutar Database/02_DatosIniciales.sql).");

        var fecha = DateTime.Now;
        var detalles = lineas.Select(l => new VentaDetalle
        {
            IdProducto = l.IdProducto,
            Cantidad = l.Cantidad,
            PrecioUnitario = productos[l.IdProducto].Precio
        }).ToList();
        var total = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

        var venta = new Venta
        {
            Fecha = fecha,
            IdCliente = cliente.IdCliente,
            IdEstadoVenta = idEstado,
            Detalles = detalles,
            // Pago simulado: se aprueba salvo que se pida simular un rechazo
            Pago = new Pago
            {
                IdMetodoPago = metodo.IdMetodoPago,
                Fecha = fecha,
                Monto = total,
                Aprobado = aprobado,
                CodigoAutorizacion = aprobado ? "SIM-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : null
            }
        };

        await using var tx = await _context.Database.BeginTransactionAsync();

        if (aprobado)
        {
            foreach (var l in lineas)
            {
                var idProducto = l.IdProducto;
                var cantidad = l.Cantidad;

                // Descuento atómico: "UPDATE ... SET Stock = Stock - n WHERE Stock >= n".
                // Si dos clientes compran la última unidad a la vez, a uno le devuelve 0 filas
                // y se entera de que ya no hay stock (nunca queda stock negativo).
                var filas = await _context.Productos
                    .Where(p => p.IdProducto == idProducto && p.Stock >= cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - cantidad));

                if (filas == 0)
                {
                    await tx.RollbackAsync();   // deshace también los productos que ya se habían descontado
                    return Conflict($"El stock de '{productos[idProducto].Nombre}' cambió mientras comprabas. Revisa tu carrito e intenta de nuevo.");
                }
            }
        }

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        var resultado = await _context.Ventas.AsNoTracking()
            .Where(v => v.IdVenta == venta.IdVenta)
            .Select(AVentaDto)
            .FirstAsync();

        // 201 en ambos casos: la venta quedó registrada. La app mira "estado" y "pago.aprobado"
        return CreatedAtAction(nameof(GetById), new { id = venta.IdVenta }, resultado);
    }

    // ---------- Cancelación (Admin) ----------

    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = NombresRol.Admin)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var venta = await _context.Ventas.Include(v => v.Detalles).FirstOrDefaultAsync(v => v.IdVenta == id);
        if (venta is null) return NotFound();

        var estados = await _context.EstadosVenta.AsNoTracking().ToDictionaryAsync(e => e.Nombre, e => e.IdEstadoVenta);

        if (venta.IdEstadoVenta != estados[NombresEstadoVenta.Pagada])
            return BadRequest("Solo se pueden cancelar ventas en estado 'Pagada'.");

        await using var tx = await _context.Database.BeginTransactionAsync();

        // Devolvemos al stock lo que se había descontado
        foreach (var d in venta.Detalles)
        {
            var idProducto = d.IdProducto;
            var cantidad = d.Cantidad;

            await _context.Productos
                .Where(p => p.IdProducto == idProducto)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + cantidad));
        }

        venta.IdEstadoVenta = estados[NombresEstadoVenta.Cancelada];
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }

    private static async Task<PagedResult<VentaDto>> PaginarAsync(IQueryable<Venta> query, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.IdVenta)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AVentaDto)
            .ToListAsync();

        return new PagedResult<VentaDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}
