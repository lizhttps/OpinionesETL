#nullable disable
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models;

public partial class SocialComment
{
    public int IdComment { get; set; }

    public int? IdCliente { get; set; }

    public int IdProducto { get; set; }

    public string Fuente { get; set; }

    public DateOnly Fecha { get; set; }

    public string Comentario { get; set; }
}