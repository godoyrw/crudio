using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CrudIo.Api.Logging;

/// <summary>
/// Representa uma entrada de log para requisições HTTP armazenada no MongoDB
/// </summary>
public class ApiLogEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("http_method")]
    public string HttpMethod { get; set; } = string.Empty;

    [BsonElement("path")]
    public string Path { get; set; } = string.Empty;

    [BsonElement("query_string")]
    public string QueryString { get; set; } = string.Empty;

    [BsonElement("remote_ip")]
    public string RemoteIpAddress { get; set; } = string.Empty;

    [BsonElement("user_agent")]
    public string UserAgent { get; set; } = string.Empty;

    [BsonElement("request_headers")]
    public Dictionary<string, string[]> RequestHeaders { get; set; } = new Dictionary<string, string[]>();

    [BsonElement("status_code")]
    public int StatusCode { get; set; }

    [BsonElement("response_length")]
    public long? ResponseLength { get; set; }

    [BsonElement("request_body")]
    public string? RequestBody { get; set; }

    [BsonElement("response_body")]
    public string? ResponseBody { get; set; }

    [BsonElement("elapsed_milliseconds")]
    public long ElapsedMilliseconds { get; set; }

    [BsonElement("user_id")]
    public string? UserId { get; set; }

    [BsonElement("client_id")]
    public string? ClientId { get; set; }
}
