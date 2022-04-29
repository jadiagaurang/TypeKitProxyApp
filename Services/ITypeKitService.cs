using System;
using System.Threading.Tasks;

namespace TypeKitProxyApp {
    public interface ITypeKitService {
        Task<String> GetTypeKitJSAsync(String TypeKitCode);
        Task<String> GetTypeKitCSSAsync(String TypeKitCode);
    }
}