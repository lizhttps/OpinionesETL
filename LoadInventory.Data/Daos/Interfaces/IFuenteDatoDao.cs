namespace OpinionesETL.Data.Daos.Interfaces
{
    public interface IFuenteDatoDao
    {
        Task ClearFuenteDatosAsync();
        Task LoadFuenteDatosAsync(List<Models.FuenteDato> fuenteDatos);
    }
}