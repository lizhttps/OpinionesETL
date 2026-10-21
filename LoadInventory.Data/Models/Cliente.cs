#nullable disable


namespace OpinionesETL.Data.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string Nombre { get; set; }

    public string Email { get; set; }
}