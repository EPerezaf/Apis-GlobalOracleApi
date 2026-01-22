namespace GM.CatalogSync.Domain.Entities;

public class EmpleadoHistorico
{
    public int IdAsignacionPuesto { get; set;}
    public int IdEmpleado { get;set;}
    public string DealerId { get; set;} = string.Empty;
    public string ClavePuesto { get; set; } = string.Empty;
    public string NombrePuesto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public int IdEmpleadoJefe { get; set; }
    public DateTime FechaInicioAsignacion { get; set; }
    public DateTime FechaFinAsignacion { get; set;}
    public int EsActual { get; set; }
    public string MotivoCambio { get; set; } = string.Empty;
    public string Observaciones { get; set;} = string.Empty;
    public string UsuarioAlta { get; set;} = string.Empty;
    public DateTime FechaAlta { get;set;}
    public string UsuarioModifica { get; set;} = string.Empty;
    public DateTime FechaModifica { get; set;}



}