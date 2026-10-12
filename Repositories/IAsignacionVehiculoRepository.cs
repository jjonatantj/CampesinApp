using System.Collections.Generic;
using System.Threading.Tasks;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Contrato de acceso a datos para el Caso de Uso CU-020: Asignación de Vehículo.
    /// REGLA ARQUITECTÓNICA: Define únicamente operaciones LINQ/EF Core sobre las entidades de la base de datos.
    /// </summary>
    public interface IAsignacionVehiculoRepository
    {
        /// <summary>
        /// Obtiene todas las demandas que están pendientes de transporte (id_Camion == null).
        /// Incluye relaciones con TipoProducto, Variedad, Municipio, Departamento y Comprador (Usuario).
        /// </summary>
        Task<List<Demandum>> ObtenerCargasPendientesAsync();

        /// <summary>
        /// Consulta el detalle de una demanda específica por su ID.
        /// Incluye producto, variedad, municipio destino, comprador y los campesinos participantes (AporteCamp).
        /// </summary>
        Task<Demandum?> ObtenerDemandaPorIdAsync(int idDemanda);

        /// <summary>
        /// Obtiene los camiones que cumplen con documentos al día (estado_pase, estado_seguro, estado_tecno)
        /// y capacidad mínima para la carga, verificando que no estén ocupados en otro transporte activo.
        /// </summary>
        Task<List<Camion>> ObtenerCamionesDisponiblesAsync(int capacidadMinimaKg, int? idDemandaExcluir = null);

        /// <summary>
        /// Consulta un camión específico por su ID junto con la información de su conductor (Usuario).
        /// </summary>
        Task<Camion?> ObtenerCamionPorIdAsync(int idCamion);

        /// <summary>
        /// Asigna el camión elegido a la demanda especificada actualizando el campo id_Camion.
        /// </summary>
        Task<bool> AsignarCamionADemandaAsync(int idDemanda, int idCamion);
    }
}

