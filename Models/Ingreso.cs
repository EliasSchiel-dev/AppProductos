namespace AppProductos.Models;

/// <summary>Cabecera de una entrada de mercadería: quién, cuándo y de qué proveedor.</summary>
public class Ingreso
{
    public int IdIngreso { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int IdProveedor { get; set; }
    public int IdUsuario { get; set; }

    public Proveedor? Proveedor { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<IngresoDetalle> Detalles { get; set; } = new List<IngresoDetalle>();
}

/// <summary>Una línea del ingreso: qué producto, cuántas unidades y a qué costo.</summary>
public class IngresoDetalle
{
    public int IdIngresoDetalle { get; set; }
    public int IdIngreso { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    public Ingreso? Ingreso { get; set; }
    public Producto? Producto { get; set; }
}
