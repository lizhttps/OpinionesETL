namespace OpinionesETL.Data.Models.DW;

public class DimCliente
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public int? Edad { get; set; }
}

public class DimProducto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
}

public class DimFuente
{
    public int IdFuente { get; set; }
    public string TipoFuente { get; set; } = string.Empty;
}

public class DimFecha
{
    public int IdFecha { get; set; }
    public DateOnly Fecha { get; set; }
    public int Mes { get; set; }
    public int Trimestre { get; set; }
    public int Anio { get; set; }
}

public class DimClasificacion
{
    public int IdClasificacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
}