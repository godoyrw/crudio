namespace CrudIo.Api.Cache;

public static class CacheKeys
{
    public static string UserById(Guid id) => $"user:{id}";
}