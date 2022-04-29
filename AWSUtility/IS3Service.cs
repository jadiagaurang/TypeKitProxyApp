using System;
using System.Threading.Tasks;

namespace TypeKitProxyApp {
    public interface IS3Service {
        Task<String> GetObjectAsync(String strKey);
        Task<Boolean> SetObjectAsync(String strKey, String strContent, String strContentType = "");
    }
}