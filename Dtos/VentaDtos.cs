using System.ComponentModel.DataAnnotations;

namespace AppProductos.Dtos;

public class VentaDto
{
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int IdCliente { get; set; }
    public string? ClienteNombre { get; set; }
    public string? Estado { get; set; }
    public decimal Total { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();
    public PagoDto? Pago { get; set; }
}

public class VentaDetalleDto
{
    public int IdProducto { get; set; }
    public string? ProductoNombre { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class PagoDto
{
    public int IdPago { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Monto { get; set; }
    public bool Aprobado { get; set; }
    public string? CodigoAutorizacion { get; set; }
    public DateTime Fecha { get; set; }
}

/// <summary>
/// Compra del carrito. OJO: no se envía el precio. El servidor toma el precio vigente de la base,
/// así un cliente no puede comprar un producto "al precio que él quiera".
/// </summary>
public class CompraCreateDto
{
    public int IdMetodoPago { get; set; }

    [Required, MinLength(1)]
    public List<CompraLineaDto> Detalles { get; set; } = new();

    /// <summary>Solo para demostrar el flujo de un pago rechazado en la simulación.</summary>
    public bool SimularRechazo { get; set; }
}

public class CompraLineaDto
{
    public int IdProducto { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }
}

public class MetodoPagoDto
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
