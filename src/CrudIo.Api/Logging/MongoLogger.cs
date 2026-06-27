using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.IO;

namespace CrudIo.Api.Logging;
/// <summary>
/// Implementação do IApiLogger que salva logs no MongoDB
/// </summary>
public class MongoLogger : IApiLogger
{
    private readonly IMongoCollection<ApiLogEntry> _logsCollection;

    public MongoLogger(IOptions<MongoLoggerSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        var database = client.GetDatabase(options.Value.DatabaseName);
        _logsCollection = database.GetCollection<ApiLogEntry>("api_logs");

        // Criar índices para melhor performance de consultas
        var indexOptions = new CreateIndexOptions { Background = true };

        // Índice por timestamp (mais recente primeiro) - essencial para consultas de log
        var timestampIndex = Builders<ApiLogEntry>.IndexKeys
            .Descending(l => l.Timestamp);
        _logsCollection.Indexes.CreateOne(new CreateIndexModel<ApiLogEntry>(timestampIndex, indexOptions));

        // Índice composto para consultas por path e método
        var pathMethodIndex = Builders<ApiLogEntry>.IndexKeys
            .Ascending(l => l.Path)
            .Ascending(l => l.HttpMethod);
        _logsCollection.Indexes.CreateOne(new CreateIndexModel<ApiLogEntry>(pathMethodIndex, indexOptions));

        // Índice para consultas por usuário
        var userIdIndex = Builders<ApiLogEntry>.IndexKeys
            .Ascending(l => l.UserId);
        _logsCollection.Indexes.CreateOne(new CreateIndexModel<ApiLogEntry>(userIdIndex, new CreateIndexOptions { Background = true, Sparse = true }));

        // Índice para consultas por client ID
        var clientIdIndex = Builders<ApiLogEntry>.IndexKeys
            .Ascending(l => l.ClientId);
        _logsCollection.Indexes.CreateOne(new CreateIndexModel<ApiLogEntry>(clientIdIndex, new CreateIndexOptions { Background = true, Sparse = true }));
    }

    /// <summary>
    /// Registra uma entrada de log de forma assíncrona no MongoDB
    /// </summary>
    /// <param name="logEntry">A entrada de log a ser registrada</param>
    /// <returns>Task que representa a operação assíncrona</returns>
    public async Task LogRequestAsync(ApiLogEntry logEntry)
    {
        try
        {
            await _logsCollection.InsertOneAsync(logEntry);
        }
        catch (Exception ex)
        {
            // Em caso de falha ao gravar no MongoDB, apenas logamos no console
            // Não lançamos a exceção para não afetar a resposta HTTP
            try
            {
                // Tentar escrever no console de forma mais segura
                var message = $"⚠️ Falha ao gravar log no MongoDB: {ex.Message}";
                System.Console.WriteLine(message);
            }
            catch
            {
                // Se até mesmo escrever no console falhar, ignorar completamente
                // Evitar recursão infinita de falhas de logging
            }
        }
    }
}

/// <summary>
/// Configurações para o MongoLogger
/// </summary>
public class MongoLoggerSettings
{
    /// <summary>
    /// String de conexão com o MongoDB
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://mongodb:27017";

    /// <summary>
    /// Nome do banco de dados onde os logs serão armazenados
    /// </summary>
    public string DatabaseName { get; set; } = "appdb_logs";
}