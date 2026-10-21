#nullable disable
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models;

public partial class Survey
{
    public int IdOpinion { get; set; }

    public int IdCliente { get; set; }

    public int IdProducto { get; set; }

    public string Fuente { get; set; }

    public DateOnly Fecha { get; set; }

    public string Comentario { get; set; }

    public string Clasificacion { get; set; }

    public int PuntajeSatisfaccion { get; set; }
}