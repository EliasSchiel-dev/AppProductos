namespace AppProductos.Models;

/// <summary>Estados posibles de una venta (tabla de referencia): Pendiente, Pagada, Cancelada.</summary>
public class EstadoVenta
{
    public int IdEstadoVenta { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Cabecera de una venta (una compra del carrito): quién compró, cuándo y en qué estado está.</summary>
public class Venta
{
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int IdCliente { get; set; }
    public int IdEstadoVenta { get; set; }

    public Cliente? Cliente { get; set; }
    public EstadoVenta? Estado { get; set; }
    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    public Pago? Pago { get; set; }
}

/// <summary>Una línea de la venta. PrecioUnitario es el precio al momento de comprar.</summary>
public class VentaDetalle
{
    public int IdVentaDetalle { get; set; }
    public int IdVenta { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    public Venta? Venta { get; set; }
    public Producto? Producto { get; set; }
}

public class MetodoPago
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Pago SIMULADO de una venta (no se procesa dinero real).</summary>
public class Pago
{
    public int IdPago { get; set; }
    public int IdVenta { get; set; }
    public int IdMetodoPago { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public decimal Monto { get; set; }
    public bool Aprobado { get; set; }
    public string? CodigoAutorizacion { get; set; }

    public Venta? Venta { get; set; }
    public MetodoPago? MetodoPago { get; set; }
}
