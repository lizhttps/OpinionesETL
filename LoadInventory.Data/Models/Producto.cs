#nullable disable
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string Nombre { get; set; }

    public string Categoria { get; set; }
}