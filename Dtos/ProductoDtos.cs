namespace AppProductos.Dtos;

public class ProductoDto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int IdCategoria { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; }

    /// <summary>Atajo para la app: true si hay unidades para vender.</summary>
    public bool Disponible => Stock > 0;
}

/// <summary>
/// El producto nace con stock 0. El stock inicial se carga registrando un Ingreso,
/// así todo el stock queda respaldado por un movimiento (se puede auditar).
/// </summary>
public class ProductoCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int IdCategoria { get; set; }
}

public class ProductoUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int IdCategoria { get; set; }
    public bool Activo { get; set; }
}
