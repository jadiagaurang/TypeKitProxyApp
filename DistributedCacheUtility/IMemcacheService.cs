using System;
using System.Threading.Tasks;

namespace TypeKitProxyApp {
    public interface IMemcacheService {
        object Get(String key);
        Task<Boolean> SetAsync(String key, object objValue, String groupKey);
        Task<Boolean> SetAsync(String key, object objValue, String groupKey, Int32 validMinutes);
    }
}