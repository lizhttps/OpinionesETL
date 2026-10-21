
namespace OpinionesETL.Data.Daos.Interfaces
{
    public interface IProductoDao
    {
        Task ClearProductoAsync();
        Task LoadProductoAsync(List<Models.Producto> productos);
    }
}
