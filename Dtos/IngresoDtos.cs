using System.ComponentModel.DataAnnotations;

namespace AppProductos.Dtos;

public class IngresoDto
{
    public int IdIngreso { get; set; }
    public DateTime Fecha { get; set; }
    public int IdProveedor { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? UsuarioNombre { get; set; }
    public decimal Total { get; set; }
    public List<IngresoDetalleDto> Detalles { get; set; } = new();
}

public class IngresoDetalleDto
{
    public int IdProducto { get; set; }
    public string? ProductoNombre { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Un ingreso puede traer varios productos de un mismo proveedor.</summary>
public class IngresoCreateDto
{
    public int IdProveedor { get; set; }

    [Required, MinLength(1)]
    public List<IngresoDetalleCreateDto> Detalles { get; set; } = new();
}

public class IngresoDetalleCreateDto
{
    public int IdProducto { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }

    [Range(0, 99999999)]
    public decimal PrecioUnitario { get; set; }
}
