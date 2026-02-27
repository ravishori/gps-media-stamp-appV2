using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GpsMediaStamp.Application.Interfaces.Common
{
    public class InMemoryVerificationRepository : IVerificationRepository
    {
        private static readonly ConcurrentDictionary<string, bool> _hashStore
            = new ConcurrentDictionary<string, bool>();

        public Task SaveAsync(string rawHash, string stampedHash)
        {
            _hashStore.TryAdd(rawHash, true);
            _hashStore.TryAdd(stampedHash, true);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string hash)
        {
            return Task.FromResult(_hashStore.ContainsKey(hash));
        }
    }
}