using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DevHabit.Api.Middleware;

public sealed partial class ETagMiddleware
{
    public sealed class InMemoryETagStore
    {
        private static readonly ConcurrentDictionary<string, string> ETags = new();

        public string? GetETag(string key)
        {
            return ETags.GetOrAdd(key, _ => string.Empty);
        }

        public void SetETag(string key, object resource)
        {
            ETags.AddOrUpdate(key, CreateETag(resource), (_, _) => CreateETag(resource));
        }

        public void RemoveETag(string key)
        {
            ETags.TryRemove(key, out _);
        }

        private static string CreateETag(object resource)
        {
            byte[] content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resource));

            byte[] hash = SHA512.HashData(content);

            return Convert.ToHexString(hash);
        }
    }
}
