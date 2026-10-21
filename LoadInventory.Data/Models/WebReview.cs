#nullable disable
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models;

public partial class WebReview
{
    public int IdReview { get; set; }

    public int IdCliente { get; set; }

    public int IdProducto { get; set; }

    public DateOnly Fecha { get; set; }

    public string Comentario { get; set; }

    public int Rating { get; set; }
}