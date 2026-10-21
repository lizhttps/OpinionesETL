namespace OpinionesETL.Data.Models;

public class EtlResult
{
    public string NombreTabla { get; set; } = string.Empty;
    public int RegistrosProcesados { get; set; }
    public int RegistrosInsertados { get; set; }
    public int RegistrosRechazados { get; set; }
    public List<string> MotivosRechazo { get; set; } = new();

    public bool Exitoso => RegistrosRechazados == 0;
}
