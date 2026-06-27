using System.Threading.Tasks;

namespace CrudIo.Api.Logging
{
    /// <summary>
    /// Interface para serviços de logging de requisições API
    /// </summary>
    public interface IApiLogger
    {
        /// <summary>
        /// Registra uma entrada de log de forma assíncrona
        /// </summary>
        /// <param name="logEntry">A entrada de log a ser registrada</param>
        /// <returns>Task que representa a operação assíncrona</returns>
        Task LogRequestAsync(ApiLogEntry logEntry);
    }
}
