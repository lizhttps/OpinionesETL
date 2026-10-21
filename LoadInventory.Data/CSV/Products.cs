using CsvHelper.Configuration.Attributes;

namespace OpinionesETL.Data.CSV
{
    public class Products
    {
        public string Nombre { get; set; }
        public int IdProducto { get; set; }

        [Name("Categoría")]
        public string Categoria { get; set; }
    }
}
