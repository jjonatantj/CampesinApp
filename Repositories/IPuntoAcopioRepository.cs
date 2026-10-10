using CampesinApp.Repositories.Models;
using CampesinApp.Dtos;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Interfaz del Repositorio de Puntos de Acopio (CU-018).
    /// Opera exclusivamente sobre las tablas existentes en la base de datos (demanda, municipio, departamento).
    /// </summary>
    public interface IPuntoAcopioRepository
    {
        /// <summary>
        /// Valida si ya existe un punto registrado con la misma dirección o en las mismas coordenadas.
        /// Requerido para la excepción FE-02-CU018 (Punto duplicado).
        /// </summary>
        Task<bool> ExistePuntoDuplicadoAsync(string direccion, decimal latitud, decimal longitud);

        /// <summary>
        /// Guarda el punto de acopio persistiendo la dirección en la tabla existente 'demanda'
        /// (direccion_Inicial, direccion_Final, etc.).
        /// </summary>
        Task<bool> GuardarPuntoAcopioAsync(PuntoAcopioRegistroDto datos);

        /// <summary>
        /// Obtiene la lista completa de departamentos para el desplegable.
        /// </summary>
        Task<List<Departamento>> ObtenerDepartamentosAsync();

        /// <summary>
        /// Obtiene los municipios correspondientes a un departamento seleccionado.
        /// </summary>
        Task<List<Municipio>> ObtenerMunicipiosPorDepartamentoAsync(int idDepartamento);

        /// <summary>
        /// Obtiene todos los municipios de la base de datos.
        /// </summary>
        Task<List<Municipio>> ObtenerTodosMunicipiosAsync();
    }
}

