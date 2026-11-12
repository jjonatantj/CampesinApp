using System.Threading.Tasks;

namespace CampesinApp.Services;

/// <summary>
/// Contrato del servicio de cálculo logístico de transporte (CU-010 / CU-017 / CU-023).
/// </summary>
public interface ILogisticaFleteService
{
    /// <summary>
    /// Consulta los kilómetros exactos vía Google Routes API entre la Región del Tequendama
    /// y el Punto de Acopio de destino, y ejecuta el SP sp_CalcularFleteDemanda.
    /// </summary>
    /// <param name="kilos">Cantidad en kilos demandada.</param>
    /// <param name="idPuntoAcopio">ID del punto de acopio destino.</param>
    /// <returns>Valor total en COP del flete calculado para el viaje.</returns>
    Task<double> CalcularFleteAsync(double kilos, int idPuntoAcopio);
}

