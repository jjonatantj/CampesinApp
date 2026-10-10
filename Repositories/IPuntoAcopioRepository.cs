using System.Collections.Generic;
using System.Threading.Tasks;
using CampesinApp.Repositories.Models;
using CampesinApp.Dtos;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Interfaz del Repositorio de Puntos de Acopio (CU-018).
    /// Regla: Contiene únicamente la definición de operaciones de acceso a datos con LINQ/EF Core.
    /// </summary>
    public interface IPuntoAcopioRepository
    {
        /// <summary>
        /// Valida si ya existe un punto registrado con la misma dirección o en las mismas coordenadas.
        /// Cumple con la validación de la excepción FE-02-CU018 (Punto duplicado).
        /// </summary>
        Task<bool> ExistePuntoDuplicadoAsync(string direccion, decimal latitud, decimal longitud);

        /// <summary>
        /// Flujo de guardado CU-018:
        /// 1. Inserta el nuevo registro en 'punto_Acopio' y obtiene el id_Punto_Acopio generado por SQL Server.
        /// 2. Inmediatamente después, inserta la relación en 'usuario_Punto_Acopio' con el id_Usuario autenticado.
        /// Retorna el id_Punto_Acopio autoincremental generado.
        /// </summary>
        Task<int> GuardarPuntoAcopioAsync(PuntoAcopioRegistroDto datos);

        /// <summary>
        /// Obtiene la lista de departamentos para el desplegable.
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

