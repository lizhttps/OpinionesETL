namespace OpinionesETL.Data.Daos.Interfaces
{
    public interface IClienteDao
    {
        Task ClearClientesAsync();
        Task LoadClientesAsync(List<Models.Cliente> clientes);
    }
}