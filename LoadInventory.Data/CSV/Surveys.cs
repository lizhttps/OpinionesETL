using CsvHelper.Configuration.Attributes;

namespace OpinionesETL.Data.CSV
{
    public class Surveys
    {
        public int IdOpinion { get; set; }
        public int IdCliente { get; set; }
        public int IdProducto { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; }

        [Name("Clasificación")]
        public string Clasificacion { get; set; }

        [Name("PuntajeSatisfacción")]
        public int PuntajeSatisfaccion { get; set; }

        public string Fuente { get; set; }
    }
}
